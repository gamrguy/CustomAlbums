using Account;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.PeroTools.Nice.Datas;
using Assets.Scripts.PeroTools.Nice.Interface;
using Assets.Scripts.PeroTools.Platforms;
using Assets.Scripts.PeroTools.Platforms.Steam;
using Assets.Scripts.UI.Panels;
using FormulaBase;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using Il2CppSystem.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Ionic.Zip;

namespace CustomAlbums.Patch
{
    public static class SavesPatch
    {
        private static readonly Logger Log = new Logger("SavesPatch");
        public static string BackupPath => Path.Combine(Directory.GetCurrentDirectory(), "UserData/saves_backup");
        public static string BackupVanilla => Path.Combine(BackupPath, "bkp-Vanilla.sav");
        public static string BackupVanillaDebug => Path.Combine(BackupPath, "bkp-Vanilla-debug.json");
        public static string BackupCustom => Path.Combine(BackupPath, "bkp-CustomAlbums.json");
        public static string BackupZip => Path.Combine(BackupPath, "backups.zip");
        public static TimeSpan MaxBackupTime => TimeSpan.FromDays(30);

        public static readonly System.Collections.Generic.List<Type> ISyncTypes = new System.Collections.Generic.List<Type> {
            typeof(SteamSync)
        };

        public static void DoPatching(HarmonyLib.Harmony harmony)
        {
            MethodInfo method;
            MethodInfo methodPrefix;
            MethodInfo methodPostfix;
            
            // StageBattleComponent.Exit
            method = AccessTools.Method(typeof(StageBattleComponent), "Exit");
            methodPrefix = AccessTools.Method(typeof(SavesPatch), "BattleExitPrefix");
            harmony.Patch(method, prefix: new HarmonyMethod(methodPrefix));
            // XDSDKManager.OnSaveSelectCallback
            method = AccessTools.Method(typeof(GameAccountSystem), "OnSaveSelectCallback");
            methodPrefix = AccessTools.Method(typeof(SavesPatch), "OnSaveSelectCallbackPrefix");
            methodPostfix = AccessTools.Method(typeof(SavesPatch), "OnSaveSelectCallbackPostfix");
            harmony.Patch(method, prefix: new HarmonyMethod(methodPrefix), postfix: new HarmonyMethod(methodPostfix));
            // XDSDKManager.RefreshDatas
            method = AccessTools.Method(typeof(GameAccountSystem), "RefreshDatas");
            methodPrefix = AccessTools.Method(typeof(SavesPatch), "RefreshDatasPrefix");
            harmony.Patch(method, prefix: new HarmonyMethod(methodPrefix));
            // ISync implement
            foreach (var ISyncImpl in ISyncTypes)
            {
                // ISync.SaveLocal
                method = AccessTools.Method(ISyncImpl, "SaveLocal");    
                methodPrefix = AccessTools.Method(typeof(SavesPatch), "ISyncSaveLocalPrefix");
                methodPostfix = AccessTools.Method(typeof(SavesPatch), "ISyncSaveLocalPostfix");
                harmony.Patch(method, prefix: new HarmonyMethod(methodPrefix), postfix: new HarmonyMethod(methodPostfix));
                // ISync.LoadLocal
                method = AccessTools.Method(ISyncImpl, "LoadLocal");
                // methodPrefix = AccessTools.Method(typeof(SavesPatch), "ISyncLoadLocalPrefix");
                methodPostfix = AccessTools.Method(typeof(SavesPatch), "ISyncLoadLocalPostfix");
                harmony.Patch(method, postfix: new HarmonyMethod(methodPostfix));
            }
        }
        /// <summary>
        /// Remove custom data before saving.
        /// </summary>
        public static void ISyncSaveLocalPrefix()
        {
            try {
                SaveManager.SplitCustomData();
                SaveManager.Save();
                SaveManager.CleanCustomData();
            } catch(Exception e) {
                Log.Error(e);
            }
        }
        /// <summary>
        /// Restore custom data after saving.
        /// </summary>
        public static void ISyncSaveLocalPostfix()
        {
            try {
                SaveManager.RestoreCustomData();
            } catch(Exception e) {
                Log.Error(e);
            }
        }
        /// <summary>
        /// Restore custom data after save file loaded.
        /// </summary>
        public static void ISyncLoadLocalPostfix()
        {
            try {
                Backup();

                SaveManager.CleanCustomData();
                SaveManager.RestoreCustomData();
            } catch(Exception e) {
                Log.Error(e);
            }
        }
        /// <summary>
        /// Upgrade NoPollution data and restore custom data after cloud synchronization
        /// </summary>
        public static void RefreshDatasPrefix()
        {
            try {
                Backup();

                SaveManager.SplitCustomData();
                SaveManager.Save();

                SaveManager.CleanCustomData();
                SaveManager.RestoreCustomData();
            } catch(Exception e) {
                Log.Error(e);
            }
        }

        /// <summary>
        /// On battle end.
        /// </summary>
        /// <param name="sceneName"></param>
        /// <param name="calllback"></param>
        /// <param name="withBack"></param>
        public static void BattleExitPrefix(ref string sceneName, ref Il2CppSystem.Action calllback, ref bool backToPnlStage)
        {
            string result = Singleton<DataManager>.instance["Account"]["SelectedMusicUid"].GetResult<string>();

            if (result.StartsWith($"{AlbumManager.Uid}-"))
            {
                Log.Debug($"Game/Finish sceneName:{sceneName} withBack:{backToPnlStage} SelectedMusicUid:{result}");
            }
        }


        public static void OnSaveSelectCallbackPrefix(ref bool isLocal)
        {
            if (isLocal)
            {
                SaveManager.SplitCustomData();
                SaveManager.Save();
                SaveManager.CleanCustomData();
            }
        }
        public static void OnSaveSelectCallbackPostfix(ref bool isLocal)
        {
            if (isLocal)
            {
                SaveManager.RestoreCustomData();
            }
        }

        public static void Backup()
        {
            Directory.CreateDirectory(BackupPath);

            CompressBackups();
            CreateBackup(BackupVanilla, Singleton<DataManager>.instance.ToBytes());
            CreateBackup(BackupVanillaDebug, ToJsonDict(Singleton<DataManager>.instance.datas).JsonSerialize());
            CreateBackup(BackupCustom, SaveManager.CustomData.JsonSerialize());
            ClearOldBackups();
        }

        private static void CreateBackup(string filePath, object data) {
            try {
                if(data == null) {
                    Log.Warning("Could not create backup of null data!");
                    return;
                }

                var wroteFile = false;
                if(data is string str) {
                    File.WriteAllText(filePath, str);
                    wroteFile = true;
                } else if(data is byte[] bytes) {
                    File.WriteAllBytes(filePath, bytes);
                    wroteFile = true;
                } else if(data is UnhollowerBaseLib.Il2CppStructArray<byte> ilBytes) {
                    File.WriteAllBytes(filePath, ilBytes);
                    wroteFile = true;
                } else {
                    Log.Warning("Could not create backup for unsupported data type " + data.GetType().FullName);
                }

                if(wroteFile) Log.Debug($"Saved backup: {filePath}");
            } catch(Exception e) {
                Log.Error("Backup failed: " + e);
            }
        }

        private static void ClearOldBackups() {
            try {
                var backups = Directory.EnumerateFiles(BackupPath).ToList();
                foreach(var backup in backups) {
                    var bkpDate = Directory.GetLastWriteTime(backup);
                    if((DateTime.Now - bkpDate).Duration() > MaxBackupTime.Duration()) {
                        Log.Debug("Removing old backup: " + backup);
                        File.Delete(backup);
                    }
                }

                if(File.Exists(BackupZip)) {
                    var zip = ZipFile.Read(BackupZip);
                    var needsSave = false;
                    foreach(var entry in zip.Entries.ToList()) {
                        if((DateTime.Now - entry.CreationTime).Duration() > MaxBackupTime.Duration()) {
                            Log.Debug("Removing compressed old backup: " + entry.FileName);
                            zip.RemoveEntry(entry);
                            needsSave = true;
                        }
                    }
                    if(needsSave) zip.Save();
                }
            } catch(Exception e) {
                Log.Error("Clearing old backups failed: " + e);
            }
        }

        private static void CompressBackups() {
            try {
                ZipFile zip = null;
                if(!File.Exists(BackupZip)) {
                    zip = new ZipFile(BackupZip);
                } else {
                    zip = ZipFile.Read(BackupZip);
                }

                var files = Directory.EnumerateFiles(BackupPath).Where((fn) => !(Path.GetExtension(fn) == ".zip"));
                if(files.Count() > 0) {
                    zip.AddFiles(files);
                    foreach(var filename in files) {
                        var creationTime = zip[filename].CreationTime.ToString("yyyy_MM_dd_H_mm_ss-");
                        var onlyFileName = Path.GetFileName(zip[filename].FileName);
                        var newFileName = creationTime + onlyFileName;
                        int i = 1;
                        while(zip[newFileName] != null) {
                            newFileName = creationTime + $"{i++}-" + onlyFileName;
                        }
                        zip[filename].FileName = newFileName;
                    }
                    zip.Save();
                }

                foreach(var filename in files) {
                    File.Delete(filename);
                }
            } catch(Exception e) {
                Log.Error("Compressing previous backups failed: " + e);
            }
        }

        /// <summary>
        /// IData dict to JObject dict
        /// </summary>
        /// <param name="datas"></param>
        /// <returns></returns>
        public static System.Collections.Generic.Dictionary<string, JObject> ToJsonDict(Dictionary<string, IData> datas)
        {
            var dictionary = new System.Collections.Generic.Dictionary<string, JObject>();
            foreach (KeyValuePair<string, IData> keyValuePair in datas)
            {
                SingletonDataObject singletonDataObject = keyValuePair.Value?.TryCast<SingletonDataObject>();
                if (singletonDataObject != null)
                {
                    dictionary.Add(keyValuePair.Key, singletonDataObject.ToJson().JsonDeserialize<JObject>());
                }
            }
            return dictionary;
        }
    }
}