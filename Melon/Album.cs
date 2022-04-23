using Assets.Scripts.GameCore;
using CustomAlbums.Data;
using GameLogic;
using Ionic.Zip;
using Newtonsoft.Json.Linq;
using RuntimeAudioClipLoader;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

using ManagedGeneric = System.Collections.Generic;
using System.IO;
using NLayer;
using Il2CppGeneric = Il2CppSystem.Collections.Generic;
using Il2CppMemoryStream = Il2CppSystem.IO.MemoryStream;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using NVorbis.NAudioSupport;
using UnhollowerBaseLib;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.PeroTools.Commons;
using NAudio.Wave;

namespace CustomAlbums
{

    public class Album
    {
        private static readonly Logger Log = new Logger("Album");
        public static readonly ManagedGeneric.Dictionary<string, AudioFormat> AudioFormatMapping = new ManagedGeneric.Dictionary<string, AudioFormat>()
            {
                {".aiff", AudioFormat.aiff},
                {".mp3", AudioFormat.mp3},
                {".ogg", AudioFormat.ogg},
                {".wav", AudioFormat.wav},
            };

        public AlbumInfo Info { get; private set; }
        public string BasePath { get; private set; }
        public bool IsPackaged { get; private set; }
        public ManagedGeneric.Dictionary<int, string> availableMaps = new ManagedGeneric.Dictionary<int, string>();
        public int Index;
        public string Name;

        public Texture2D CoverTex { get; private set; }
        public Sprite CoverSprite { get; private set; }
        public static AudioClip MusicAudio { get; private set; }
        public static Il2CppMemoryStream MusicStream { get; private set; }
        /// <summary>
        /// Load custom from folder or mdm file.
        /// </summary>
        /// <param name="path"></param>
        public Album(string path)
        {
            if (File.Exists($"{path}/info.json"))
            {
                // Load from folder
                this.Info = File.OpenRead($"{path}/info.json").JsonDeserialize<AlbumInfo>();
                this.BasePath = path;
                this.IsPackaged = false;
                verifyMaps();
                return;
            }
            else
            {
                // Load from package
                using (ZipFile zip = ZipFile.Read(path))
                {
                    if (zip["info.json"] != null)
                    {
                        this.Info = zip["info.json"].OpenReader().JsonDeserialize<AlbumInfo>();
                        this.BasePath = path;
                        this.IsPackaged = true;
                        verifyMaps();
                        return;
                    }
                }
            }
            throw new FileNotFoundException($"info.json not found");
        }
        /// <summary>
        /// TODO: Check this difficulty can be play.
        /// </summary>
        /// <returns></returns>
        public bool IsPlayable()
        {
            return true;
        }
        /// <summary>
        /// Get chart hash.
        /// TODO: for custom score.
        /// </summary>
        public void verifyMaps()
        {
            foreach (var mapIdx in Info.GetDifficulties().Keys)
            {
                try
                {
                    using (var stream = Open($"map{mapIdx}.bms"))
                    {
                        availableMaps.Add(mapIdx, stream.ToArray().GetMD5().ToString("x2"));
                    }
                }
                catch (Exception)
                {
                    // Pass
                }
            }
        }
        /// <summary>
        /// Get cover sprite
        /// </summary>
        /// <returns></returns>
        unsafe public Sprite GetCover()
        {
            try {
                using(Stream stream = Open("cover.png")) {
                    var image = Image.Load<Rgba32>(stream);
                    image.Mutate(processor => processor.Flip(FlipMode.Vertical));
                    image.TryGetSinglePixelSpan(out var pixels);

                    CoverTex = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false);

                    fixed(void* pixelsPtr = pixels)
                        CoverTex.LoadRawTextureData((IntPtr)pixelsPtr, pixels.Length * 4);
                    CoverTex.Apply(false, true);

                    Log.Debug($"{CoverTex.width}x{CoverTex.height}");
                }

                CoverSprite = Sprite.Create(CoverTex,
                    new Rect(0, 0, CoverTex.width, CoverTex.height),
                    new Vector2(CoverTex.width / 2, CoverTex.height / 2));
                CoverSprite.name = AlbumManager.GetAlbumKeyByIndex(Index) + "_cover";

                return CoverSprite;
            } catch(FileNotFoundException e) {
                Log.Warning(e.Message);
            } catch(Exception e) {
                Log.Warning("Failed to read cover image: " + e);
            }

            // Empty pixel
            return Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }
        /// <summary>
        /// Get music AudioClip.
        /// </summary>
        /// <param name="name">"music" or "demo"</param>
        /// <returns></returns>
        public AudioClip GetMusic(string name = "music")
        {
            ManagedGeneric.List<string> fileNames = new ManagedGeneric.List<string>();
            foreach (var ext in AudioFormatMapping.Keys)
                fileNames.Add(name + ext);

            if (!TryOpenOne(fileNames, out var fileName, out var buffer))
            {
                Log.Warning($"Not found: {name} from: {BasePath}");
                return null;
            }

            try
            {
                if (!AudioFormatMapping.TryGetValue(Path.GetExtension(fileName), out var format))
                {
                    Log.Warning($"Unknown audio format: {fileName} from: {BasePath}");
                    return null;
                }

                var audioName = $"{Name}_{name}";
                AudioClip audioClip = null;

                switch (format)
                {
                    case AudioFormat.aiff:
                    case AudioFormat.wav:
                        audioClip = Manager.Load(buffer.ToIL2CppStream(), format, audioName);
                        break;
                    case AudioFormat.ogg:
                        audioClip = AsyncBgmManager.BeginAsyncOgg(buffer.ToIL2CppStream(), audioName);
                        break;
                    case AudioFormat.mp3:
                        audioClip = AsyncBgmManager.BeginAsyncMp3(buffer.ToStream(), audioName);
                        break;
                }

                return audioClip;
            }
            catch (Exception ex)
            {
                Log.Error($"Could not load audio {Name}_{name}: {ex}");
            }
            return null;
        }

        

        /// <summary>
        /// Load map.
        /// 1. Load map*.bms.
        /// 2. Convert to MusicData.
        /// 3. Create StageInfo.
        /// </summary>
        /// <param name="index">map index</param>
        /// <returns></returns>
        public StageInfo GetMap(int index)
        {
            try
            {
                using (Stream stream = Open($"map{index}.bms"))
                {
                    /* 1.加载bms
                     * 2.转换为MusicData
                     * 3.创建StageInfo
                     * */

                    var mapName = $"{Name}_map{index}";

                    var bms = BMSCLoader.Load(stream, mapName);
                    if (bms == null)
                    {
                        return null;
                    }

                    MusicConfigReader reader = MusicConfigReader.Instance;
                    reader.ClearData();
                    reader.bms = bms;
                    reader.Init(mapName);

                    var musicDatas = reader.GetData();
                    var il2cppDatas = new Il2CppGeneric.List<MusicData>();
                    foreach(var data in musicDatas) {
                        il2cppDatas.Add(data.Cast<MusicData>());
                    }

                    StageInfo stageInfo = new StageInfo
                    {
                        musicDatas = il2cppDatas,
                        delay = reader.delay,
                        mapName = mapName,
                        music = $"{Name}_music",
                        scene = (string)reader.bms.info["GENRE"],
                        difficulty = index,
                        bpm = reader.bms.GetBpm(),
                        md5 = reader.bms.md5,
                        sceneEvents = reader.sceneEvents,
                        name = Info.name
                    };
                    Log.Debug($"Delay: {reader.delay.ToString()}");
                    return stageInfo;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
            return null;
        }
        /// <summary>
        /// Destory AudioClip instance and close buffer stream.
        /// </summary>
        public static void DestoryAudio()
        {
            if (MusicAudio != null)
            {
                UnityEngine.Object.Destroy(MusicAudio);
                MusicAudio = null;
            }
            if (MusicStream != null)
            {
                MusicStream.Dispose();
                MusicStream = null;
            }
        }
        /// <summary>
        /// Destory Sprite instance and destory Texture2D instance.
        /// </summary>
        public void DestoryCover()
        {
            if (CoverSprite != null)
            {
                Addressables.Release(CoverSprite);
                UnityEngine.Object.Destroy(CoverSprite);
                CoverSprite = null;
            }
            if (CoverTex != null)
            {
                Addressables.Release(CoverTex);
                UnityEngine.Object.Destroy(CoverTex);
                CoverTex = null;
            }
        }
        /// <summary>
        /// Open a streaming from the file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private Stream Open(string filePath)
        {
            if (IsPackaged)
            {
                // load from package
                using (ZipFile zip = ZipFile.Read(BasePath))
                {
                    if (!zip.ContainsEntry(filePath))
                        throw new FileNotFoundException($"File not found: {filePath} in {BasePath}");
                    // ModLogger.Debug($"Loaded:{BasePath}/{filePath}");
                    // CrcCalculatorStream not support set_position, Read all bytes then convert to MemoryStream
                    return zip[filePath].OpenReader().ToArray().ToStream();
                }
            }
            else
            {
                // Load from folder
                var fullPath = Path.Combine(BasePath, filePath);

                if (!File.Exists(fullPath))
                    throw new FileNotFoundException($"File not found: {fullPath}");
                // ModLogger.Debug($"Loaded:{BasePath}/{filePath}");
                return File.OpenRead(fullPath);
            }
        }
        /// <summary>
        /// Open a streaming from the file list returns only the first exist one.
        /// </summary>
        /// <param name="filePaths"></param>
        /// <param name="openedFilePath"></param>
        /// <returns></returns>
        private bool TryOpenOne(ManagedGeneric.IEnumerable<string> filePaths, out string openedFilePath, out byte[] buffer)
        {
            if (IsPackaged)
            {
                // load from package
                using (ZipFile zip = ZipFile.Read(BasePath))
                {
                    foreach (var filePath in filePaths)
                    {
                        if (!zip.ContainsEntry(filePath))
                            continue;
                        openedFilePath = filePath;
                        // CrcCalculatorStream doesn't support set_position. We read all bytes
                        buffer = zip[filePath].OpenReader().ToArray();
                        return true;
                    }
                }
            }
            else
            {
                // Load from folder
                foreach (var filePath in filePaths)
                {
                    var fullPath = Path.Combine(BasePath, filePath);
                    if (!File.Exists(fullPath))
                        continue;
                    openedFilePath = filePath;
                    buffer = File.ReadAllBytes(fullPath);
                    return true;
                }
            }
            openedFilePath = null;
            buffer = null;
            return false;
        }
    }
}
