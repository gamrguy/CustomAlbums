using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CustomAlbums.Data
{
    public class AlbumInfo
    {
        public enum HideBmsMode
        {
            CLICK,
            PRESS,
            TOGGLE
        }

        public string name;
        public string name_en;
        public string name_ko;
        public string name_ja;
        public string name_zh_hans;
        public string name_zh_hant;

        public string author;
        public string author_en;
        public string author_ko;
        public string author_ja;
        public string author_zh_hans;
        public string author_zh_hant;

        public string levelDesigner;
        public string levelDesigner1;
        public string levelDesigner2;
        public string levelDesigner3;
        public string levelDesigner4;

        [DefaultValue("0")]
        public string bpm;
        [DefaultValue("scene_01")]
        public string scene;

        public string difficulty1;
        public string difficulty2;
        public string difficulty3;
        public string difficulty4;

        [DefaultValue("0")]
        public string unlockLevel;

        [DefaultValue("CLICK")]
        public string hideBmsMode;
        public int hideBmsDifficulty;
        public string hideBmsMessage;

        public string[] searchTags;

        public string GetName(string lang = null)
        {
            // If "name_<lang>" not avaliable will return "name"
            // If "name" not avaliable will return "Unknown" 
            string result;
            switch (lang)
            {
                case "ChineseT":
                    result = name_zh_hant;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                case "ChineseS":
                    result = name_zh_hans;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                case "English":
                    result = name_en;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                case "Korean":
                    result = name_ko;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                case "Japanese":
                    result = name_ja;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                default:
                    result = name;
                    if (string.IsNullOrEmpty(result)) result = "Unknown";
                    break;
            }
            return result;
        }
        public string GetAuthor(string lang = null)
        {
            // If "author_<lang>" not avaliable will return "author"
            // If "author" not avaliable will return "Unknown" 
            string result;
            switch (lang)
            {
                case "ChineseT":
                    result = author_zh_hant;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                case "ChineseS":
                    result = author_zh_hans;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                case "English":
                    result = author_en;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                case "Korean":
                    result = author_ko;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                case "Japanese":
                    result = author_ja;
                    if (string.IsNullOrEmpty(result)) goto default;
                    break;
                default:
                    result = author;
                    if (string.IsNullOrEmpty(result)) result = "Unknown";
                    break;
            }
            return result;
        }

        public string GetDifficulty(int idx)
        {
            switch (idx)
            {
                case 1:
                    return difficulty1;
                case 2:
                    return difficulty2;
                case 3:
                    return difficulty3;
                case 4:
                    return difficulty4;
            }
            return "?";
        }
        public string GetLevelDesigner(int idx)
        {
            switch (idx)
            {
                case 0:
                    return levelDesigner;
                case 1:
                    return levelDesigner1;
                case 2:
                    return levelDesigner2;
                case 3:
                    return levelDesigner3;
                case 4:
                    return levelDesigner4;
            }
            return "?";
        }

        public Dictionary<int, string> GetDifficulties()
        {
            Dictionary<int, string> map = new Dictionary<int, string>();
            map.Add(1, difficulty1);
            map.Add(2, difficulty2);
            map.Add(3, difficulty3);
            if (!string.IsNullOrEmpty(difficulty4))
                map.Add(4, difficulty4);

            return map;
        }

        public Dictionary<int, string> GetLevelDesigners()
        {
            Dictionary<int, string> map = new Dictionary<int, string>();
            if (!string.IsNullOrEmpty(levelDesigner))
                map.Add(0, levelDesigner);
            if (!string.IsNullOrEmpty(levelDesigner1))
                map.Add(1, levelDesigner1);
            if (!string.IsNullOrEmpty(levelDesigner2))
                map.Add(2, levelDesigner2);
            if (!string.IsNullOrEmpty(levelDesigner3))
                map.Add(3, levelDesigner3);
            if (!string.IsNullOrEmpty(levelDesigner4))
                map.Add(4, levelDesigner4);

            return map;
        }

        public HideBmsMode GetHideBmsMode() {
            return Enum.TryParse(hideBmsMode, out HideBmsMode result) ? result : HideBmsMode.CLICK;
        }
    }
}
