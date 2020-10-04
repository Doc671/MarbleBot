using MarbleBot.Common.Games;
using MarbleBot.Common.Games.Scavenge;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MarbleBot.Common
{
    public class ItemConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue,
            JsonSerializer serializer)
        {
            var jObject = JToken.ReadFrom(reader);
            Item? result;

            if (jObject["WeaponClass"] != null)
            {
                result = new Weapon(jObject["Id"]?.ToObject<int>(),
                    jObject["Name"]?.ToObject<string>(),
                    jObject["Price"]?.ToObject<decimal>(),
                    jObject["Description"]?.ToObject<string>(),
                    jObject["OnSale"]?.ToObject<bool>(),
                    jObject["Stage"]?.ToObject<int>(),
                    jObject["ScavengeLocation"]?.ToObject<ScavengeLocation>(),
                    jObject["CraftingProduced"]?.ToObject<int>(),
                    jObject["CraftingRecipe"]?.ToObject<Dictionary<int, int>?>(),
                    jObject["CraftingStationRequired"]?.ToObject<int>(),
                    jObject["Accuracy"]?.ToObject<int>(),
                    jObject["Ammo"]?.ToObject<int[]>(),
                    jObject["Damage"]?.ToObject<int>(),
                    jObject["Hits"]?.ToObject<int>(),
                    jObject["WeaponClass"]?.ToObject<WeaponClass>());
            }
            else if (jObject["Damage"] != null)
            {
                result = new Ammo(jObject["Id"]?.ToObject<int>(),
                    jObject["Name"]?.ToObject<string>(),
                    jObject["Price"]?.ToObject<decimal>(),
                    jObject["Description"]?.ToObject<string>(),
                    jObject["OnSale"]?.ToObject<bool>(),
                    jObject["Stage"]?.ToObject<int>(),
                    jObject["ScavengeLocation"]?.ToObject<ScavengeLocation>(),
                    jObject["CraftingProduced"]?.ToObject<int>(),
                    jObject["CraftingRecipe"]?.ToObject<Dictionary<int, int>?>(),
                    jObject["CraftingStationRequired"]?.ToObject<int>(),
                    jObject["Damage"]?.ToObject<int>());
            }
            else if (jObject["IncomingDamageMultiplier"] != null)
            {
                result = new Shield(jObject["Id"]?.ToObject<int>(),
                    jObject["Name"]?.ToObject<string>(),
                    jObject["Price"]?.ToObject<decimal>(),
                    jObject["Description"]?.ToObject<string>(),
                    jObject["OnSale"]?.ToObject<bool>(),
                    jObject["Stage"]?.ToObject<int>(),
                    jObject["ScavengeLocation"]?.ToObject<ScavengeLocation>(),
                    jObject["CraftingProduced"]?.ToObject<int>(),
                    jObject["CraftingRecipe"]?.ToObject<Dictionary<int, int>?>(),
                    jObject["CraftingStationRequired"]?.ToObject<int>(),
                    jObject["IncomingDamageMultiplier"]?.ToObject<int>());
            }
            else if (jObject["OutgoingDamageMultiplier"] != null)
            {
                result = new Spikes(jObject["Id"]?.ToObject<int>(),
                    jObject["Name"]?.ToObject<string>(),
                    jObject["Price"]?.ToObject<decimal>(),
                    jObject["Description"]?.ToObject<string>(),
                    jObject["OnSale"]?.ToObject<bool>(),
                    jObject["Stage"]?.ToObject<int>(),
                    jObject["ScavengeLocation"]?.ToObject<ScavengeLocation>(),
                    jObject["CraftingProduced"]?.ToObject<int>(),
                    jObject["CraftingRecipe"]?.ToObject<Dictionary<int, int>?>(),
                    jObject["CraftingStationRequired"]?.ToObject<int>(),
                    jObject["OutgoingDamageMultiplier"]?.ToObject<float>());
            }
            else
            {
                result = new Item(jObject["Id"]?.ToObject<int>(),
                    jObject["Name"]?.ToObject<string>(),
                    jObject["Price"]?.ToObject<decimal>(),
                    jObject["Description"]?.ToObject<string>(),
                    jObject["OnSale"]?.ToObject<bool>(),
                    jObject["Stage"]?.ToObject<int>(),
                    jObject["ScavengeLocation"]?.ToObject<ScavengeLocation>(),
                    jObject["CraftingProduced"]?.ToObject<int>(),
                    jObject["CraftingRecipe"]?.ToObject<Dictionary<int, int>?>(),
                    jObject["CraftingStationRequired"]?.ToObject<int>());
            }

            serializer.Populate(jObject.CreateReader(), result!);
            return result;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
