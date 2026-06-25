using System;
using System.IO;
using System.Linq;
using Godot;
using VocaluxeCore.Game;
using VocaluxeCore.Log;
using VocaluxeCore.Songs;
using VocaluxeAudio.Record;

namespace Vocaluxe
{
    /// <summary>
    ///     First playable: loads an UltraStar song, plays its audio, captures the default microphone
    ///     (PortAudio -> pitch tracker) and scores the singing live, drawing the current line's notes,
    ///     the detected pitch and the score. Single player for now; the capture/scoring core underneath
    ///     is multiplayer-ready.
    /// </summary>
    public partial class CSingScreen : Control
    {
        [Export] public string SongName = "AnnenMayKantereit - Pocahontas";

        /// <summary>Silence gate (0-1). Lower it if your mic is quiet and no tone shows up.</summary>
        [Export] public float VolumeThreshold = 0.02f;

        /// <summary>Software mic boost. Raw ALSA capture is quiet; raise this if you must sing very loud.</summary>
        [Export] public float MicGain = 8f;

        /// <summary>Manual audio/scoring sync offset in seconds (positive = notes later).</summary>
        [Export] public float AudioOffset = 0f;

        /// <summary>
        ///     Name (substring, case-insensitive) of the mic to assign to player 1. Falls back to the
        ///     system default input device if not found. Empty = always use the default device.
        /// </summary>
        [Export] public string PreferredMicName = "QuadCast";

        private const int Player = 0;

        private CSong? _Song;
        private CVoice? _Voice;
        private CVoiceScorer? _Scorer;
        private CPortAudioRecord? _Record;
        private AudioStreamPlayer? _Audio;
        private string _MicName = "(none)";
        private string? _Error;

        private float _BeatF;
        private int _PlayerTone = -1;
        private bool _PlayerToneValid;
        private float _MaxVol;

        private int _Frame;

        public override void _Ready()
        {
            // File logger (read back after a run) + echo warnings/errors to the Godot console.
            var repoRoot = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), ".."));
            CLog.InitFile(Path.Combine(repoRoot, "vocaluxe.log"));
            CLog.ErrorSink = s => GD.PrintErr("[audio] " + s);
            CLog.WarningSink = s => GD.Print("[audio] " + s);
            CLog.Info("SingScreen start");
            CRecordBase.DebugChannelLevels = true; // log raw per-channel mic peaks (~1/s) for debugging

            try
            {
                var songsRoot = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "songs"));
                var folder = Path.Combine(songsRoot, SongName);
                var txt = Directory.Exists(folder) ? Directory.GetFiles(folder, "*.txt").FirstOrDefault() : null;
                if (txt == null)
                {
                    throw new Exception($"No .txt song found in {folder}");
                }

                _Song = CSong.LoadSong(txt);
                if (_Song == null || !_Song.LoadNotes())
                {
                    throw new Exception("Failed to load song / notes from " + txt);
                }

                _Voice = _Song.Notes.GetVoice(0);
                _Scorer = new CVoiceScorer(_Voice, EGameDifficulty.TR_CONFIG_NORMAL);

                // Audio
                var mp3Path = _Song.GetMP3();
                var stream = new AudioStreamMP3 { Data = File.ReadAllBytes(mp3Path) };
                _Audio = new AudioStreamPlayer { Stream = stream };
                AddChild(_Audio);

                // Microphone capture
                _Record = new CPortAudioRecord();
                if (_Record.Init())
                {
                    var devices = _Record.RecordDevices();

                    CRecordDevice? dev = null;
                    if (devices != null && !string.IsNullOrEmpty(PreferredMicName))
                    {
                        dev = devices.FirstOrDefault(d => d.Name.IndexOf(PreferredMicName, StringComparison.OrdinalIgnoreCase) >= 0);
                    }

                    dev ??= _Record.DefaultInputDevice();

                    CLog.Info($"Input devices ({devices?.Count ?? 0}):");
                    if (devices != null)
                    {
                        foreach (var d in devices)
                        {
                            CLog.Info($"  '{d.Name}' (ch {d.Channels}){(d == dev ? "   <-- assigned to player 1" : "")}");
                        }
                    }

                    if (dev != null)
                    {
                        _MicName = dev.Name;
                        dev.PlayerChannel[0] = 1; // channel 0 -> player 1
                        _Record.SetVolumeThreshold(Player, VolumeThreshold);
                        _Record.SetGain(Player, MicGain);
                        if (!_Record.Start())
                        {
                            _Error = "Mic stream failed to start (see [audio] log).";
                        }

                        CLog.Info($"Capturing '{_MicName}' for player 1 (threshold {VolumeThreshold}, gain {MicGain}).");
                    }
                    else
                    {
                        _Error = "No input device found (you can still watch playback).";
                    }
                }
                else
                {
                    _Error = "PortAudio init failed (you can still watch playback).";
                }

                _Audio.Play();

                // Skip the (often long) instrumental intro: start ~1.5s before the first note so notes
                // and scoring are visible immediately instead of after the song's GAP.
                if (_Voice != null && _Voice.Lines.Length > 0)
                {
                    var firstNoteTime = CGame.GetTimeFromBeats(_Voice.Lines[0].FirstNoteBeat, _Song.Bpm) + _Song.Gap - 1.5f;
                    if (firstNoteTime > 0.5f)
                    {
                        _Audio.Seek(firstNoteTime);
                        CLog.Info($"Seek to {firstNoteTime:0.0}s (first note ~beat {_Voice.Lines[0].FirstNoteBeat}, gap {_Song.Gap:0.0}s).");
                    }
                }
            }
            catch (Exception e)
            {
                _Error = e.Message;
                GD.PrintErr("SingScreen error: " + e);
            }
        }

        public override void _Process(double delta)
        {
            if (_Song == null || _Audio == null || !_Audio.Playing)
            {
                QueueRedraw();
                return;
            }

            var time = (float)_Audio.GetPlaybackPosition() + AudioOffset;
            _BeatF = CGame.GetBeatFromTime(time, _Song.Bpm, _Song.Gap);

            if (_Record != null)
            {
                _Record.AnalyzeBuffer(Player);
                _PlayerToneValid = _Record.ToneValid(Player);
                _PlayerTone = _Record.GetToneAbs(Player);
                _MaxVol = _Record.GetMaxVolume(Player);

                var recordedBeat = (int)Math.Floor(_BeatF - 0.5f);
                _Scorer?.Update(recordedBeat, _PlayerToneValid, _PlayerTone);
            }

            if (++_Frame % 30 == 0)
            {
                CLog.Info($"t={time:0.0} beat={_BeatF:0.0} valid={_PlayerToneValid} tone={_PlayerTone} vol={_MaxVol:0.0000} score={_Scorer?.Score.Points ?? 0:0}");
            }

            QueueRedraw();
        }

        public override void _ExitTree()
        {
            _Record?.Close();
            CLog.Info("SingScreen exit");
            CLog.Close();
        }

        private static readonly Color CColPlayer = new Color(0.0f, 0.58f, 0.79f);
        private static readonly Color CColNote = new Color(0.55f, 0.62f, 0.72f);
        private static readonly Color CColGolden = new Color(0.95f, 0.78f, 0.20f);
        private static readonly Color CColRap = new Color(0.62f, 0.45f, 0.78f);

        public override void _Draw()
        {
            var font = GetThemeDefaultFont();
            var size = GetViewportRect().Size;
            DrawRect(new Rect2(Vector2.Zero, size), new Color(0.07f, 0.08f, 0.10f));

            if (_Song == null || _Voice == null)
            {
                DrawString(font, new Vector2(20, 40), "Loading… " + (_Error ?? ""), HorizontalAlignment.Left, -1, 20, Colors.White);
                return;
            }

            // Header
            DrawString(font, new Vector2(20, 30), $"{_Song.Artist} - {_Song.Title}", HorizontalAlignment.Left, -1, 22, Colors.White);
            DrawString(font, new Vector2(20, 56), $"BPM {_Song.Bpm / 4:0.##}   Mic: {_MicName}   beat {_BeatF:0.0}", HorizontalAlignment.Left, -1, 14, new Color(0.7f, 0.7f, 0.75f));
            DrawString(font, new Vector2(size.X - 320, 30), $"Score {_Scorer?.Score.Points ?? 0:0}", HorizontalAlignment.Left, -1, 22, CColGolden);
            DrawString(font, new Vector2(size.X - 320, 56),
                $"tone {(_PlayerToneValid ? _PlayerTone.ToString() : "-")}  vol {_MaxVol:0.000}  thr {VolumeThreshold:0.000}  gain {MicGain:0.#}",
                HorizontalAlignment.Left, -1, 14, _PlayerToneValid ? CColPlayer : new Color(0.6f, 0.6f, 0.6f));

            if (_Error != null)
            {
                DrawString(font, new Vector2(20, 84), _Error, HorizontalAlignment.Left, -1, 14, new Color(0.9f, 0.5f, 0.5f));
            }

            // Note area
            var area = new Rect2(40, 130, size.X - 80, size.Y - 260);
            DrawRect(area, new Color(0.11f, 0.13f, 0.16f));

            var voice = _Voice;
            var lines = voice.Lines;
            if (lines.Length == 0)
            {
                return;
            }

            var curBeat = (int)Math.Floor(_BeatF);
            var li = voice.FindPreviousLine(curBeat);
            if (li < 0)
            {
                li = 0;
            }

            var line = lines[li];
            // Advance to the line that actually contains the current beat when between notes.
            if (line.LastNoteBeat < curBeat && li + 1 < lines.Length)
            {
                line = lines[li + 1];
            }

            var notes = line.Notes;
            if (notes.Length == 0)
            {
                return;
            }

            int beatStart = line.FirstNoteBeat;
            int beatEnd = Math.Max(line.LastNoteBeat + 1, beatStart + 1);
            int minTone = notes.Min(n => n.Tone);
            int maxTone = notes.Max(n => n.Tone);
            if (maxTone - minTone < 11)
            {
                var mid = (minTone + maxTone) / 2;
                minTone = mid - 6;
                maxTone = mid + 6;
            }

            float X(float beat) => area.Position.X + (beat - beatStart) / (beatEnd - beatStart) * area.Size.X;
            float Y(int tone)
            {
                var t = Math.Clamp((tone - minTone) / (float)(maxTone - minTone), 0f, 1f);
                return area.Position.Y + area.Size.Y - t * (area.Size.Y - 20) - 10;
            }

            var rowH = Math.Max(8f, area.Size.Y / (maxTone - minTone + 2));

            foreach (var n in notes)
            {
                var col = n.Type == ENoteType.Golden || n.Type == ENoteType.RapGolden ? CColGolden
                    : n.IsRapNote ? CColRap
                    : n.Type == ENoteType.Freestyle ? new Color(0.4f, 0.4f, 0.4f)
                    : CColNote;
                var x0 = X(n.StartBeat);
                var x1 = X(n.EndBeat + 1);
                DrawRect(new Rect2(x0, Y(n.Tone) - rowH / 2, Math.Max(3f, x1 - x0 - 2), rowH), col);
            }

            // "now" line + player pitch marker
            var nowX = X(_BeatF);
            DrawLine(new Vector2(nowX, area.Position.Y), new Vector2(nowX, area.Position.Y + area.Size.Y), new Color(1, 1, 1, 0.5f), 2);
            if (_PlayerToneValid)
            {
                DrawCircle(new Vector2(nowX, Y(_PlayerTone)), 7, CColPlayer);
            }

            // Lyrics
            DrawString(font, new Vector2(40, area.Position.Y + area.Size.Y + 50), line.Lyrics, HorizontalAlignment.Left, size.X - 80, 26, Colors.White);
            DrawString(font, new Vector2(40, size.Y - 24), "Sing into the mic. Esc to quit.", HorizontalAlignment.Left, -1, 13, new Color(0.6f, 0.6f, 0.65f));
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is InputEventKey { Pressed: true, Keycode: Key.Escape })
            {
                GetTree().Quit();
            }
        }
    }
}
