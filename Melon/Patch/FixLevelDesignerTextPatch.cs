using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.UI.Panels.PnlMusicTag;
using HarmonyLib;

namespace CustomAlbums.Patch
{
    /// <summary>
    /// Flips levelDesigner display priority.
    /// Original behavior: levelDesigner overrides levelDesignerX
    /// New behavior:      levelDesignerX overrides levelDesigner
    /// 
    /// This allows levelDesigner to be used as a "hidden" designer, for search purposes.
    /// </summary>
    [HarmonyPatch(typeof(MusicInfo), nameof(MusicInfo.GetLevelDesignerStringByIndex))]
    internal static class FixLevelDesignerTextPatch
    {
        private static bool Prefix(MusicInfo __instance, ref string __result, int index) {
            switch(index) {
                case 1:
                    __result = __instance.m_MaskValue.ContainsKey("levelDesigner1") ? __instance.m_MaskValue["levelDesigner1"].ToString() : __instance.levelDesigner1;
                    break;
                case 2:
                    __result = __instance.m_MaskValue.ContainsKey("levelDesigner2") ? __instance.m_MaskValue["levelDesigner2"].ToString() : __instance.levelDesigner2;
                    break;
                case 3:
                    __result = __instance.m_MaskValue.ContainsKey("levelDesigner3") ? __instance.m_MaskValue["levelDesigner3"].ToString() : __instance.levelDesigner3;
                    break;
                case 4:
                    __result = __instance.m_MaskValue.ContainsKey("levelDesigner4") ? __instance.m_MaskValue["levelDesigner4"].ToString() : __instance.levelDesigner4;
                    break;
                case 5:
                    __result = __instance.m_MaskValue.ContainsKey("levelDesigner5") ? __instance.m_MaskValue["levelDesigner5"].ToString() : __instance.levelDesigner5;
                    break;
            }

            if(string.IsNullOrEmpty(__result) || __result == "?") __result = __instance.m_MaskValue.ContainsKey("levelDesigner") ? __instance.m_MaskValue["levelDesigner"].ToString() : __instance.levelDesigner;
            if(string.IsNullOrEmpty(__result)) __result = "?????";
            return false;
        }
    }
}
