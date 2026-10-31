using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NullTweaker.Core;

public sealed class ConfigDocument
{
    public DocumentKind Kind
    {
        get;
    }
    public JsonNode Root
    {
        get;
    }
    public string? FilePath
    {
        get; private set;
    }
    public List<string> Notices { get; } = new();
    private string _saved;
    private byte[]? _diskHash;
    private readonly Dictionary<string, JsonNode> _npcDrafts = new();
    public bool Dirty => Json() != _saved;
    public string Title => Root is JsonObject obj ? obj[Kind == DocumentKind.Map ? "MapName" : "Name"]?.ToString() ?? Kind.ToString() : Kind.ToString();

    public ConfigDocument(DocumentKind kind, JsonNode? root = null)
    {
        Kind = kind;
        Root = root ?? Schema.New(kind);
        _saved = root == null ? "" : Json();
    }

    public string Json() => Root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;

    public static ConfigDocument Open(string path, DocumentKind? arrayKind = null)
    {
        if (new FileInfo(path).Length > 1024 * 1024)
            throw new InvalidDataException("Configuration files must be at most 1 MB.");
        byte[] bytes = File.ReadAllBytes(path);
        string text = File.ReadAllText(path);
        ConfigDocument document;
        if (Path.GetExtension(path).Equals(".ini", StringComparison.OrdinalIgnoreCase))
        {
            document = ImportIni(text);
            document.Notices.Add("Legacy INI imported. Save as map.json; the original file is unchanged.");
            return document;
        }
        using JsonDocument parsed = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        RejectDuplicates(parsed.RootElement);
        JsonNode root = JsonNode.Parse(parsed.RootElement.GetRawText())!;
        DocumentKind kind;
        if (root is JsonArray array)
        {
            JsonObject? first = array.FirstOrDefault() as JsonObject;
            kind = Has(first, "WaveInfo") || Has(first, "maxNPC") || Path.GetFileName(path).Equals("waves.json", StringComparison.OrdinalIgnoreCase) ? DocumentKind.Waves :
                Has(first, "Type") || Path.GetFileName(path).Equals("triggers.json", StringComparison.OrdinalIgnoreCase) ? DocumentKind.Triggers :
                arrayKind ?? throw new InvalidDataException("This array has no recognisable wave or trigger fields. Choose a document type first.");
        }
        else if (root is JsonObject obj)
        {
            kind = Has(obj, "MapName") || Path.GetFileName(path).Equals("map.json", StringComparison.OrdinalIgnoreCase) ? DocumentKind.Map : Schema.NPCTypes.Any(t => Has(obj, t)) || Path.GetFileName(path).Equals("npc.json", StringComparison.OrdinalIgnoreCase) ? DocumentKind.NPC :
                Has(obj, "MainMenuTransform") || Has(obj, "InGameTransform") || Has(obj, "Price") ||
                Path.GetFileName(path).Equals("customization.json", StringComparison.OrdinalIgnoreCase) ? DocumentKind.Customization :
                Has(obj, "Name") || Has(obj, "Type") || Has(obj, "FireRate") || Has(obj, "AnimationType") ? DocumentKind.Weapon :
                throw new InvalidDataException("This JSON is not a recognised weapon, map, NPC or customization configuration.");
        }
        else
            throw new InvalidDataException("The configuration must be a JSON object or array.");
        document = new(kind, root)
        {
            FilePath = Path.GetFullPath(path),
            _diskHash = SHA256.HashData(bytes)
        };
        document.Normalize();
        if (document.Notices.Count != 0)
            document._saved = "";
        return document;
    }

    private static void RejectDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new InvalidDataException("Duplicate JSON property: " + property.Name);
                RejectDuplicates(property.Value);
            }
        }
        if (element.ValueKind == JsonValueKind.Array)
            foreach (JsonElement child in element.EnumerateArray())
                RejectDuplicates(child);
    }

    private static bool Has(JsonObject? obj, string key) => obj?.Any(p => p.Key.Equals(key, StringComparison.OrdinalIgnoreCase) && p.Value != null) == true;

    private void Canonical(JsonObject obj, Field[] fields)
    {
        foreach (Field field in fields)
        {
            string? old = obj.Select(p => p.Key).FirstOrDefault(k => k.Equals(field.Key, StringComparison.OrdinalIgnoreCase));
            if (old != null && old != field.Key)
                Rename(obj, old, field.Key);
            if (field.Kind == FieldKind.Object && obj[field.Key] is JsonObject nested)
                Canonical(nested, field.Children!);
            if (field.Kind == FieldKind.Objects && obj[field.Key] is JsonArray items)
                foreach (JsonObject item in items.OfType<JsonObject>())
                    Canonical(item, field.Children!);
        }
    }

    private void Rename(JsonObject obj, string old, string key)
    {
        if (!obj.ContainsKey(old))
            return;
        if (obj.ContainsKey(key))
            throw new InvalidDataException($"Conflicting fields: {old} / {key}.");
        JsonNode? value = obj[old];
        obj.Remove(old);
        obj[key] = value;
        Notices.Add($"Converted {old} to {key}.");
    }

    private void Alias(JsonObject obj, string old, string key)
    {
        string? found = obj.Select(p => p.Key).FirstOrDefault(k => k.Equals(old, StringComparison.OrdinalIgnoreCase));
        if (found != null && found != key)
            Rename(obj, found, key);
    }

    private void Normalize()
    {
        if (Root is JsonObject obj)
        {
            Canonical(obj, Schema.Fields(Kind));
            if (Kind == DocumentKind.Weapon)
            {
                Alias(obj, "aimPosX", "AimPositionX");
                Alias(obj, "aimPosY", "AimPositionY");
                Alias(obj, "aimPosZ", "AimPositionZ");
            }
            if (Kind == DocumentKind.NPC)
            {
                foreach (string type in Schema.NPCTypes)
                {
                    string? old = obj.Select(p => p.Key).FirstOrDefault(k => k.Equals(type, StringComparison.OrdinalIgnoreCase));
                    if (old != null && old != type)
                        Rename(obj, old, type);
                    if (obj[type] is JsonObject data)
                    {
                        Canonical(data, Schema.NPCData(type));
                        Alias(data, "hp", "Health");
                    }
                }
            }
        }
        if (Root is JsonArray array)
        {
            foreach (JsonObject item in array.OfType<JsonObject>())
            {
                Canonical(item, Schema.Fields(Kind));
                if (Kind == DocumentKind.Waves)
                {
                    Alias(item, "waveInfo", "WaveInfo");
                    if (item["WaveInfo"] is JsonObject info)
                    {
                        Alias(info, "npc", "NPCs");
                        Canonical(info, Schema.WaveInfo);
                    }
                }
            }
        }
    }

    public void SelectNPCType(string type)
    {
        if (Kind != DocumentKind.NPC || !Schema.NPCTypes.Contains(type))
            throw new ArgumentException("Invalid NPC type.");
        JsonObject obj = (JsonObject)Root;
        foreach (string key in Schema.NPCTypes)
        {
            if (obj[key] is { } previous)
                _npcDrafts[key] = previous.DeepClone();
            obj.Remove(key);
        }
        obj[type] = _npcDrafts.TryGetValue(type, out var draft) ? draft.DeepClone() : Schema.Object(Schema.NPCData(type));
    }

    public void Save(string path)
    {
        IReadOnlyList<string> issues = Validator.Check(this);
        if (issues.Count != 0)
            throw new InvalidDataException("Fix validation errors before saving:\n" + string.Join("\n", issues));
        path = Path.GetFullPath(path);
        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Save framework configurations with a .json extension.");
        byte[] bytes = Encoding.UTF8.GetBytes(Json());
        if (bytes.Length > 1024 * 1024)
            throw new InvalidDataException("Configuration exceeds the framework's 1 MB limit.");
        if (string.Equals(FilePath, path, StringComparison.OrdinalIgnoreCase) && _diskHash != null && (!File.Exists(path) || !SHA256.HashData(File.ReadAllBytes(path)).SequenceEqual(_diskHash)))
            throw new IOException("This file changed outside NullTweaker. Save As to a different file or reopen it first.");
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        FilePath = path;
        _diskHash = SHA256.HashData(bytes);
        _saved = Json();
    }

    public static ConfigDocument ImportIni(string text)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        bool section = false;
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            if (line.StartsWith('['))
            {
                section = line.Equals("[CustomMap]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!section || line.StartsWith(';') || line.StartsWith('#'))
                continue;
            int equals = line.IndexOf('=');
            if (equals > 0)
                values[line[..equals].Trim()] = line[(equals + 1)..].Trim();
        }
        if (!values.ContainsKey("mapName"))
            throw new InvalidDataException("No [CustomMap] mapName found in INI.");
        ConfigDocument doc = new(DocumentKind.Map);
        JsonObject obj = (JsonObject)doc.Root;
        foreach (Field field in Schema.Map)
        {
            if (!values.TryGetValue(field.Key, out string? value))
                continue;
            obj[field.Key] = field.Kind switch
            {
                FieldKind.Toggle => JsonValue.Create(bool.Parse(value)),
                FieldKind.Integer => JsonValue.Create(int.Parse(value, CultureInfo.InvariantCulture)),
                FieldKind.Strings => new JsonArray(value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(v => (JsonNode?)JsonValue.Create(v)).ToArray()),
                _ => JsonValue.Create(value)
            };
        }
        obj.Remove("Id");
        return doc;
    }
}
