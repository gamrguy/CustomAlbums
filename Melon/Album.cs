using Assets.Scripts.GameCore;
using CustomAlbums.Data;
using Ionic.Zip;
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
using Assets.Scripts.Database;
using Assets.Scripts.Structs;

namespace CustomAlbums
{

    public class Album
    {
        private static readonly Logger Log = new Logger(nameof(Album));
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
        public Sprite CoverSprite { get => CoverSpriteFrames[0]; }
        public Sprite[] CoverSpriteFrames { get; private set; }
        public int CoverFrameRateMs { get; private set; }
        public static AudioClip MusicAudio { get; private set; }
        public static Il2CppMemoryStream MusicStream { get; private set; }

        /// <summary>
        /// Load custom from folder or mdm file.
        /// </summary>
        /// <param name="path"></param>
        public Album(string path) {
            if(File.Exists($"{path}/info.json")) {
                // Load from folder
                Info = File.OpenRead($"{path}/info.json").JsonDeserialize<AlbumInfo>();
                BasePath = path;
                IsPackaged = false;
                verifyMaps();
                return;
            } else {
                // Load from package
                using(ZipFile zip = ZipFile.Read(path)) {
                    if(zip["info.json"] != null) {
                        Info = zip["info.json"].OpenReader().JsonDeserialize<AlbumInfo>();
                        BasePath = path;
                        IsPackaged = true;
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
        public bool IsPlayable() => true;

        /// <summary>
        /// Get chart hash.
        /// TODO: for custom score.
        /// </summary>
        public void verifyMaps() {
            foreach(var mapIdx in Info.GetDifficulties().Keys) {
                try {
                    using(var stream = Open($"map{mapIdx}.bms")) {
                        availableMaps.Add(mapIdx, stream.ToArray().GetMD5().ToString("x2"));
                    }
                } catch(Exception) {
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
                int frames = 0;
                using(Stream stream = Open("cover.png")) {
                    var image = Image.Load<Rgba32>(stream);
                    image.Mutate(processor => processor.Flip(FlipMode.Vertical));
                    frames = image.Frames.Count;
                    CoverTex = new Texture2D(image.Width, image.Height * frames, TextureFormat.RGBA32, false);

                    Image<Rgba32> outImage;

                    if(frames > 1) {
                        outImage = new Image<Rgba32>(image.Width, image.Height * frames);
                        for(var i = 0; i < frames; i++) {
                            image.Frames[i].TryGetSinglePixelSpan(out var px);
                            var pxArr = px.ToArray();
                            outImage.Mutate(processor => processor.DrawImage(Image.LoadPixelData(pxArr, image.Width, image.Height), new Point(0, image.Height * i), 1));
                        }
                    } else {
                        outImage = image;
                    }

                    if(frames > 1) {
                        CoverFrameRateMs = image.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay * 10;
                    } else {
                        CoverFrameRateMs = 1000;
                    }

                    outImage.TryGetSinglePixelSpan(out var pixels);
                    Log.Debug(pixels.Length);
                    fixed(void* pixelsPtr = pixels)
                        CoverTex.LoadRawTextureData((IntPtr)pixelsPtr, pixels.Length * 4);
                    CoverTex.Apply(false, true);

                    Log.Debug($"{CoverTex.width}x{CoverTex.height}");
                }

                CoverSpriteFrames = new Sprite[frames];
                for(var i = 0; i < frames; i++) {
                    CoverSpriteFrames[i] = Sprite.Create(CoverTex,
                        new Rect(0, (CoverTex.height / frames) * i, CoverTex.width, CoverTex.height / frames),
                        new Vector2(CoverTex.width / 2, CoverTex.height / 2 / frames)
                    );
                    CoverSpriteFrames[i].name = AlbumManager.GetAlbumKeyByIndex(Index) + "_cover_" + i;
                    CoverSpriteFrames[i].hideFlags |= HideFlags.DontUnloadUnusedAsset;
                }

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
        public AudioClip GetMusic(string name = "music") {
            ManagedGeneric.List<string> fileNames = new ManagedGeneric.List<string>();
            foreach(var ext in AudioFormatMapping.Keys)
                fileNames.Add(name + ext);

            if(!TryOpenOne(fileNames, out var fileName, out var buffer)) {
                Log.Warning($"Not found: {name} from: {BasePath}");
                return null;
            }

            try {
                if(!AudioFormatMapping.TryGetValue(Path.GetExtension(fileName), out var format)) {
                    Log.Warning($"Unknown audio format: {fileName} from: {BasePath}");
                    return null;
                }

                var audioName = $"{Name}_{name}";
                AudioClip audioClip = null;

                switch(format) {
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
            } catch(Exception ex) {
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
        public StageInfo GetMap(int index) {
            try {
                using(Stream stream = Open($"map{index}.bms")) {
                    var mapName = $"{Name}_map{index}";

                    var bms = BMSCLoader.Load(stream, mapName);
                    if(bms == null) {
                        return null;
                    }

                    var stageInfo = ScriptableObject.CreateInstance<StageInfo>();
                    stageInfo.mapName = mapName;

                    // Try loading talk file
                    try {
                        using(Stream talkStream = Open($"map{index}.talk")) {
                            stageInfo.dialogEvents = new StreamReader(talkStream).ReadToEnd().IL2CppJsonDeserialize<Il2CppGeneric.Dictionary<string, Il2CppGeneric.List<GameDialogArgs>>>();
                        }
                    } catch(FileNotFoundException) {
                    } catch(Exception e) {
                        Log.Error(e);
                    }

                    GlobalDataBase.dbStageInfo.SetStageInfo(stageInfo);

                    // Sets stageInfo.musicDatas and stageInfo.delay
                    BMSCLoader.TransmuteData(bms, stageInfo);
                    
                    stageInfo.music = $"{Name}_music";
                    stageInfo.scene = (string)bms.info["GENRE"];
                    stageInfo.difficulty = index;
                    stageInfo.bpm = bms.GetBpm();
                    stageInfo.md5 = bms.md5;
                    stageInfo.sceneEvents = bms.GetSceneEvents();
                    stageInfo.name = Info.name;

                    return stageInfo;
                }
            } catch(Exception ex) {
                Log.Error(ex);
            }
            return null;
        }

        /// <summary>
        /// Destroy AudioClip instance and close buffer stream.
        /// </summary>
        public static void DestroyAudio() {
            if(MusicAudio != null) {
                UnityEngine.Object.Destroy(MusicAudio);
                MusicAudio = null;
            }
            if(MusicStream != null) {
                MusicStream.Dispose();
                MusicStream = null;
            }
        }

        /// <summary>
        /// Destroy Sprite instance and destroy Texture2D instance.
        /// </summary>
        public void DestroyCover() {
            if(CoverSprite != null) {
                Addressables.Release(CoverSprite);
                UnityEngine.Object.Destroy(CoverSprite);
                //CoverSprite = null;
                CoverSpriteFrames = null;
            }
            if(CoverTex != null) {
                Addressables.Release(CoverTex);
                UnityEngine.Object.Destroy(CoverTex);
                CoverTex = null;
            }
        }

        /// <summary>
        /// Open Stream from file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private Stream Open(string filePath) {
            if(IsPackaged) {
                // Load from package
                using(ZipFile zip = ZipFile.Read(BasePath)) {
                    if(!zip.ContainsEntry(filePath))
                        throw new FileNotFoundException($"File not found: {filePath} in {BasePath}");
                    // CrcCalculatorStream not support set_position, Read all bytes then convert to MemoryStream
                    return zip[filePath].OpenReader().ToArray().ToStream();
                }
            } else {
                // Load from folder
                var fullPath = Path.Combine(BasePath, filePath);

                if(!File.Exists(fullPath))
                    throw new FileNotFoundException($"File not found: {fullPath}");
                return File.OpenRead(fullPath);
            }
        }

        /// <summary>
        /// Open Stream from first existing file in list.
        /// </summary>
        /// <param name="filePaths"></param>
        /// <param name="openedFilePath"></param>
        /// <returns></returns>
        private bool TryOpenOne(ManagedGeneric.IEnumerable<string> filePaths, out string openedFilePath, out byte[] buffer) {
            if(IsPackaged) {
                // load from package
                using(ZipFile zip = ZipFile.Read(BasePath)) {
                    foreach(var filePath in filePaths) {
                        if(!zip.ContainsEntry(filePath))
                            continue;
                        openedFilePath = filePath;
                        // CrcCalculatorStream doesn't support set_position. We read all bytes
                        buffer = zip[filePath].OpenReader().ToArray();
                        return true;
                    }
                }
            } else {
                // Load from folder
                foreach(var filePath in filePaths) {
                    var fullPath = Path.Combine(BasePath, filePath);
                    if(!File.Exists(fullPath))
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
