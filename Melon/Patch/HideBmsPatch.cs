using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using Assets.Scripts.Database;
using CustomAlbums.Data;
using Assets.Scripts.PeroTools.Commons;
using UnhollowerBaseLib;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.UI.Tips;
using PeroPeroGames.GlobalDefines;

namespace CustomAlbums.Patch
{
    /// <summary>
    /// Adds custom albums with hidden charts to the list
    /// </summary>
    [HarmonyPatch(typeof(SpecialSongManager), nameof(SpecialSongManager.InitHideBmsInfoDic))]
    internal static class HideBmsInfoDicPatch {
        private static bool runOnce;

        private static void Postfix(SpecialSongManager __instance) {
            if(!runOnce) {
                foreach(var album in AlbumManager.LoadedAlbums) {
                    var albumUid = $"{AlbumManager.Uid}-{album.Value.Index}";
                    // Enable hidden mode for charts containing map4
                    if(album.Value.availableMaps.ContainsKey(4)) {
                        __instance.m_HideBmsInfos.Add($"{AlbumManager.Uid}-{album.Value.Index}",
                        new SpecialSongManager.HideBmsInfo(
                            albumUid,
                            album.Value.Info.hideBmsDifficulty == 0 ? (album.Value.availableMaps.ContainsKey(3) ? 3 : 2) : album.Value.Info.hideBmsDifficulty,
                            4,
                            $"{album.Value.Name}_map4",
                            (Il2CppSystem.Func<bool>)delegate { return __instance.IsInvokeHideBms(albumUid); }
                        ));

                        // Add chart to the appropriate list for their hidden type
                        switch(album.Value.Info.GetHideBmsMode()) {
                            case AlbumInfo.HideBmsMode.CLICK:
                                var newClickArr = new Il2CppStringArray(__instance.m_ClickHideUids.Length + 1);
                                for(int i = 0; i < __instance.m_ClickHideUids.Length; i++) newClickArr[i] = __instance.m_ClickHideUids[i];
                                newClickArr[newClickArr.Length - 1] = albumUid;
                                __instance.m_ClickHideUids = newClickArr;
                                break;
                            case AlbumInfo.HideBmsMode.PRESS:
                                var newPressArr = new Il2CppStringArray(__instance.m_LongPressHideUids.Length + 1);
                                for(int i = 0; i < __instance.m_LongPressHideUids.Length; i++) newPressArr[i] = __instance.m_LongPressHideUids[i];
                                newPressArr[newPressArr.Length - 1] = albumUid;
                                __instance.m_LongPressHideUids = newPressArr;
                                break;
                            case AlbumInfo.HideBmsMode.TOGGLE:
                                var newToggleArr = new Il2CppStringArray(__instance.m_ToggleChangedHideUids.Length + 1);
                                for(int i = 0; i < __instance.m_ToggleChangedHideUids.Length; i++) newToggleArr[i] = __instance.m_ToggleChangedHideUids[i];
                                newToggleArr[newToggleArr.Length - 1] = albumUid;
                                __instance.m_ToggleChangedHideUids = newToggleArr;
                                break;
                            default:
                                break;
                        }
                    }
                }

                // This may run multiple times, but creates data which can only be generated once
                runOnce = true;
            }
        }
    }

    /// <summary>
    /// Activates hidden charts when the conditions are met
    /// </summary>
    [HarmonyPatch(typeof(SpecialSongManager), nameof(SpecialSongManager.InvokeHideBms))]
    internal static class InvokeHideBmsPatch {
        private static bool Prefix(MusicInfo musicInfo, SpecialSongManager __instance) {
            if(musicInfo.uid.StartsWith(AlbumManager.Uid.ToString()) && __instance.m_HideBmsInfos.ContainsKey(musicInfo.uid)) {
                var hideBms = __instance.m_HideBmsInfos[musicInfo.uid];
                __instance.m_IsInvokeHideDic[hideBms.uid] = true;
                
                if(hideBms.extraCondition.Invoke()) {
                    var album = AlbumManager.LoadedAlbums[AlbumManager.GetAlbumKeyByIndex(musicInfo.musicIndex)];

                    ActivateHidden(hideBms);

                    if(album.Info.hideBmsMessage != null) {
                        var msgBox = PnlTipsManager.instance.GetMessageBox("PnlSpecialsBmsAsk");
                        msgBox.Show("TIPS", album.Info.hideBmsMessage);
                    }
                    SpecialSongManager.onTriggerHideBmsEvent?.Invoke();
                    if(album.Info.GetHideBmsMode() == AlbumInfo.HideBmsMode.PRESS) Singleton<EventManager>.instance.Invoke("UI/OnSpecialsMusic", null);
                }
                return false;
            }
            return true;
        }

        private static bool ActivateHidden(SpecialSongManager.HideBmsInfo hideBms) {
            if(hideBms == null) return false;

            var info = GlobalDataBase.dbMusicTag.GetMusicInfoFromAll(hideBms.uid);
            var success = false;
            if(hideBms.triggerDiff != 0) {
                var targetDiff = hideBms.triggerDiff;
                if(targetDiff == -1) {
                    targetDiff = 2;

                    // Disable the other difficulty options
                    info.AddMaskValue("difficulty1", "0");
                    info.AddMaskValue("difficulty3", "0");
                }
                var diffToHide = "difficulty" + targetDiff;
                var levelDesignToHide = "levelDesigner" + targetDiff;
                var diffStr = "?";
                var levelDesignStr = info.levelDesigner;
                switch(hideBms.m_HideDiff) {
                    case 1:
                        diffStr = info.difficulty1;
                        levelDesignStr = info.levelDesigner1 ?? info.levelDesigner;
                        break;
                    case 2:
                        diffStr = info.difficulty2;
                        levelDesignStr = info.levelDesigner2 ?? info.levelDesigner;
                        break;
                    case 3:
                        diffStr = info.difficulty3;
                        levelDesignStr = info.levelDesigner3 ?? info.levelDesigner;
                        break;
                    case 4:
                        diffStr = info.difficulty4;
                        levelDesignStr = info.levelDesigner4 ?? info.levelDesigner;
                        break;
                    case 5:
                        diffStr = info.difficulty5;
                        levelDesignStr = info.levelDesigner5 ?? info.levelDesigner;
                        break;
                }
                info.AddMaskValue(diffToHide, diffStr);
                info.AddMaskValue(levelDesignToHide, levelDesignStr);
                info.SetDifficulty(targetDiff, hideBms.m_HideDiff);

                success = true;
            }

            return success;
        }
    }

    /// <summary>
    /// Adds charts to the "With Hidden Sheet" tag
    /// </summary>
    [HarmonyPatch(typeof(MusicTagManager), nameof(MusicTagManager.InitDefaultInfo))]
    internal static class AddHiddenSheetTagPatch
    {
        private static bool runOnce;

        private static void Postfix() {
            if(!runOnce) {
                var albumList = new List<string>();
                foreach(var album in AlbumManager.LoadedAlbums) {
                    if(album.Value.availableMaps.ContainsKey(4)) {
                        albumList.Add($"{AlbumManager.Uid}-{album.Value.Index}");

                        var newArr = new Il2CppStringArray(DBMusicTagDefine.s_HiddenLocal.Length + 1);
                        for(int i = 0; i < DBMusicTagDefine.s_HiddenLocal.Count; i++) {
                            newArr[i] = DBMusicTagDefine.s_HiddenLocal[i];
                        }
                        newArr[newArr.Length - 1] = $"{AlbumManager.Uid}-{album.Value.Index}";
                        DBMusicTagDefine.s_HiddenLocal = newArr;
                    }
                }
                var tagInfo = GlobalDataBase.dbMusicTag.GetAlbumTagInfo(32776);
                tagInfo.AddTagUids(albumList);
                runOnce = true;
            }
        }
    }
}