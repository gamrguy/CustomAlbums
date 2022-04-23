using HarmonyLib;
using Newtonsoft.Json.Linq;
using IL2CppJson = Il2CppNewtonsoft.Json.Linq;
using Il2CppSystem.Collections.Generic;
using Assets.Scripts.Database;
using Assets.Scripts.Database.DataClass;
using static Assets.Scripts.Database.DBConfigCustomTags;
using DYUnityLib;

namespace CustomAlbums.Patch
{
	/// <summary>
	/// Patches the chart timers to last longer than 4 minutes.
	/// </summary>
	[HarmonyPatch(typeof(FixUpdateTimer), "Run")]
	internal class LongChartPatch
	{
		private static Logger Log = new Logger("LongChartPatch");
		private static void Prefix(FixUpdateTimer __instance) {
			if(__instance.totalTick >= 24000 && __instance.totalTick < int.MaxValue) {
				Log.Debug("Extending length of timer with length " + __instance.totalTick);
				__instance.totalTick = int.MaxValue;
			}
		}
	}
}