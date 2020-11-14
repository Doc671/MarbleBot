using MarbleBot.Common.Games;
using MarbleBot.Common.Games.Scavenge;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarbleBot.Common
{
    public class ItemConverter : JsonConverter<Item>
    {
        public override Item Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionary = ReadObject(ref reader, options);

            var item = new Item(
                (int)dictionary["Id"],
                (string)dictionary["Name"],
                (decimal)dictionary["Price"],
                (string)dictionary["Description"],
                (bool)dictionary["OnSale"],
                dictionary.ContainsKey("Stage") ? (int)dictionary["Stage"] : 1,
                (ScavengeLocation)dictionary["ScavengeLocation"],
                dictionary.ContainsKey("CraftingProduced") ? (int)dictionary["CraftingProduced"] : 0,
                dictionary.ContainsKey("CraftingRecipe") ? (dictionary["CraftingRecipe"] as Dictionary<string, object>)!.ToDictionary(pair => int.Parse(pair.Key), pair => (int)pair.Value) : null,
                dictionary.ContainsKey("CraftingStationRequired") ? (int)dictionary["CraftingStationRequired"] : 1);

            if (dictionary.ContainsKey("Accuracy"))
            {
                return new Weapon(item.Id, item.Name, item.Price, item.Description, item.OnSale, item.Stage, item.ScavengeLocation,
                    item.CraftingProduced, item.CraftingRecipe, item.CraftingStationRequired,
                    (int)dictionary["Accuracy"],
                    dictionary.ContainsKey("Ammo") ? (dictionary["Ammo"] as List<object>)!.Cast<int>().ToImmutableArray() : ImmutableArray.Create<int>(),
                    (int)dictionary["Damage"],
                    (int)dictionary["Hits"],
                    (WeaponClass)dictionary["WeaponClass"]);
            }
            else if (dictionary.ContainsKey("Damage"))
            {
                return new Ammo(item.Id, item.Name, item.Price, item.Description, item.OnSale, item.Stage, item.ScavengeLocation,
                    item.CraftingProduced, item.CraftingRecipe, item.CraftingStationRequired,
                    (int)dictionary["Damage"]);
            }
            else if (dictionary.ContainsKey("IncomingDamageMultiplier"))
            {
                return new Shield(item.Id, item.Name, item.Price, item.Description, item.OnSale, item.Stage, item.ScavengeLocation,
                    item.CraftingProduced, item.CraftingRecipe, item.CraftingStationRequired,
                    (float)(decimal)dictionary["IncomingDamageMultiplier"]);
            }
            else if (dictionary.ContainsKey("OutgoingDamageMultiplier"))
            {
                return new Spikes(item.Id, item.Name, item.Price, item.Description, item.OnSale, item.Stage, item.ScavengeLocation,
                    item.CraftingProduced, item.CraftingRecipe, item.CraftingStationRequired,
                    (float)(decimal)dictionary["OutgoingDamageMultiplier"]);
            }
            else
            {
                return item;
            }
        }

        private Dictionary<string, object> ReadObject(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
            }

            var dictionary = new Dictionary<string, object>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected a property name");
                }

                string? propertyName = reader.GetString();

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new JsonException("Failed to get property name");
                }

                reader.Read();

                dictionary.Add(propertyName, ExtractValue(ref reader, options));
            }

            return dictionary;
        }

        private object ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    if (reader.TryGetDateTime(out DateTime date))
                    {
                        return date;
                    }

                    return reader.GetString()!;

                case JsonTokenType.False:
                    return false;

                case JsonTokenType.True:
                    return true;

                case JsonTokenType.Number:
                    if (reader.TryGetInt32(out int result))
                    {
                        return result;
                    }
                    return reader.GetDecimal();

                case JsonTokenType.StartObject:
                    return ReadObject(ref reader, options);

                case JsonTokenType.StartArray:
                    var list = new List<object>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        list.Add(ExtractValue(ref reader, options));
                    }
                    return list;

                default:
                    throw new JsonException($"'{reader.TokenType}' is not supported");
            }
        }

        public override void Write(Utf8JsonWriter writer, Item value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
