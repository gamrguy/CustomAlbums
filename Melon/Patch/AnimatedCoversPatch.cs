using Assets.Scripts.Database;
using Assets.Scripts.GameCore.Managers;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.PeroTools.Nice.Interface;
using Assets.Scripts.UI.Panels.PnlMusicTag;
using HarmonyLib;
using PeroTools2.Resources;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomAlbums.Patch
{
    /// <summary>
    /// Enables animated album covers.
    /// </summary>
    [HarmonyPatch(typeof(MusicStageCell), nameof(MusicStageCell.Awake))]
    internal static class AnimatedCoversPatch
    {
        private static List<MusicStageCell> cells = new();

        public static void Update() {
            var dbMusicTag = GlobalDataBase.dbMusicTag;

            // potential fix for null error
            if (dbMusicTag == null) return;

            for(var i = cells.Count - 1; i >= 0; i--) {
                if(cells[i] == null || !cells[i].enabled) {
                    cells.RemoveAt(i);
                }
            }

            foreach(var cell in cells) {
                var idx = cell.m_VariableBehaviour.Cast<IVariable>().GetResult<int>();
                var uid = dbMusicTag.GetShowStageUidByIndex(idx);
                var musicInfo = dbMusicTag.GetMusicInfoFromAll(uid);
                if(musicInfo.albumJsonIndex < AlbumManager.Uid) continue;

                if(uid != "?") {
                    var album = AlbumManager.LoadedAlbumsByUid[uid];
                    var frame = ((int)Mathf.Floor(Time.time * 1000) % (album.CoverFrameRateMs * album.CoverSpriteFrames.Length)) / album.CoverFrameRateMs;
                    cell.m_StageImg.sprite = album.CoverSpriteFrames[Math.Min(frame, album.CoverSpriteFrames.Length)];
                }
            }
        }

        private static void Prefix(MusicStageCell __instance) {
            cells.Add(__instance);
        }
    }
}
