using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.Structs.Modules;
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
    [HarmonyPatch(typeof(SearchResults), nameof(SearchResults.PeroLevelDesigner))]
    internal static class FixLevelDesignerSearchPatch
    {
        private static bool Prefix(ref bool __result, PeroPeroGames.PeroString peroString, MusicInfo musicInfo, string containsText) {
            if(string.IsNullOrEmpty(containsText)) return false;

            __result |= peroString.LowerContains(musicInfo.levelDesigner ?? "", containsText);
            if(__result) return false;

            for(int i = 1; i <= 5; i++) {
                var diff = musicInfo.GetMusicLevelStringByDiff(i, false);
                if(string.IsNullOrEmpty(diff) || diff == "0") continue;
                __result |= peroString.LowerContains(musicInfo.GetLevelDesignerStringByIndex(i) ?? "", containsText);
                if(__result) return false;
            }
            return false;
        }

        private static bool LowerContains(this PeroPeroGames.PeroString peroString, string compareText, string containsText) {
            peroString.Clear();
            peroString.Append(compareText);
            peroString.ToLower();
            return peroString.Contains(containsText);
        }
    }
}
