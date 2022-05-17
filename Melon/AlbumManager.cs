using Il2Generic = Il2CppSystem.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PeroTools2.Resources;
using UnityEngine;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using Il2CppNewtonsoft.Json.Linq;

namespace CustomAlbums
{
    public static class AlbumManager
    {
        private static readonly Logger Log = new Logger("AlbumManager");

        /// <summary>
        /// Music package uid.
        /// </summary>
        public static readonly int Uid = 999;
        /// <summary>
        /// Album file name.
        /// </summary>
        public static readonly string JsonName = $"ALBUM{Uid + 1}";
        /// <summary>
        /// Music package uid in albums.json.
        /// </summary>
        public static readonly string MusicPackge = $"music_package_{Uid}";
        /// <summary>
        /// Localized string. Do not move the order of items!!
        /// </summary>
        public static readonly Dictionary<string, string> Langs = new Dictionary<string, string>()
        {
            { "English", "Custom Albums" },
            { "ChineseS", "自定义" },
            { "ChineseT", "自定義" },
            { "Japanese", "Custom Albums" },
            { "Korean", "Custom Albums" },
        };
        /// <summary>
        /// Search custom album in this folder.
        /// </summary>
        public static readonly string SearchPath = "Custom_Albums";
        /// <summary>
        /// Packaged custom album extension name.
        /// </summary>
        public static readonly string SearchExtension = "mdm";
        /// <summary>
        /// Loaded custom albums, indexed by their filename
        /// </summary>
        public static Dictionary<string, Album> LoadedAlbums = new Dictionary<string, Album>();
        /// <summary>
        /// Loaded custom albums, indexed by their in-game UID
        /// </summary>
        public static Dictionary<string, Album> LoadedAlbumsByUid = new Dictionary<string, Album>();
        /// <summary>
        /// Failed to load custom album.
        /// </summary>
        public static Dictionary<string, string> CorruptedAlbums = new Dictionary<string, string>();

        public static Il2Generic.List<Il2CppSystem.Object> AssetKeys = new Il2Generic.List<Il2CppSystem.Object>();
        /// <summary>
        /// Clear all loaded custom albums and reload.
        /// </summary>
        public static void LoadAll() {
            try {
                LoadedAlbums.Clear();
                CorruptedAlbums.Clear();

                // Ensure the customs directory actually exists
                Directory.CreateDirectory(SearchPath);

                int nextIndex = 0;
                foreach(var path in Directory.GetFiles(SearchPath).Union(Directory.GetDirectories(SearchPath))) {
                    try {
                        var album = new Album(path);
                        album.Index = nextIndex++;

                        if(LoadedAlbums.ContainsKey(album.Name)) {
                            throw new Exception("Duplicate chart file name!");
                        }
                        LoadedAlbums.Add(album.Name, album);
                        LoadedAlbumsByUid.Add($"{Uid}-{album.Index}", album);

                        AssetKeys.Add($"{album.Name}_demo");
                        AssetKeys.Add($"{album.Name}_music");
                        AssetKeys.Add($"{album.Name}_cover");

                        if(!string.IsNullOrEmpty(album.Info.difficulty1))
                            AssetKeys.Add($"{album.Name}_map1");
                        if(!string.IsNullOrEmpty(album.Info.difficulty2))
                            AssetKeys.Add($"{album.Name}_map2");
                        if(!string.IsNullOrEmpty(album.Info.difficulty3))
                            AssetKeys.Add($"{album.Name}_map3");
                        if(!string.IsNullOrEmpty(album.Info.difficulty4))
                            AssetKeys.Add($"{album.Name}_map4");

                        // Preload chart cover, and never unload it
                        ResourcesManager.instance.LoadFromName<Sprite>($"{album.Name}_cover").hideFlags |= HideFlags.DontUnloadUnusedAsset;
                    } catch(Exception e) {
                        Log.Warning($"Invalid MDM file/folder: {e.Message} | {path}");
                    }
                }

                Log.Info("Maps loaded:");
                Log.Info(string.Format("{0,-20} {1,-20} {2,-20}", "Map", "Title", "Author"));
                foreach(var album in LoadedAlbums) {
                    var mapName = album.Key.Substring(0, Math.Min(album.Key.Length, 20));
                    var mapTitle = album.Value.Info.name.Substring(0, Math.Min(album.Value.Info.name.Length, 20));
                    var mapAuthor = album.Value.Info.levelDesigner.Substring(0, Math.Min(album.Value.Info.levelDesigner.Length, 20));
                    Log.Info(string.Format("{0,-20} {1,-20} {2,-20}", mapName, mapTitle, mapAuthor));
                }

            } catch(Exception e) {
                Log.Error("Exception while loading albums: " + e);
            }
        }

        /// <summary>
        /// Get all loaded album uid.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllUid()
        {
            List<string> uids = new List<string>();

            foreach (var album in LoadedAlbums)
            {
                uids.Add($"{Uid}-{album.Value.Index}");
            }

            return uids;
        }
        /// <summary>
        /// Get album mapping key from index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string GetAlbumKeyByIndex(int index)
        {
            return LoadedAlbums.FirstOrDefault(pair => pair.Value.Index == index).Key;
        }
    
        /// <summary>
        /// Updates the in-game album lists
        /// </summary>
        public static void RefreshAlbumData() {
            if(Singleton<ConfigManager>.instance.m_Dictionary.TryGetValue("albums", out var albumsJson)) {
                var found = false;
                foreach(JToken thing in albumsJson._values) {
                    if((string)thing["uid"] == MusicPackge) {
                        found = true;
                        break;
                    }
                }

                if(!found) {
                    var thisJson = new JObject();
                    thisJson["uid"] = MusicPackge;
                    thisJson["title"] = "Custom Albums";
                    thisJson["prefabsName"] = $"AlbumDisco{Uid}";
                    thisJson["price"] = "$0.00";
                    thisJson["jsonName"] = JsonName;
                    thisJson["needPurchase"] = false;
                    thisJson["free"] = true;

                    albumsJson.Add(thisJson);
                }
            }
/*

            if(_assetName == "albums") {
                var textAsset = new TextAsset(assetPtr);
                var jArray = textAsset.text.JsonDeserialize<JArray>();
                jArray.Add(JObject.FromObject(new {
                    uid = AlbumManager.MusicPackge,
                    title = "Custom Albums",
                    prefabsName = $"AlbumDisco{AlbumManager.Uid}",
                    price = "¥25.00",
                    jsonName = AlbumManager.JsonName,
                    needPurchase = false,
                    free = true,
                }));
                newAsset = CreateTextAsset(_assetName, jArray.JsonSerialize());
                if(!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(_assetName)) Singleton<ConfigManager>.instance.Add(_assetName, ((TextAsset)newAsset).text);
            } else if(_assetName == AlbumManager.JsonName) {
                var jArray = new JArray();
                foreach(var keyValue in AlbumManager.LoadedAlbums) {
                    var key = keyValue.Key;
                    var album = keyValue.Value;
                    var info = album.Info;
                    var jObject = new JObject();
                    jObject.Add("uid", $"{AlbumManager.Uid}-{album.Index}");
                    jObject.Add("name", info.GetName());
                    jObject.Add("author", info.GetAuthor());
                    jObject.Add("bpm", info.bpm);
                    jObject.Add("music", $"{key}_music");
                    jObject.Add("demo", $"{key}_demo");
                    jObject.Add("cover", $"{key}_cover");
                    jObject.Add("noteJson", $"{key}_map");
                    jObject.Add("scene", info.scene);
                    jObject.Add("unlockLevel", string.IsNullOrEmpty(info.unlockLevel) ? "0" : info.unlockLevel);
                    if(!string.IsNullOrEmpty(info.levelDesigner))
                        jObject.Add("levelDesigner", info.levelDesigner);
                    if(!string.IsNullOrEmpty(info.levelDesigner1))
                        jObject.Add("levelDesigner1", info.levelDesigner1);
                    if(!string.IsNullOrEmpty(info.levelDesigner2))
                        jObject.Add("levelDesigner2", info.levelDesigner2);
                    if(!string.IsNullOrEmpty(info.levelDesigner3))
                        jObject.Add("levelDesigner3", info.levelDesigner3);
                    if(!string.IsNullOrEmpty(info.levelDesigner4))
                        jObject.Add("levelDesigner4", info.levelDesigner4);
                    if(!string.IsNullOrEmpty(info.difficulty1))
                        jObject.Add("difficulty1", info.difficulty1);
                    if(!string.IsNullOrEmpty(info.difficulty2))
                        jObject.Add("difficulty2", info.difficulty2);
                    if(!string.IsNullOrEmpty(info.difficulty3))
                        jObject.Add("difficulty3", info.difficulty3);
                    if(!string.IsNullOrEmpty(info.difficulty4))
                        jObject.Add("difficulty4", info.difficulty4);
                    jArray.Add(jObject);
                }
                newAsset = CreateTextAsset(_assetName, jArray.JsonSerialize());
                if(!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(_assetName)) Singleton<ConfigManager>.instance.Add(_assetName, ((TextAsset)newAsset).text);
            } else if(_assetName == $"albums_{lang}") {
                var textAsset = new TextAsset(assetPtr);
                var jArray = textAsset.text.JsonDeserialize<JArray>();
                jArray.Add(JObject.FromObject(new {
                    title = AlbumManager.Langs[lang],
                }));
                newAsset = CreateTextAsset(_assetName, jArray.JsonSerialize());
                if(!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(_assetName)) Singleton<ConfigManager>.instance.Add(_assetName, ((TextAsset)newAsset).text);
            } else if(_assetName == $"{AlbumManager.JsonName}_{lang}") {
                var jArray = new JArray();
                foreach(var keyValue in AlbumManager.LoadedAlbums) {
                    jArray.Add(JObject.FromObject(new {
                        name = keyValue.Value.Info.GetName(lang),
                        author = keyValue.Value.Info.GetAuthor(lang),
                    }));
                }
                newAsset = CreateTextAsset(_assetName, jArray.JsonSerialize());
                if(!Singleton<ConfigManager>.instance.m_Dictionary.ContainsKey(_assetName)) Singleton<ConfigManager>.instance.Add(_assetName, ((TextAsset)newAsset).text);
            }*/
        }
    }
}
