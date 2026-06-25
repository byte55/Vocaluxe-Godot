#region license
// This file is part of Vocaluxe.
//
// Vocaluxe is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Vocaluxe is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Vocaluxe. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using VocaluxeCore.Songs;

namespace VocaluxeCore.Game
{
    /// <summary>
    ///     Accumulated score for one player/voice.
    /// </summary>
    public class CPlayerScore
    {
        public double Points;
        public double PointsGoldenNotes;
        public double PointsLineBonus;
        public double Rating;
        public double RatingLastLine;
        public int NoteDiff;
        public int CurrentLine = -1;
        public int CurrentNote = -1;
        public readonly List<CSungLine> SungLines = new List<CSungLine>();
    }

    /// <summary>
    ///     Pure, portable scoring core extracted from the app's CGame.UpdatePoints. It scores one
    ///     voice for one player. Drive it beat-by-beat with the player's currently detected tone;
    ///     the recording/game-state coupling (CRecord, Players[], game modes) lives in the host.
    /// </summary>
    public class CVoiceScorer
    {
        private readonly CVoice _Voice;
        private readonly EGameDifficulty _Difficulty;
        private int _LastEvalBeat;

        public CPlayerScore Score { get; } = new CPlayerScore();

        /// <param name="voice">Expected notes for this player's voice.</param>
        /// <param name="difficulty">Hit tolerance: easy = +-2, normal = +-1, hard = +-0 half tones.</param>
        /// <param name="firstBeat">Last already-evaluated beat; evaluation starts at firstBeat + 1.</param>
        public CVoiceScorer(CVoice voice, EGameDifficulty difficulty, int firstBeat = -1)
        {
            _Voice = voice;
            _Difficulty = difficulty;
            _LastEvalBeat = firstBeat;
        }

        /// <summary>
        ///     Evaluate every beat in (_LastEvalBeat, recordedBeat] using the player's current tone.
        ///     Mirrors the original: the same current tone is applied across the caught-up beat range.
        /// </summary>
        public void Update(int recordedBeat, bool toneValid, int sungTone)
        {
            if (_LastEvalBeat >= recordedBeat)
            {
                return;
            }

            for (var beat = _LastEvalBeat + 1; beat <= recordedBeat; beat++)
            {
                _EvaluateBeat(beat, toneValid, sungTone);
            }

            _LastEvalBeat = recordedBeat;
        }

        private void _EvaluateBeat(int beat, bool toneValid, int sungTone)
        {
            var s = Score;
            var lines = _Voice.Lines;
            var line = _Voice.FindPreviousLine(beat);
            if (line < 0 || lines[line].EndBeat < beat)
            {
                return;
            }

            //Check for already sung
            if (line < s.SungLines.Count - 1)
            {
                return; // Already sung whole line
            }

            if (line == s.SungLines.Count - 1)
            {
                //We are in the last line
                if (beat <= s.SungLines[line].LastNoteBeat)
                {
                    return; //We already have something that ends with/after that beat
                }
            }

            if (line != s.CurrentLine)
            {
                s.CurrentNote = -1;
            }

            s.CurrentLine = line;

            while (s.SungLines.Count <= line)
            {
                s.SungLines.Add(new CSungLine());
            }

            var notes = lines[line].Notes;
            var note = lines[line].FindPreviousNote(beat);
            if (note < 0 || notes[note].EndBeat < beat)
            {
                return;
            }

            s.CurrentNote = note;

            if (notes[note].PointsForBeat > 0 && toneValid)
            {
                var tone = notes[note].Tone;
                var tonePlayer = sungTone;

                while (tonePlayer - tone > 6)
                {
                    tonePlayer -= 12;
                }

                while (tonePlayer - tone < -6)
                {
                    tonePlayer += 12;
                }

                if (notes[note].Type == ENoteType.Rap || notes[note].Type == ENoteType.RapGolden)
                {
                    tonePlayer = tone;
                }

                s.NoteDiff = Math.Abs(tone - tonePlayer);
                var hit = s.NoteDiff <= 2 - (int)_Difficulty;

                if (hit)
                {
                    var points = (CSettings.MaxScore - CSettings.LinebonusScore) * (double)notes[note].PointsForBeat / _Voice.Points;
                    if (notes[note].Type == ENoteType.Golden || notes[note].Type == ENoteType.RapGolden)
                    {
                        s.PointsGoldenNotes += points;
                    }

                    s.Points += points;

                    // update player notes (sung notes)
                    if (s.SungLines[line].NoteCount > 0)
                    {
                        var lastNote = s.SungLines[line].LastNote;

                        if (notes[note].StartBeat == beat || lastNote.EndBeat + 1 != beat || lastNote.Tone != tone || !lastNote.Hit)
                        {
                            s.SungLines[line].AddNote(new CSungNote(beat, 1, tone, notes[note], points));
                        }
                        else
                        {
                            s.SungLines[line].IncLastNoteLength();
                            s.SungLines[line].LastNote.Points += points;
                        }
                    }
                    else
                    {
                        s.SungLines[line].AddNote(new CSungNote(beat, 1, tone, notes[note], points));
                    }

                    s.SungLines[line].LastNote.CheckPerfect();
                    s.SungLines[line].IsPerfect(lines[line]);
                }
                else
                {
                    if (s.SungLines[line].NoteCount > 0)
                    {
                        var lastNote = s.SungLines[line].LastNote;
                        if (lastNote.Tone != tonePlayer || lastNote.EndBeat + 1 != beat || lastNote.Hit)
                        {
                            s.SungLines[line].AddNote(new CSungNote(beat, 1, tonePlayer));
                        }
                        else
                        {
                            s.SungLines[line].IncLastNoteLength();
                        }
                    }
                    else
                    {
                        s.SungLines[line].AddNote(new CSungNote(beat, 1, tonePlayer));
                    }
                }
            }

            // Check if line ended
            var numLinesWithPoints = _Voice.NumLinesWithPoints;
            if (beat == lines[line].LastNoteBeat && lines[line].Points > 0 && numLinesWithPoints > 0)
            {
                // Line Bonus
                var factor = s.SungLines[line].Points / (double)lines[line].Points;
                if (factor <= 0.4)
                {
                    factor = 0.0;
                }
                else if (factor >= 0.9)
                {
                    factor = 1.0;
                }
                else
                {
                    factor -= 0.4;
                    factor *= 2;
                    factor *= factor;
                }

                var points = CSettings.LinebonusScore * factor / numLinesWithPoints;
                s.Points += points;
                s.PointsLineBonus += points;
                s.SungLines[line].BonusPoints += points;

                //Calculate rating
                //Shift fraction of correct sung notes to [-0.1, 0.1], player needs to sing five lines fully correctly to get highest ranking
                var current = s.SungLines[line].Points / (double)lines[line].Points;
                s.Rating = (s.Rating + (current * 0.2 - 0.1)).Clamp(0, 1);
                s.RatingLastLine = current;
            }
        }
    }
}
