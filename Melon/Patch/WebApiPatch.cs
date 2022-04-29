using Account;
using Assets.Scripts.Database;
using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using IL2CppJson = Il2CppNewtonsoft.Json.Linq;

namespace CustomAlbums.Patch
{
    class WebApiPatch
    {
        private static readonly Logger Log = new Logger("WebApiPatch");
        

        public static void DoPatching(HarmonyLib.Harmony harmony) {
            var targetMethod = AccessTools.Method(typeof(GameAccountSystem), "SendToUrl");
            var patchMethod = AccessTools.Method(typeof(WebApiPatch), "SendToUrlPatch");
            harmony.Patch(targetMethod, patchMethod.ToNewHarmonyMethod());
        }

        /// <summary>
        /// Hook GameAccountSystem request.
        /// </summary>
        public static bool SendToUrlPatch(string url, string method, Dictionary<string, Object> datas) {

            Log.Debug($"[SendToUrlPatch] url:{url} method:{method}");

            switch(url) {
                case "statistics/pc-play-statistics-feedback":
                    if(datas["music_uid"].ToString().StartsWith($"{AlbumManager.Uid}")) {
                        Log.Debug("[SendToUrlPatch] Blocked play feedback upload:" + datas["music_uid"].ToString());
                        return false;
                    }
                    break;
                case "musedash/v2/pcleaderboard/high-score":
                    if(GlobalDataBase.dbBattleStage.musicUid.StartsWith($"{AlbumManager.Uid}")) {
                        Log.Debug("[SendToUrlPatch] Blocked high score upload:" + GlobalDataBase.dbBattleStage.musicUid);
                        return false;
                    }
                    break;
            }
            return true;
        }
    }
}
