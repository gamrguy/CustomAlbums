using HarmonyLib;
using Assets.Scripts.GameCore.Managers;
using GameLogic;

namespace CustomAlbums.Patch
{
    /// <summary>
    /// Adds notes to the NoteDatas.
    /// </summary>
    [HarmonyPatch(typeof(NoteDataMananger), "Init")]
    internal static class NotesPatch
    {
        private static void Postfix(NoteDataMananger __instance) {
            // Add DJMax scene switch
            __instance.NoteDatas.Add(new NoteConfigData {
                ibms_id = "1W",

                m_BmsUid = PeroPeroGames.GlobalDefines.BmsNodeUid.ToggleScene10,
                id = (__instance.NoteDatas.Count + 1).ToString(),
                des = "DJMax Scene Switch",
                prefab_name = "000401",
                uid = "000409",
                noteUid = 409,
                isShowPlayEffect = true,
                scene = "0",
                boss_action = "0",
                key_audio = "0",
                effect = "0"
            });
        }
    }
}