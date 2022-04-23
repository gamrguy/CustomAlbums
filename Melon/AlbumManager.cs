using Il2Generic = Il2CppSystem.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using PeroTools2.Resources;
using UnityEngine;

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

                if(!Directory.Exists(SearchPath)) {
                    // Target folder not exist, create it.
                    Directory.CreateDirectory(SearchPath);
                    return;
                }

                int nextIndex = 0;
                // Load albums package
                foreach(var file in Directory.GetFiles(SearchPath, $"*.{SearchExtension}")) {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    try {
                        var album = new Album(file);
                        if(album.Info != null) {
                            album.Index = nextIndex;
                            album.Name = $"pkg_{fileName}".Replace("/", "_").Replace("\\", "_").Replace(".", "_");
                            nextIndex++;

                            LoadedAlbums.Add(album.Name, album);
                            Log.Debug($"Album \"{album.Name}\" loaded.");
                        }
                    } catch(Exception ex) {
                        Log.Debug($"Load album failed: {fileName}, reason: {ex.Message}");
                        CorruptedAlbums.Add(file, ex.Message);
                    }
                }
                // Load albums folder
                foreach(var path in Directory.GetDirectories(SearchPath)) {
                    string folderName = Path.GetFileNameWithoutExtension(path);

                    try {
                        var album = new Album(path);
                        if(album.Info != null) {
                            album.Index = nextIndex;
                            album.Name = $"fs_{folderName}".Replace("/", "_").Replace("\\", "_").Replace(".", "_");
                            nextIndex++;

                            LoadedAlbums.Add(album.Name, album);
                            LoadedAlbumsByUid.Add($"{AlbumManager.Uid}-{album.Index}", album);
                            Log.Debug($"Album \"{album.Name}\" loaded.");
                        }
                    } catch(Exception ex) {
                        Log.Debug($"Load album failed: {folderName}, reason: {ex.Message}");
                        CorruptedAlbums.Add(path, ex.Message);
                    }
                }
                // Get all asset keys
                foreach(var album in LoadedAlbums) {
                    var albumKey = album.Key;
                    var info = album.Value.Info;

                    AssetKeys.Add($"{albumKey}_demo");
                    AssetKeys.Add($"{albumKey}_music");
                    AssetKeys.Add($"{albumKey}_cover");

                    if(!string.IsNullOrEmpty(info.difficulty1))
                        AssetKeys.Add($"{albumKey}_map1");
                    if(!string.IsNullOrEmpty(info.difficulty2))
                        AssetKeys.Add($"{albumKey}_map2");
                    if(!string.IsNullOrEmpty(info.difficulty3))
                        AssetKeys.Add($"{albumKey}_map3");
                    if(!string.IsNullOrEmpty(info.difficulty4))
                        AssetKeys.Add($"{albumKey}_map4");

                    // Preload chart cover, and never unload it
                    ResourcesManager.instance.LoadFromName<Sprite>($"{album.Key}_cover").hideFlags |= HideFlags.DontUnloadUnusedAsset;
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
    }
}