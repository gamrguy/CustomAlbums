using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnhollowerBaseLib;
using NLayer;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using NVorbis.NAudioSupport;
using UnityEngine;
using Assets.Scripts.UI.Panels.PnlMusicTag;
using Assets.Scripts.UI.Panels;

namespace CustomAlbums
{
    public static class AsyncBgmManager
    {
        public const int ASYNC_READ_SPEED = 4096;
        private static readonly Logger Log = new Logger("AsyncBgmManager");

        private static Coroutine currentRoutine;
        private static Dictionary<string, Coroutine> coroutines = new Dictionary<string, Coroutine>();

        /// <summary>
        /// Attempts to switch the current audio load coroutine to the given audio name.
        /// Returns whether the switch was successful.
        /// </summary>
        /// <param name="audioName"></param>
        /// <returns></returns>
        public static bool TrySwitchLoad(string audioName) {
            if(coroutines.TryGetValue(audioName, out Coroutine routine)) {
                currentRoutine = routine;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Begins asynchronously loading an MP3 file from the given stream and sets itself as the current audio coroutine.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AudioClip BeginAsyncMp3(Stream stream, string name) {
            var mpgFile = new MpegFile(stream);
            var sampleCount = mpgFile.Length / sizeof(float);
            var remaining = sampleCount;
            var index = 0;
            var audioClip = AudioClip.Create(name, (int)sampleCount / mpgFile.Channels, mpgFile.Channels, mpgFile.SampleRate, false);

            if(name.EndsWith("_music") && mpgFile.SampleRate != 44100) {
                Log.Warning($"{name}.mp3 is not 44.1khz, desyncs may occur! Consider switching to 44.1khz or using the .ogg format instead.");
            }

            Coroutine routine = null;
            routine = SingletonMonoBehaviour<CoroutineManager>.instance.StartCoroutine(
                (Il2CppSystem.Action)delegate { },
                (Il2CppSystem.Func<bool>)delegate {
                    // Stop if the asset is unloaded during read
                    if(audioClip == null) {
                        coroutines.Remove(name);
                        if(currentRoutine == routine) currentRoutine = null;
                        Log.Debug($"Aborting async load of {name}.mp3");
                        return true;
                    }

                    // Pause when not the current routine
                    if(currentRoutine != routine) return false;

                    var sampArr = new float[Math.Min(ASYNC_READ_SPEED, remaining)];
                    var readCount = mpgFile.ReadSamples(sampArr, 0, sampArr.Length);

                    audioClip.SetData(sampArr, index / mpgFile.Channels);

                    index += readCount;
                    remaining -= readCount;
                    if(remaining <= 0 || readCount == 0) {
                        stream.Dispose();
                        Log.Debug($"Finished async read of {name}.mp3");
                        currentRoutine = null;
                        coroutines.Remove(name);
                        return true;
                    }

                    return false;
                });
            currentRoutine = routine;
            coroutines[name] = routine;

            return audioClip;
        }

        /// <summary>
        /// Begins asynchronously loading an OGG file from the given stream and sets itself as the current audio coroutine.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AudioClip BeginAsyncOgg(Il2CppSystem.IO.Stream stream, string name) {
            var waveStream = new VorbisWaveReader(stream);
            var sampleCount = (int)(waveStream.Length / (waveStream.WaveFormat.BitsPerSample / 8));
            var remaining = sampleCount;
            var index = 0;
            var audioClip = AudioClip.Create(name, sampleCount / waveStream.WaveFormat.Channels, waveStream.WaveFormat.Channels, waveStream.WaveFormat.SampleRate, false);
            //Log.Info($"Name: {name} | SampleCount: {sampleCount} | Channels: {waveStream.WaveFormat.Channels} | SampleRate: {waveStream.WaveFormat.SampleRate} | BytesPerSample: {waveStream.WaveFormat.BitsPerSample/8}");
            Coroutine routine = null;
            routine = SingletonMonoBehaviour<CoroutineManager>.instance.StartCoroutine(
                (Il2CppSystem.Action)delegate { },
                (Il2CppSystem.Func<bool>)delegate {
                    // Stop if the asset is unloaded during read
                    if(audioClip == null) {
                        coroutines.Remove(name);
                        if(currentRoutine == routine) currentRoutine = null;
                        Log.Debug($"Aborting async load of {name}.ogg");
                        return true;
                    }

                    // Pause when not the current routine
                    if(currentRoutine != routine) return false;

                    var dataSet = new Il2CppStructArray<float>(Math.Min(ASYNC_READ_SPEED, remaining));
                    var readCount = waveStream.Read(dataSet, 0, dataSet.Length);
                    
                    try {
                        audioClip.SetData(dataSet, index / waveStream.WaveFormat.Channels);
                    } catch(Exception e) {
                        if(((double)waveStream.Position)/1000 > waveStream.Length) {
                            Log.Warning("Possible over-read of audio file, this is a file-dependent anomaly. Consider making a minimal edit and re-saving.");
                        }
                        Log.Error($"Exception while reading at offset {index} of {((double)waveStream.Position)/1000}/{waveStream.Length} in {name}, aborting: {e.Message}");
                        waveStream.Dispose();
                        currentRoutine = null;
                        coroutines.Remove(name);
                        return true;
                    }

                    index += readCount;
                    remaining -= readCount;
                    if(remaining <= 0 || readCount == 0) {
                        waveStream.Dispose();
                        Log.Debug($"Finished async read of {name}.ogg");
                        currentRoutine = null;
                        coroutines.Remove(name);
                        return true;
                    }

                    return false;
                });
            currentRoutine = routine;
            coroutines[name] = routine;

            return audioClip;
        }
    }
}
