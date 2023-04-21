using Assets.Scripts.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Melon.Misc
{
    /// <summary>
    /// Converts dialogs into JSON.
    /// </summary>
    public class DialogConverter : JsonConverter<GameDialogArgs>
    {
        public override GameDialogArgs ReadJson(JsonReader reader, Type objectType, GameDialogArgs existingValue, bool hasExistingValue, JsonSerializer serializer) {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, GameDialogArgs value, JsonSerializer serializer) {
            var json = new JObject() {
                    { "index", value.index },
                    { "time", (float)value.time },
                    { "dialogType", value.dialogType },
                    { "dialogIndex", value.dialogIndex },
                    { "text", value.text },
                    { "textColor", new JObject() {
                        { "r", value.textColor.r },
                        { "g", value.textColor.g },
                        { "b", value.textColor.b },
                        { "a", value.textColor.a }
                    } },
                    { "bgColor", new JObject() {
                        { "r", value.bgColor.r },
                        { "g", value.bgColor.g },
                        { "b", value.bgColor.b },
                        { "a", value.bgColor.a }
                    } },
                    { "speed", value.speed },
                    { "fontSize", value.fontSize },
                    { "dialogSize", new JObject() {
                        { "x", value.dialogSize.x },
                        { "y", value.dialogSize.y }
                    } },
                    { "dialogState", (int)value.dialogState },
                    { "alignment", (int)value.alignment }
                };
            json.WriteTo(writer);
        }
    }
}
