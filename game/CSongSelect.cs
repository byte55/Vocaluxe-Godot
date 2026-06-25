using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using VocaluxeCore.Log;
using VocaluxeCore.Songs;

namespace Vocaluxe
{
    /// <summary>
    ///     Minimal song-selection screen: scans the song library, lists artist/title, and launches the
    ///     sing screen for the chosen song. Up/Down to move, Enter to sing, Esc to quit.
    /// </summary>
    public partial class CSongSelect : Control
    {
        private readonly List<(string Txt, string Artist, string Title)> _Songs = new();
        private int _Index;
        private string? _Error;

        public override void _Ready()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), ".."));
            CLog.InitFile(Path.Combine(repoRoot, "vocaluxe.log"));
            CLog.ErrorSink = s => GD.PrintErr("[audio] " + s);
            CLog.WarningSink = s => GD.Print("[audio] " + s);

            try
            {
                var songsRoot = Path.Combine(repoRoot, "songs");
                if (!Directory.Exists(songsRoot))
                {
                    _Error = "No songs folder at " + songsRoot;
                    return;
                }

                foreach (var dir in Directory.GetDirectories(songsRoot).OrderBy(d => d))
                {
                    var txt = Directory.GetFiles(dir, "*.txt").FirstOrDefault();
                    if (txt == null)
                    {
                        continue;
                    }

                    var song = CSong.LoadSong(txt);
                    if (song != null)
                    {
                        _Songs.Add((txt, song.Artist, song.Title));
                    }
                }

                _Songs.Sort((a, b) => string.Compare(a.Artist + a.Title, b.Artist + b.Title, StringComparison.OrdinalIgnoreCase));
                CLog.Info($"Song select: {_Songs.Count} songs found.");
                if (_Songs.Count == 0)
                {
                    _Error = "No playable songs found in " + songsRoot;
                }
            }
            catch (Exception e)
            {
                _Error = e.Message;
                CLog.Error("Song scan failed: " + e);
            }
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            if (@event is not InputEventKey { Pressed: true } key)
            {
                return;
            }

            switch (key.Keycode)
            {
                case Key.Up:
                    _Index = Math.Max(0, _Index - 1);
                    break;
                case Key.Down:
                    _Index = Math.Min(_Songs.Count - 1, _Index + 1);
                    break;
                case Key.Enter or Key.KpEnter:
                    if (_Songs.Count > 0)
                    {
                        CSession.SelectedSongTxt = _Songs[_Index].Txt;
                        GetTree().ChangeSceneToFile("res://SingScreen.tscn");
                        return;
                    }

                    break;
                case Key.Escape:
                    GetTree().Quit();
                    return;
            }

            QueueRedraw();
        }

        public override void _Draw()
        {
            var font = GetThemeDefaultFont();
            var size = GetViewportRect().Size;
            DrawRect(new Rect2(Vector2.Zero, size), new Color(0.07f, 0.08f, 0.10f));

            DrawString(font, new Vector2(40, 50), "Select a song", HorizontalAlignment.Left, -1, 28, Colors.White);
            DrawString(font, new Vector2(40, 80), "Up/Down to move · Enter to sing · Esc to quit", HorizontalAlignment.Left, -1, 14, new Color(0.65f, 0.65f, 0.7f));

            if (_Error != null)
            {
                DrawString(font, new Vector2(40, 120), _Error, HorizontalAlignment.Left, -1, 16, new Color(0.9f, 0.5f, 0.5f));
                return;
            }

            var y = 130f;
            for (var i = 0; i < _Songs.Count; i++)
            {
                var selected = i == _Index;
                if (selected)
                {
                    DrawRect(new Rect2(30, y - 22, size.X - 60, 30), new Color(0.0f, 0.58f, 0.79f, 0.30f));
                }

                var s = _Songs[i];
                DrawString(font, new Vector2(44, y), $"{s.Artist} - {s.Title}", HorizontalAlignment.Left, size.X - 88, 20,
                    selected ? Colors.White : new Color(0.75f, 0.78f, 0.82f));
                y += 34;
            }
        }
    }
}
