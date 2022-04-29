using CustomAlbums.Patch;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(CustomAlbums.ModEntry), "CustomAlbums", "4.0.0", "Mo10 & RobotLucca")]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]

namespace CustomAlbums
{
    public static class Entry
    {
        public static void DoPatching(HarmonyLib.Harmony harmony) {
            Application.runInBackground = true;

            WebApiPatch.DoPatching(harmony);
            AssetPatch.DoPatching();
            SavesPatch.DoPatching(harmony);

            AlbumManager.LoadAll();
            SaveManager.Load();

            //harmony.PatchAll(typeof(SteamPatch));

            //harmony.PatchAll(typeof(AssetPatch));

            //ResourcePatch.DoPatching();
            //harmony.PatchAll(typeof(WebApiPatch));
            //harmony.PatchAll(typeof(ResourcePatch));
            //ResourcePatch.DoPatching(harmony);
        }
    }

    public class ModEntry : MelonMod
    {
        public override void OnApplicationStart() {
            LoggerInstance.Msg($"CustomAlbums is loaded!");
            ModSettings.RegisterSettings();
            Entry.DoPatching(HarmonyInstance);
        }
    }
}
