using HarmonyLib;

namespace CustomAlbums.Patch
{
	/// <summary>
	/// Stops PPG from being gay
	/// 
	/// Actual function: Disables the check on number of files within game directories for the purpose of Steam API initialization.
	/// Fixes an issue where any excess files in the base game directory would cause it to refuse to recognize the DLC.
	/// 
	/// Patch removed on 4/6/2022, when PPG removed this from the game. Archived in case they pull this again.
	/// </summary>
	/*[HarmonyPatch(typeof(SteamManager), nameof(SteamManager.DoSomething2))]
	internal static class PeroPeroGaysPatch
	{
		private static bool Prefix(ref bool __result) {
			__result = true;
			return false;
		}
	}*/
}
