using System.Collections.Generic;
namespace CustomAlbums.Data
{
    public enum Side
    {
        Bottom = 1,
        Top
    }
    public class BeatRecord
    {
        public string NoteUid { get; set; }
        public int Offset { get; set; }
        public int Score { get; set; }
        public Side Side { get; set; }
    }
    public class PlayData
    {
        public string BMSHash { get; set; }
        public string SelectedMusicUid { get; set; }
        public int SelectedDifficulty { get; set; }
        public string SelectedCharacterUid { get; set; }
        public string SelectedElfinUid { get; set; }
        public int ComboCount { get; set; }
        public int Hp { get; set; }
        public int Score { get; set; }
        public float Accuracy { get; set; }
        public int MissCount { get; set; }
        public string Judge { get; set; }
        public List<BeatRecord> BeatRecords { get; set; }
    }
}
