using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomAlbums.Data
{
    public class CustomScore
    {
        public int Evaluate;
        public int Score;
        public int Combo;
        public float Accuracy;
        public string AccuracyStr;
        public float Clear;
        public int FailCount;
        public bool Passed;
    }

    public class CustomData
    {
        public string SelectedAlbum;
        public int SelectedDifficulty;
        public List<string> Collections = new List<string>();
        public List<string> Hides = new List<string>();
        public List<string> History = new List<string>();
        public Dictionary<string, Dictionary<int, CustomScore>> Highest = new Dictionary<string, Dictionary<int, CustomScore>>();
        public Dictionary<string, List<int>> FullCombo = new Dictionary<string, List<int>>();
    }
}
