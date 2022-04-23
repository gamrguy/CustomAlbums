using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Nice.Datas;
using NiceData = Assets.Scripts.PeroTools.Nice.Datas;
using Assets.Scripts.PeroTools.Nice.Interface;
using CustomAlbums.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CustomAlbums
{
    public static class SaveManager
    {
        private static readonly Logger Log = new Logger("SaveManager");
        public static string OldFilePath => Path.Combine(Directory.GetCurrentDirectory(), "Mods/CustomAlbums.json");
        public static string FilePath => Path.Combine(Directory.GetCurrentDirectory(), "UserData/CustomAlbums.json");

        public static CustomData CustomData = new CustomData();

        public static void Save()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), FilePath);
            File.WriteAllText(FilePath, CustomData.JsonSerialize());
        }

        public static void Load()
        {
            // Relocate old save files
            if(File.Exists(OldFilePath) && !File.Exists(FilePath)) {
                File.Move(OldFilePath, FilePath);
            }

            var couldLoadSave = false;
            if(File.Exists(FilePath)) {
                try {
                    CustomData = File.ReadAllText(FilePath).JsonDeserialize<CustomData>();
                    couldLoadSave = true;
                } catch(Exception e) {
                    Log.Error("Error while loading save, loading backup if available!\n" + e);
                    CustomData = new CustomData();
                }
            }

            if(!couldLoadSave && File.Exists(Patch.SavesPatch.BackupCustom)) {
                try {
                    CustomData = File.ReadAllText(Patch.SavesPatch.BackupCustom).JsonDeserialize<CustomData>();
                } catch(Exception e) {
                    Log.Error("Could not load backup! Your save is probably dead now.\n" + e);
                    CustomData = new CustomData();
                }
            }
        }

        public static void SplitCustomData()
        {
            var GameAccount = Singleton<DataManager>.instance["Account"];
            var GameAchievement = Singleton<DataManager>.instance["Achievement"];
            var GameSelectedIndex = GameAccount["SelectedMusicIndex"].GetResult<int>();
            var GameSelectedDifficulty = GameAccount["SelectedDifficulty"].GetResult<int>();
            var GameSelectedMusicPack = GameAccount["SelectedAlbumUid"].GetResult<string>();
            var GameCollections = GameAccount["Collections"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHides = GameAccount["Hides"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHistory = GameAccount["History"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHighest = GameAchievement["highest"].GetResult<Il2CppSystem.Collections.Generic.List<IData>>();
            var GameFailCount = GameAchievement["fail_count"].GetResult<Il2CppSystem.Collections.Generic.List<IData>>();
            var GameEasyPass = GameAchievement["easy_pass"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHardPass = GameAchievement["hard_pass"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameMasterPass = GameAchievement["master_pass"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameFullComboMusic = GameAchievement["full_combo_music"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();

            // Account.SelectedAlbumUid, Account.SelectedMusicIndex, Account.SelectedDifficulty
            CustomData.SelectedAlbum = null;
            CustomData.SelectedDifficulty = 2;
            if (AlbumManager.MusicPackge == GameSelectedMusicPack)
            {
                CustomData.SelectedAlbum = AlbumManager.GetAlbumKeyByIndex(GameSelectedIndex);
                CustomData.SelectedDifficulty = GameSelectedDifficulty;
            }
            // Account.Collections
            CustomData.Collections.Clear();
            foreach (var uid in GameCollections.FindAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-"))))
            {
                var albumIndex = int.Parse(uid.RemoveFromStart($"{AlbumManager.Uid}-"));
                var albumKey = AlbumManager.GetAlbumKeyByIndex(albumIndex);

                if (AssertAlbumKeyIndex(albumKey, albumIndex))
                    CustomData.Collections.Add(albumKey);
            }
            // Account.Hide
            CustomData.Hides.Clear();
            foreach (var uid in GameHides.FindAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-"))))
            {
                var albumIndex = int.Parse(uid.RemoveFromStart($"{AlbumManager.Uid}-"));
                var albumKey = AlbumManager.GetAlbumKeyByIndex(albumIndex);

                if (AssertAlbumKeyIndex(albumKey, albumIndex))
                    CustomData.Hides.Add(albumKey);
            }
            // Account.History
            CustomData.History.Clear();
            foreach (var uid in GameHistory.FindAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-"))))
            {
                var albumIndex = int.Parse(uid.RemoveFromStart($"{AlbumManager.Uid}-"));
                var albumKey = AlbumManager.GetAlbumKeyByIndex(albumIndex);

                if (AssertAlbumKeyIndex(albumKey, albumIndex))
                    CustomData.History.Add(albumKey);
            }
            // Achievement.highest
            foreach (var highest in GameHighest.FindAll((Il2CppSystem.Predicate<IData>)(d => d["uid"].GetResult<string>().StartsWith($"{AlbumManager.Uid}-"))))
            {
                var dUid = highest["uid"].GetResult<string>();
                var albumIndex = int.Parse(dUid.RemoveFromStart($"{AlbumManager.Uid}-").Split('_')[0]);
                var difficulty = int.Parse(dUid.RemoveFromStart($"{AlbumManager.Uid}-").Split('_')[1]);
                var albumKey = AlbumManager.GetAlbumKeyByIndex(albumIndex);

                if (!AssertAlbumKeyIndex(albumKey, albumIndex))
                    continue;

                var failData = GameFailCount.Find((Il2CppSystem.Predicate<IData>)(d => d["uid"].GetResult<string>() == dUid));
                var failCount = failData?["count"].GetResult<int>() ?? 0;
                var passed = false;
                switch (difficulty)
                {
                    case 1:
                        passed = GameEasyPass.Contains(dUid);
                        break;
                    case 2:
                        passed = GameHardPass.Contains(dUid);
                        break;
                    case 3:
                    case 4:
                        passed = GameMasterPass.Contains(dUid);
                        break;
                }
                var score = new CustomScore()
                {
                    Evaluate = highest["evaluate"].GetResult<int>(),
                    Score = highest["score"].GetResult<int>(),
                    Combo = highest["combo"].GetResult<int>(),
                    Accuracy = highest["accuracy"].GetResult<float>(),
                    AccuracyStr = highest["accuracyStr"].GetResult<string>(),
                    Clear = highest["clear"].GetResult<float>(),
                    FailCount = failCount,
                    Passed = passed
                };

                // New
                if (!CustomData.Highest.ContainsKey(albumKey))
                    CustomData.Highest.Add(albumKey, new Dictionary<int, CustomScore>());
                // Overwrite
                if (CustomData.Highest[albumKey].ContainsKey(difficulty))
                    CustomData.Highest[albumKey][difficulty] = score;
                // Append
                else
                    CustomData.Highest[albumKey].Add(difficulty, score);
            }
            // Achievement.full_combo_music
            foreach (var dUid in GameFullComboMusic.FindAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-"))))
            {
                var albumIndex = int.Parse(dUid.RemoveFromStart($"{AlbumManager.Uid}-").Split('_')[0]);
                var difficulty = int.Parse(dUid.RemoveFromStart($"{AlbumManager.Uid}-").Split('_')[1]);
                var albumKey = AlbumManager.GetAlbumKeyByIndex(albumIndex);

                if (!AssertAlbumKeyIndex(albumKey, albumIndex))
                    continue;

                // New
                if (!CustomData.FullCombo.ContainsKey(albumKey))
                    CustomData.FullCombo.Add(albumKey, new List<int>());
                // Append
                if (!CustomData.FullCombo[albumKey].Contains(difficulty))
                    CustomData.FullCombo[albumKey].Add(difficulty);
            }
        }

        public static void CleanCustomData()
        {
            var GameAccount = Singleton<DataManager>.instance["Account"];
            var GameAchievement = Singleton<DataManager>.instance["Achievement"];
            var GameSelectedIndex = GameAccount["SelectedMusicIndex"];
            var GameSelectedDifficulty = GameAccount["SelectedDifficulty"];
            var GameSelectedMusicPack = GameAccount["SelectedAlbumUid"];
            var GameSelectedMusicUidFromInfoList = GameAccount["SelectedMusicUidFromInfoList"];

            var GameCollections = GameAccount["Collections"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHides = GameAccount["Hides"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHistory = GameAccount["History"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHighest = GameAchievement["highest"].GetResult<Il2CppSystem.Collections.Generic.List<IData>>();
            var GameFailCount = GameAchievement["fail_count"].GetResult<Il2CppSystem.Collections.Generic.List<IData>>();
            var GameEasyPass = GameAchievement["easy_pass"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHardPass = GameAchievement["hard_pass"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameMasterPass = GameAchievement["master_pass"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameFullComboMusic = GameAchievement["full_combo_music"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();

            if (AlbumManager.MusicPackge == GameSelectedMusicPack.GetResult<string>())
            {
                GameSelectedMusicPack.SetResult("music_package_0");
                GameSelectedIndex.SetResult(new Il2CppSystem.Int32() { m_value = 0 }.BoxIl2CppObject());
                GameSelectedDifficulty.SetResult(new Il2CppSystem.Int32() { m_value = 2 }.BoxIl2CppObject());
            }
            if (GameSelectedMusicUidFromInfoList.GetResult<string>().StartsWith($"{AlbumManager.Uid}-"))
            {
                GameSelectedMusicUidFromInfoList.SetResult("0-0");
            }
            GameCollections.RemoveAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-")));
            GameHides.RemoveAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-")));
            GameHistory.RemoveAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-")));
            GameHighest.RemoveAll((Il2CppSystem.Predicate<IData>)(d => d["uid"].GetResult<string>().StartsWith($"{AlbumManager.Uid}-")));
            GameFailCount.RemoveAll((Il2CppSystem.Predicate<IData>)(d => d["uid"].GetResult<string>().StartsWith($"{AlbumManager.Uid}-")));
            GameEasyPass.RemoveAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-")));
            GameHardPass.RemoveAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-")));
            GameMasterPass.RemoveAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-")));
            GameFullComboMusic.RemoveAll((Il2CppSystem.Predicate<string>)(s => s.StartsWith($"{AlbumManager.Uid}-")));
        }

        public static void RestoreCustomData()
        {
            var GameAccount = Singleton<DataManager>.instance["Account"];
            var GameAchievement = Singleton<DataManager>.instance["Achievement"];
            var GameSelectedIndex = GameAccount["SelectedMusicIndex"];
            var GameSelectedDifficulty = GameAccount["SelectedDifficulty"];
            var GameSelectedMusicPack = GameAccount["SelectedAlbumUid"];
            var GameSelectedMusicUidFromInfoList = GameAccount["SelectedMusicUidFromInfoList"];

            var GameCollections = GameAccount["Collections"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHides = GameAccount["Hides"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHistory = GameAccount["History"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHighest = GameAchievement["highest"].GetResult<Il2CppSystem.Collections.Generic.List<IData>>();
            var GameFailCount = GameAchievement["fail_count"].GetResult<Il2CppSystem.Collections.Generic.List<IData>>();
            var GameEasyPass = GameAchievement["easy_pass"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameHardPass = GameAchievement["hard_pass"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameMasterPass = GameAchievement["master_pass"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();
            var GameFullComboMusic = GameAchievement["full_combo_music"].GetResult<Il2CppSystem.Collections.Generic.List<string>>();

            // Account.SelectedAlbumUid, Account.SelectedMusicIndex, Account.SelectedDifficulty
            if (!string.IsNullOrEmpty(CustomData.SelectedAlbum) && AlbumManager.LoadedAlbums.ContainsKey(CustomData.SelectedAlbum))
            {
                var albumIndex = AlbumManager.LoadedAlbums[CustomData.SelectedAlbum].Index;

                GameSelectedMusicPack.SetResult(AlbumManager.MusicPackge);
                GameSelectedIndex.SetResult(new Il2CppSystem.Int32() { m_value = albumIndex }.BoxIl2CppObject());
                GameSelectedDifficulty.SetResult(new Il2CppSystem.Int32() { m_value = CustomData.SelectedDifficulty }.BoxIl2CppObject());
                GameSelectedMusicUidFromInfoList.SetResult($"{AlbumManager.Uid}-{albumIndex}");
            }
            // Account.Collections
            foreach (var albumKey in CustomData.Collections)
            {
                if (!AlbumManager.LoadedAlbums.ContainsKey(albumKey))
                {
                    Log.Debug($"[Collections] Album \"{albumKey}\" not found, cannot restore.");
                    continue;
                }
                var albumIndex = AlbumManager.LoadedAlbums[albumKey].Index;

                var value = $"{AlbumManager.Uid}-{albumIndex}";
                GameCollections.Add(value);
            }
            // Account.Hide
            foreach (var albumKey in CustomData.Hides)
            {
                if (!AlbumManager.LoadedAlbums.ContainsKey(albumKey))
                {
                    Log.Debug($"[Hides] Album \"{albumKey}\" not found, cannot restore.");
                    continue;
                }
                var albumIndex = AlbumManager.LoadedAlbums[albumKey].Index;

                var value = $"{AlbumManager.Uid}-{albumIndex}";
                GameHides.Add(value);
            }
            // Account.History
            foreach (var albumKey in CustomData.History)
            {
                if (!AlbumManager.LoadedAlbums.ContainsKey(albumKey))
                {
                    Log.Debug($"[History] Album \"{albumKey}\" not found, cannot restore.");
                    continue;
                }
                var albumIndex = AlbumManager.LoadedAlbums[albumKey].Index;

                var value = $"{AlbumManager.Uid}-{albumIndex}";
                GameHistory.Add(value);
            }
            // Achievement.highest, Achievement.fail_count, Achievement.easy_pass, Achievement.hard_pass, Achievement.master_pass
            foreach (var highest in CustomData.Highest)
            {
                var albumKey = highest.Key;
                if (!AlbumManager.LoadedAlbums.ContainsKey(albumKey))
                {
                    Log.Debug($"[Highest] Album \"{albumKey}\" not found, cannot restore.");
                    continue;
                }
                var albumIndex = AlbumManager.LoadedAlbums[albumKey].Index;

                foreach (var record in highest.Value)
                {
                    var difficulty = record.Key;
                    var score = record.Value;
                    var dUid = $"{AlbumManager.Uid}-{albumIndex}_{difficulty}";

                    // Achievement.highest
                    var data = new NiceData.Data();
                    data["uid"].SetResult(dUid);
                    data["evaluate"].SetResult(new Il2CppSystem.Int32() { m_value = score.Evaluate }.BoxIl2CppObject());
                    data["score"].SetResult(new Il2CppSystem.Int32() { m_value = score.Score }.BoxIl2CppObject());
                    data["combo"].SetResult(new Il2CppSystem.Int32() { m_value = score.Combo }.BoxIl2CppObject());
                    data["accuracy"].SetResult(new Il2CppSystem.Single() { m_value = score.Accuracy }.BoxIl2CppObject());
                    data["accuracyStr"].SetResult(score.AccuracyStr);
                    data["clear"].SetResult(new Il2CppSystem.Single() { m_value = score.Clear }.BoxIl2CppObject());
                    GameHighest.Add(data.Cast<IData>());

                    // Achievement.fail_count
                    data = new NiceData.Data();
                    data["uid"].SetResult(dUid);
                    data["count"].SetResult(new Il2CppSystem.Int32() { m_value = score.FailCount }.BoxIl2CppObject());
                    GameFailCount.Add(data.Cast<IData>());

                    // Achievement.easy_pass, Achievement.hard_pass, Achievement.master_pass
                    switch (difficulty)
                    {
                        case 1:
                            GameEasyPass.Add(dUid);
                            break;
                        case 2:
                            GameHardPass.Add(dUid);
                            break;
                        case 3:
                        case 4:
                            GameMasterPass.Add(dUid);
                            break;
                    }
                }
            }
            // Achievement.full_combo_music
            foreach (var fullComboMusic in CustomData.FullCombo)
            {
                var albumKey = fullComboMusic.Key;
                if (!AlbumManager.LoadedAlbums.ContainsKey(albumKey))
                {
                    Log.Debug($"[full_combo_music] Album \"{albumKey}\" not found, cannot restore.");
                    continue;
                }
                var albumIndex = AlbumManager.LoadedAlbums[albumKey].Index;

                foreach (var difficulty in fullComboMusic.Value)
                {
                    var dUid = $"{AlbumManager.Uid}-{albumIndex}_{difficulty}";
                    GameFullComboMusic.Add(dUid);
                }
            }
        }

        public static bool AssertAlbumKeyIndex(string albumKey, int albumIndex)
        {
            if (string.IsNullOrEmpty(albumKey))
            {
                Log.Debug($"Not found album index: {albumIndex}");
                return false;
            }
            if (albumKey.StartsWith("fs_"))
            {
                Log.Debug($"Ignore folder album: {albumKey}");
                return false;
            }
            return true;
        }
    }
}