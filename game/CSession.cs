namespace Vocaluxe
{
    /// <summary>
    ///     Tiny process-wide state passed between scenes (survives scene switches as a static).
    /// </summary>
    public static class CSession
    {
        /// <summary>Absolute path to the selected song's .txt, set by the song-select screen.</summary>
        public static string? SelectedSongTxt;
    }
}
