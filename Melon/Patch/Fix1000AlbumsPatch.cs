using Assets.Scripts.Database;
using Assets.Scripts.PeroTools.Commons;
using Assets.Scripts.PeroTools.Managers;
using Assets.Scripts.UI.Panels.PnlMusicTag;
using HarmonyLib;

namespace CustomAlbums.Patch
{
    /// <summary>
    /// Stops the game from thinking there are 1000 album JSONs in consecutive order.
    /// Makes a huge difference for systems without absurd disk read speeds.
    /// Also stops the music index and search bar from crashing on use.
    /// </summary>
    [HarmonyPatch(typeof(PnlMusicTag), nameof(PnlMusicTag.InitBaseUi))]
    internal static class Fix1000AlbumsPatch
    {
        private static void Prefix() {
            var config = Singleton<ConfigManager>.instance;
            var albums = config.GetConfigObject<DBConfigAlbums>(-1);
            // Custom album + 2 "virtual" albums for internal use
            albums.m_MaxAlbumUid = albums.count - 3;
        }
    }
}
