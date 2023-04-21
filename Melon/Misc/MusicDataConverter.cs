using GameLogic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Melon.Misc
{
    /// <summary>
    /// Converts MusicData into JSON.
    /// </summary>
    public class MusicDataConverter : JsonConverter<MusicData>
    {
        public override MusicData ReadJson(JsonReader reader, Type objectType, MusicData existingValue, bool hasExistingValue, JsonSerializer serializer) {
            var result = new MusicData();
            do {
                MelonLoader.MelonLogger.Msg(reader.Path);
            } while(reader.Read());
            return result;
        }

        public override void WriteJson(JsonWriter writer, MusicData value, JsonSerializer serializer) {
            var json = new JObject() {
                    { "configData", new JObject() {
                        { "blood", value.configData.blood },
                        { "id", value.configData.id },
                        { "length", (float)value.configData.length },
                        { "note_uid", value.configData.note_uid },
                        { "pathway", value.configData.pathway },
                        { "time", (float)value.configData.time }
                    } },
                    { "doubleIdx", value.doubleIdx },
                    { "dt", (float)value.dt },
                    { "endIndex", value.endIndex },
                    { "isDouble", value.isDouble },
                    { "isLongPressEnd", value.isLongPressEnd },
                    { "isLongPressing", value.isLongPressing },
                    { "isLongPressStart", value.isLongPressStart },
                    { "islongPressNum", value.longPressNum },
                    { "longPressPTick", (float)value.longPressPTick },
                    { "noteData", new JObject() {
                        { "addCombo", value.noteData.addCombo },
                        { "boss_action", value.noteData.boss_action },
                        { "damage", value.noteData.damage },
                        { "des", value.noteData.des },
                        { "effect", value.noteData.effect },
                        { "fever", value.noteData.fever },
                        { "ibms_id", value.noteData.ibms_id },
                        { "id", value.noteData.id },
                        { "isShowPlayEffect", value.noteData.isShowPlayEffect },
                        { "jumpNote", value.noteData.jumpNote },
                        { "key_audio", value.noteData.key_audio },
                        { "left_great_range", (float)value.noteData.left_great_range },
                        { "left_perfect_range", (float)value.noteData.left_perfect_range },
                        { "mirror_uid", value.noteData.mirror_uid },
                        { "missCombo", value.noteData.missCombo },
                        { "noteUid", value.noteData.noteUid },
                        { "pathway", value.noteData.pathway },
                        { "prefab_name", value.noteData.prefab_name },
                        { "right_great_range", (float)value.noteData.right_great_range },
                        { "right_perfect_range", (float)value.noteData.right_perfect_range },
                        { "scene", value.noteData.scene },
                        { "sceneChangeNames", new JArray() },
                        { "score", value.noteData.score },
                        { "speed", value.noteData.speed },
                        { "type", value.noteData.type },
                        { "uid", value.noteData.uid }
                    } },
                    { "objId", value.objId },
                    { "showTick", (float)value.showTick },
                    { "tick", (float)value.tick }
                };

            if(value.noteData.sceneChangeNames != null) {
                foreach(var n in value.noteData.sceneChangeNames) {
                    var jArr = json["noteData"]["sceneChangeNames"] as JArray;
                    jArr.Add(n);
                }
            }

            json.WriteTo(writer);
        }
    }
}
