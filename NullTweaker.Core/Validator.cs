using System.Text.Json.Nodes;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NullTweaker.Core;

public static partial class Validator
{
    [GeneratedRegex("^[a-z0-9:_.-]{3,128}$")]
    private static partial Regex Identifier();
    [GeneratedRegex("^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$")]
    private static partial Regex Colour();

    public static IReadOnlyList<string> Check(ConfigDocument document)
    {
        List<string> errors = new();
        if (document.Root is JsonObject obj)
        {
            Fields(obj, Schema.Fields(document.Kind), "", errors);
            if (document.Kind is DocumentKind.Weapon or DocumentKind.NPC or DocumentKind.Customization)
            {
                Name(obj, "Name", errors);
                string id = String(obj, "Id");
                if (id != "" && (!Identifier().IsMatch(id) || !id.Contains(':')))
                    errors.Add("Id must be a lowercase namespace:name identifier (3–128 characters).");
            }
            if (document.Kind == DocumentKind.Customization && String(obj, "Id").StartsWith("base:", StringComparison.Ordinal))
                errors.Add("The base: ID prefix is reserved for native customization items.");
            if (document.Kind == DocumentKind.NPC)
            {
                if (obj["Id"] != null && String(obj,"Id")=="") errors.Add("Id must be omitted/null or a valid namespace:name.");
                string[] types = Schema.NPCTypes.Where(t => obj[t] != null).ToArray();
                if (types.Length != 1)
                    errors.Add("Select exactly one NPC behaviour type. Legacy multi-type files must be resolved explicitly.");
                foreach (string type in types)
                {
                    if (obj[type] is not JsonObject data)
                    {
                        errors.Add(type + " must be an object.");
                        continue;
                    }
                    Fields(data, Schema.NPCData(type), type + ".", errors);
                    Name(data, "DummyNPC", errors);
                    if (String(data, "DummyNPC").Contains(".."))
                        errors.Add(type + ".DummyNPC cannot contain '..'.");
                    foreach (string key in type is "Bot" or "BossBot" ? new[] { "Health", "AttackRange", "AttackSpeed" } : Array.Empty<string>())
                        if (Number(data, key) <= 0)
                            errors.Add(type + "." + key + " must be greater than zero.");
                    if (type == "Bot" && Bool(data, "UseRage") && (Number(data, "RageAttackTime") <= 0))
                        errors.Add("Bot.RageAttackTime must be greater than zero when rage is enabled.");
                }
                string color = String(obj, "ModelColor");
                if (color != "" && !Colour().IsMatch(color))
                    errors.Add("ModelColor must be #RRGGBB or #RRGGBBAA.");
            }
            if (document.Kind == DocumentKind.Map)
            {
                Name(obj, "MapName", errors);
                string name = String(obj, "MapName");
                if (name.IndexOfAny(['/', '\\', ':']) >= 0 || name.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || name.Length + (Bool(obj, "UseDayAndNight") ? 8 : 0) > 128)
                    errors.Add("MapName must be a scene name without a path or .unity extension (up to 128 characters including day/night suffix).");
                foreach (string key in new[] { "CollectMonster", "VersusMonster", "SurvivalMonster" })
                    if (obj.ContainsKey(key))
                        Name(obj, key, errors);
                string id = String(obj, "Id");
                if (obj["Id"] != null && (id == "" || !Identifier().IsMatch(id) || !id.Contains(':')))
                    errors.Add("Id must be a lowercase namespace:name identifier, or omit the optional field.");
                if (obj["Dependencies"] is JsonArray deps && (deps.Count > 128 || deps.Any(d => !ValidName(d?.ToString()))))
                    errors.Add("Use up to 128 nonempty NPC dependencies.");
            }
        }
        else if (document.Root is JsonArray array)
        {
            int minimum = document.Kind == DocumentKind.Waves ? 1 : 0;
            if (array.Count < minimum || array.Count > 256)
                errors.Add($"Use {minimum}–256 {document.Kind.ToString().ToLowerInvariant()}.");
            HashSet<string> ids = new(StringComparer.Ordinal);
            for (int i = 0; i < array.Count; i++)
            {
                string prefix = $"Item {i + 1}: ";
                if (array[i] is not JsonObject item)
                {
                    errors.Add(prefix + "must be an object.");
                    continue;
                }
                Fields(item, Schema.Fields(document.Kind), prefix, errors);
                if (document.Kind == DocumentKind.Waves)
                {
                    if (item["WaveInfo"] is not JsonObject info)
                    {
                        errors.Add(prefix + "WaveInfo is required.");
                        continue;
                    }
                    Fields(info, Schema.WaveInfo, prefix, errors);
                    if (info.ContainsKey("DefaultNPC"))
                        Name(info, "DefaultNPC", errors, prefix);
                    if (info["NPCs"] is JsonArray npcs)
                    {
                        if (npcs.Count > 128)
                            errors.Add(prefix + "use at most 128 special NPCs.");
                        foreach (JsonObject npc in npcs.OfType<JsonObject>())
                            Name(npc, "NPCName", errors, prefix);
                    }
                }
                else
                {
                    Name(item, "Id", errors, prefix);
                    if (!ids.Add(String(item, "Id")))
                        errors.Add(prefix + "duplicate trigger Id.");
                    foreach (string key in new[] { "WaterColor", "AmbientColor", "FogColor" })
                        if (item.ContainsKey(key) && !Colour().IsMatch(String(item, key)))
                            errors.Add(prefix + key + " must be #RRGGBB or #RRGGBBAA.");
                    if (Number(item, "FogEnd", 300) < Number(item, "FogStart"))
                        errors.Add(prefix + "FogEnd must be at least FogStart.");
                }
            }
        }
        else
            errors.Add("Unexpected JSON root.");
        return errors.Distinct().ToArray();
    }

    private static void Fields(JsonObject obj, Field[] fields, string prefix, List<string> errors)
    {
        foreach (Field field in fields)
        {
            if (!obj.TryGetPropertyValue(field.Key, out JsonNode? node))
                continue;
            string key = prefix + field.Key;
            bool valid = true;
            switch (field.Kind)
            {
                case FieldKind.Text:
                    valid = node == null || node is JsonValue v && v.TryGetValue<string>(out _);
                    break;
                case FieldKind.Toggle:
                    valid = node is JsonValue b && b.TryGetValue<bool>(out _);
                    break;
                case FieldKind.Number:
                case FieldKind.Integer:
                    valid = Numeric(node, field);
                    break;
                case FieldKind.Choice:
                    valid = field.Options!.Any(o => JsonNode.DeepEquals(node, System.Text.Json.JsonSerializer.SerializeToNode(o.Value)));
                    break;
                case FieldKind.Vector:
                    valid = node is JsonArray vector && vector.Count == 3 && vector.All(n => Numeric(n, field));
                    break;
                case FieldKind.Strings:
                    valid = node is JsonArray strings && strings.All(n => n is JsonValue sv && sv.TryGetValue<string>(out _));
                    break;
                case FieldKind.Object:
                    valid = node is JsonObject;
                    if (node is JsonObject nested)
                        Fields(nested, field.Children!, key + ".", errors);
                    break;
                case FieldKind.Objects:
                    valid = node is JsonArray objects && objects.All(n => n is JsonObject);
                    if (node is JsonArray children)
                        for (int i = 0; i < children.Count; i++)
                        if (children[i] is JsonObject child)
                            Fields(child, field.Children!, key + $"[{i + 1}].", errors);
                    break;
            }
            if (!valid)
                errors.Add(key + " has an invalid value" + (field.Min != null ? $" (expected {field.Min} to {field.Max})" : "") + ".");
        }
    }

    private static bool Numeric(JsonNode? node, Field field) => node is JsonValue && double.TryParse(node.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double n) &&
        double.IsFinite(n) && n >= -float.MaxValue && n <= float.MaxValue && (field.Min == null || n >= field.Min) &&
        (field.Max == null || n <= field.Max) && (field.Kind != FieldKind.Integer || Math.Truncate(n) == n);
    public static string String(JsonObject obj, string key) => obj[key] is JsonValue v && v.TryGetValue<string>(out string? s) ? s : "";
    public static double Number(JsonObject obj, string key, double fallback = 0) => obj[key] is JsonValue v && double.TryParse(v.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double n) ? n : fallback;
    public static bool Bool(JsonObject obj, string key) => obj[key] is JsonValue v && v.TryGetValue<bool>(out bool b) && b;
    private static bool ValidName(string? name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 128 && !name.Any(char.IsControl);
    private static void Name(JsonObject obj, string key, List<string> errors, string prefix = "")
    {
        string text = String(obj, key);
        if (!ValidName(text) || key == "Name" && text.IndexOfAny(['/', '\\']) >= 0)
            errors.Add(prefix + key + " must contain a valid name (1–128 characters).");
    }
}
