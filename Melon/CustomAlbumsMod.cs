using CustomAlbums;
using CustomAlbums.Patch;
using MelonLoader;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyVersion(CustomAlbumsMod.MOD_VERSION)]
[assembly: MelonInfo(typeof(CustomAlbumsMod), CustomAlbumsMod.MOD_NAME, CustomAlbumsMod.MOD_VERSION, CustomAlbumsMod.MOD_AUTHOR)]
[assembly: MelonGame("PeroPeroGames", "MuseDash")]

namespace CustomAlbums
{
    public class CustomAlbumsMod : MelonMod
    {
        public const string MOD_NAME = "CustomAlbums";
        public const string MOD_AUTHOR = "Mo10, RobotLucca, & MDMC";
        public const string MOD_VERSION = "4.0.0.0";

        public override void OnApplicationStart() {
            LoggerInstance.Msg("CustomAlbums is loaded!");
            ModSettings.RegisterSettings();

            // ???
            Application.runInBackground = true;

            // Apply patches
            WebApiPatch.DoPatching(HarmonyInstance);
            AssetPatch.DoPatching();
            SavesPatch.DoPatching(HarmonyInstance);

            // Load albums and savefile
            AlbumManager.LoadAll();
            SaveManager.Load();
        }
    }
}
