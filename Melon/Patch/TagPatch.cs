using HarmonyLib;
using Newtonsoft.Json.Linq;
using IL2CppJson = Il2CppNewtonsoft.Json.Linq;
using Il2CppSystem.Collections.Generic;
using Assets.Scripts.Database;
using Assets.Scripts.Database.DataClass;
using static Assets.Scripts.Database.DBConfigCustomTags;

namespace CustomAlbums.Patch
{
    /// <summary>
    /// Adds a tag for Custom Albums on the top row.
    /// </summary>
    [HarmonyPatch(typeof(MusicTagManager), "InitAlbumTagInfo")]
    internal static class TagPatch
    {
        private static void Postfix() {
            var info = new AlbumTagInfo {
                name = AlbumManager.Langs["English"],
                tagUid = "tag-custom-albums",
                iconName = "IconCustomAlbums"
            };
            var customInfo = new CustomTagInfo {
                tag_name = JObject.FromObject(AlbumManager.Langs).JsonSerialize().IL2CppJsonDeserialize<IL2CppJson.JObject>().ToObject<Dictionary<string, string>>(),
                tag_picture = "https://mdmc.moe/cdn/melon.png"
            };
            customInfo.music_list = new List<string>();
            foreach(var uid in AlbumManager.GetAllUid()) customInfo.music_list.Add(uid);
            
            info.InitCustomTagInfo(customInfo);

            GlobalDataBase.dbMusicTag.m_AlbumTagsSort.Insert(GlobalDataBase.dbMusicTag.m_AlbumTagsSort.Count - 4, AlbumManager.Uid);
            GlobalDataBase.dbMusicTag.AddAlbumTagData(AlbumManager.Uid, info);
        }
    }
}
