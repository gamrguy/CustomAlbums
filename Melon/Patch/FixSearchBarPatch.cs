using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.Structs.Modules;
using Assets.Scripts.UI.Panels.PnlMusicTag;
using HarmonyLib;

namespace CustomAlbums.Patch
{
    /// <summary>
    /// Tells the game to load all of CAM's language variants when the search bar asks for them.
    /// Fixes the search bar being unable to properly detect custom charts.
    /// </summary>
    [HarmonyPatch(typeof(SearchResults), nameof(SearchResults.RefreshData))]
    internal static class FixSearchBarPatch
    {
        private static void Prefix() {
            var config = Singleton<ConfigManager>.instance;
            var album = config.GetConfigObject<DBConfigALBUM>(AlbumManager.Uid + 1);
            album.GetAllLocal(new Il2CppSystem.Collections.Generic.List<DBConfigLocalALBUM>());
        }
    }
}
