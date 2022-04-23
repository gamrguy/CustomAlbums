using HarmonyLib;
using Assets.Scripts.Database;

namespace CustomAlbums.Patch
{
    /// <summary>
    /// Patches the DJMax victory screen to enable the scrolling song title object.
    /// Fixes a vanilla bug where this object does not enable automatically.
    /// </summary>
    [HarmonyPatch(typeof(PnlVictory), "OnVictory")]
    internal static class DjVictoryPatch
    {
        private static void Postfix(PnlVictory __instance) {
            if(__instance.m_CurControls.mainPnl.transform.parent.name == "Djmax") {
                var titleObj = __instance.m_CurControls.mainPnl.transform.Find("PnlVictory_3D").Find("SongTittle").Find("ImgSongTittleMask");
                var titleNormalTxt = titleObj.Find("TxtSongTittle").gameObject;

                // If the normal title text isn't active, then the scrollable text should be
                if(!titleNormalTxt.active) {
                    var titleScrollTxt = titleObj.Find("MaskPos").gameObject;
                    titleScrollTxt.SetActive(true);
                }
            }
        }
    }
}