using System.Text.Json.Nodes;

namespace NullTweaker.Core;

public enum DocumentKind
{
    Weapon, NPC, Map, Waves, Triggers, Customization
}
public enum FieldKind
{
    Text, Integer, Number, Toggle, Choice, Vector, Strings, Objects, Object
}
public sealed record Option(string Label, object Value);
public sealed record Field(string Key, string Label, string Group, FieldKind Kind, object? Default = null,
    double? Min = null, double? Max = null, string Hint = "", Option[]? Options = null, Field[]? Children = null);

public static class Schema
{
    public static readonly string[] NPCTypes = ["Bot", "BossBot", "CustardBot", "PlayerMonster"];

    private static Field Text(string key, string label, string group, string value = "", string hint = "") =>
        new(key, label, group, FieldKind.Text, value, Hint: hint);
    private static Field Num(string key, string label, string group, double value, double min = 0, double max = 1000000) =>
        new(key, label, group, FieldKind.Number, value, min, max);
    private static Field Int(string key, string label, string group, int value, int min = 0, int max = 1000000) =>
        new(key, label, group, FieldKind.Integer, value, min, max);
    private static Field Bool(string key, string label, string group, bool value = false) =>
        new(key, label, group, FieldKind.Toggle, value);
    private static Field Choice(string key, string label, string group, object value, params Option[] options) =>
        new(key, label, group, FieldKind.Choice, value, Options: options);

    public static readonly Field[] Weapon =
    [
        Text("Name", "Prefab name", "Identity", "MyWeapon", "Exact bundle asset name. Requires Name and Name (TPS), plus legacy Animation."),
        Text("Id", "Pack ID", "Identity", "author:my-weapon", "Stable lowercase namespace:name. Keep it unchanged after distribution."),
        Choice("Type", "Weapon type", "Identity", 0, new("Automatic", 0), new("Grenade launcher", 1), new("Shotgun", 2), new("Knife", 3)),
        Choice("AnimationType", "Player animation", "Identity", 1, new("One hand", 0), new("Two hand rifle", 1), new("Two hand pistol", 2)),
        Num("FireRate", "Fire rate", "Firing", 0.15), Bool("SingleFire", "Single fire", "Firing"),
        Int("Fractions", "Shotgun pellets", "Firing", 8, 1), Int("ProjectileSpeed", "Projectile speed", "Firing", 50),
        Num("ShotDelay", "Shot delay", "Firing", 0), Num("HitDelay", "Melee hit delay", "Firing", 0.15),
        Int("BulletsPerClip", "Magazine capacity", "Ammunition", 30), Int("Clips", "Spare magazines", "Ammunition", 5),
        Num("ReloadTime", "Reload time", "Ammunition", 2), Num("WaitBeforeReload", "Delay before reload", "Ammunition", 0),
        Bool("CanAim", "Allow aiming", "Aiming", true), Bool("UseScope", "Use scope texture", "Aiming"),
        Bool("AimAnimation", "Play aim animation", "Aiming"), Int("AimFov", "Aim field of view", "Aiming", 55, 1, 179),
        Num("AimPositionX", "Aim offset X", "Aiming", 0, -10000), Num("AimPositionY", "Aim offset Y", "Aiming", 0, -10000),
        Num("AimPositionZ", "Aim offset Z", "Aiming", 0, -10000),
        Num("Recoil", "Hip recoil", "Recoil", 1), Num("AimRecoil", "Aim recoil", "Recoil", 0.5),
        Num("RecoilPower", "Recoil power", "Recoil", 1), Num("ShakePower", "Camera shake", "Recoil", 0.1),
        Num("BulletLifeTime", "Bullet lifetime", "Projectile", 10), Int("BulletDamage", "Bullet damage", "Projectile", 15),
        new("BulledSpeed", "Bullet speed", "Projectile", FieldKind.Integer, 500, 0, 1000000, "BulledSpeed is the exact field spelling required by the framework."),
        Int("BulletForce", "Impact force", "Projectile", 17), Bool("BulletParticles", "Bullet particles", "Projectile", true),
        Bool("IsTool", "Tool behaviour", "Availability"), Bool("SandboxAvailable", "Show in Sandbox", "Availability", true),
        Bool("ShopAvailable", "Available in shop", "Availability"), Int("ShopPrice", "Shop price", "Availability", 100),
        Text("ShopInfo", "Shop description", "Availability")
    ];

    public static readonly Field[] NPC =
    [
        Text("Name", "NPC name", "Identity", "MyMonster", "Must match the model prefab in the bundle."),
        Text("Id", "Pack ID", "Identity", "author:my-monster"), Text("Version", "Content version", "Identity", "1.0.0"),
        Bool("UseDummyModel", "Reuse the game's model", "Appearance", true),
        Num("ModelScale", "Model scale", "Appearance", 1, 0.001, 10),
        Text("ModelColor", "Model colour", "Appearance", "#A06EFF", "HTML colour, for example #A06EFF or #A06EFFFF. Empty keeps the native colour.")
    ];

    public static Field[] NPCData(string type)
    {
        List<Field> fields =
        [
            Text("DummyNPC", "Native prefab path", "Behaviour", type == "Bot" ? "SUR/Newborn_Bot" : "",
                "Editable resource path. Must contain the matching native behaviour; this app cannot inspect game prefabs."),
            Num("Health", "Health", "Behaviour", 350, type is "Bot" or "BossBot" ? 0.001 : 0),
            Num("Damage", "Damage", "Behaviour", 12), Num("Speed", "Walk speed", "Behaviour", 2)
        ];
        if (type is "Bot" or "BossBot")
        {
            fields.Add(Num("AttackRange", "Attack range", "Attack", 2, 0.001));
            fields.Add(Num("AttackSpeed", "Attack interval", "Attack", 1, 0.001));
        }
        if (type == "Bot")
        {
            fields.AddRange([Bool("UseRagdoll", "Use a ragdoll", "Attack", true), Bool("UseRage", "Enable rage", "Rage"),
                Int("RageRequiredHp", "Rage health threshold", "Rage", 100), Num("RageAttackTime", "Rage attack interval", "Rage", 0.5, 0.001),
                Num("RageRunSpeed", "Rage run speed", "Rage", 5)]);
        }
        if (type != "Bot")
            fields.Add(Num("RunSpeed", "Run speed", "Movement", 4));
        if (type is "CustardBot" or "PlayerMonster")
            fields.Add(Num("JumpSpeed", "Jump speed", "Movement", 2));
        return fields.ToArray();
    }

    public static readonly Field[] CustomizationTransform =
    [
        new("Position", "World position", "Transform", FieldKind.Vector, new double[]{0,0,0}, -float.MaxValue, float.MaxValue,
            "Applied first. The framework then sets Local position relative to the attachment parent."),
        new("LocalPosition", "Local position", "Transform", FieldKind.Vector, new double[]{0,0,0}, -float.MaxValue, float.MaxValue,
            "Final position relative to the attachment parent; applied after World position."),
        new("Rotation", "Local rotation (degrees)", "Transform", FieldKind.Vector, new double[]{0,0,0}, -float.MaxValue, float.MaxValue),
        new("Scale", "Local scale", "Transform", FieldKind.Vector, new double[]{1,1,1}, -float.MaxValue, float.MaxValue,
            "XYZ scale. Negative values mirror the model; a zero axis collapses it.")
    ];

    public static readonly Field[] Customization =
    [
        Text("Name", "Prefab name", "Identity", "MyCustomization", "Exact bundle asset name. Requires Name for the menu and Name (TPS) for the game."),
        Text("Id", "Pack ID", "Identity", "author:my-customization", "Optional stable namespace:name. Without an ID, the framework derives one from the folder. The base: prefix is reserved."),
        Text("Author", "Author", "Identity"),
        Int("Price", "Price", "Availability", 0, 0, int.MaxValue),
        Bool("IsAvailable", "Available in customization", "Availability", true),
        new("MainMenuTransform", "Main menu transform", "Transforms", FieldKind.Object, Children: CustomizationTransform,
            Hint: "Placement of the menu prefab."),
        new("InGameTransform", "In-game transform", "Transforms", FieldKind.Object, Children: CustomizationTransform,
            Hint: "Placement of the TPS prefab on the player.")
    ];

    public static readonly Field[] Map =
    [
        Text("MapName", "Scene name", "Identity", "My Map", "Exact Unity scene name, without .unity. Day/night adds (Day), (Dusk), (Night)."),
        Text("Id", "Pack ID", "Identity", "author:my-map"), Text("Version", "Content version", "Identity", "1.0.0"),
        Bool("UseDayAndNight", "Day / dusk / night scenes", "Environment"), Bool("IsOutside", "Outdoor map", "Environment"),
        Int("Size", "Map size index", "Environment", 0, 0, int.MaxValue),
        Text("CollectMonster", "Collect monster", "Monsters", "Newborn_Bot"),
        Text("VersusMonster", "Versus monster", "Monsters", "Newborn"),
        Text("SurvivalMonster", "Survival monster", "Monsters", "Newborn_Bot"),
        new("Dependencies", "NPC pack dependencies", "Dependencies", FieldKind.Strings, Hint: "Pack IDs or NPC names. One entry per line.")
    ];

    public static readonly Field[] SpecialNPC =
    [
        Text("NPCName", "NPC prefab / custom name", "Spawn", "Newborn_Runner_Bot"),
        Int("SpawnChance", "Spawn chance", "Spawn", 10, 0, 100), Bool("IsBoss", "Boss NPC", "Spawn")
    ];

    public static readonly Field[] WaveInfo =
    [
        Int("TotalCount", "Total NPCs", "Wave", 10, 1, 10000), Text("DefaultNPC", "Default NPC", "Wave", "Newborn_Bot"),
        new("NPCs", "Special NPCs", "Special NPCs", FieldKind.Objects, Children: SpecialNPC)
    ];

    public static readonly Field[] Wave = [Int("MaxNPC", "Maximum alive at once", "Wave", 15, 1, 128)];

    public static readonly Field[] Trigger =
    [
        Text("Id", "Trigger ID", "Identity", "trigger-1"),
        Choice("Type", "Trigger type", "Identity", "Event", new("Event", "Event"), new("Damage", "Damage"), new("Kill", "Kill"), new("Water", "Water"), new("Ambient", "Ambient")),
        Text("Scene", "Scene filter", "Placement", "", "Empty applies to every scene variant."),
        Text("Object", "Existing object path", "Placement", "", "Optional path from the scene root. Otherwise a volume is created."),
        new("Position", "World position", "Placement", FieldKind.Vector, new double[]{0,0,0}, -1000000, 1000000),
        new("Rotation", "Rotation (degrees)", "Placement", FieldKind.Vector, new double[]{0,0,0}, -1000000, 1000000),
        new("Size", "Volume size", "Placement", FieldKind.Vector, new double[]{1,1,1}, 0.001, 1000000),
        Num("Damage", "Damage per tick", "Effects", 5), Num("CoolDown", "Tick interval", "Effects", 1, 0.001, 3600),
        Num("Gravity", "Water gravity", "Water", 4, 0, 1000), Num("MaxFallSpeed", "Water fall speed limit", "Water", 10, 0.001, 1000),
        Bool("Blur", "Underwater blur", "Water", true), Text("WaterColor", "Water colour", "Water", "#35D8FFFF"),
        Int("Priority", "Zone priority", "Effects", 0, int.MinValue, int.MaxValue),
        Text("AmbientColor", "Ambient colour", "Ambient", "#808080"), Bool("Fog", "Enable fog", "Ambient", true),
        Text("FogColor", "Fog colour", "Ambient", "#808080"),
        Choice("FogMode", "Fog mode", "Ambient", "ExponentialSquared", new("Linear", "Linear"), new("Exponential", "Exponential"), new("Exponential squared", "ExponentialSquared")),
        Num("FogDensity", "Fog density", "Ambient", 0.01, 0, 1), Num("FogStart", "Fog start", "Ambient", 0), Num("FogEnd", "Fog end", "Ambient", 300),
        Num("TransitionSpeed", "Transition speed", "Ambient", 4, 0.001, 100)
    ];

    public static Field[] Fields(DocumentKind kind) => kind switch
    {
        DocumentKind.Weapon => Weapon,
        DocumentKind.NPC => NPC,
        DocumentKind.Map => Map,
        DocumentKind.Waves => Wave,
        DocumentKind.Customization => Customization,
        _ => Trigger
    };

    public static JsonObject Object(Field[] fields)
    {
        JsonObject result = new();
        foreach (Field field in fields)
        {
            result[field.Key] = field.Kind == FieldKind.Object ? Object(field.Children!) :
                field.Kind is FieldKind.Strings or FieldKind.Objects ? new JsonArray() :
                System.Text.Json.JsonSerializer.SerializeToNode(field.Default);
        }
        return result;
    }

    public static JsonObject NewWave()
    {
        JsonObject wave = Object(Wave);
        wave["WaveInfo"] = Object(WaveInfo);
        return wave;
    }

    public static JsonNode New(DocumentKind kind) => kind switch
    {
        DocumentKind.NPC => NewNPC(),
        DocumentKind.Waves => new JsonArray(NewWave()),
        DocumentKind.Triggers => new JsonArray(),
        _ => Object(Fields(kind))
    };

    private static JsonObject NewNPC()
    {
        JsonObject npc = Object(NPC);
        npc["Bot"] = Object(NPCData("Bot"));
        return npc;
    }

    public static string FileName(DocumentKind kind) => kind switch
    {
        DocumentKind.Weapon => "weapon.json",
        DocumentKind.NPC => "npc.json",
        DocumentKind.Map => "map.json",
        DocumentKind.Waves => "waves.json",
        DocumentKind.Customization => "customization.json",
        _ => "triggers.json"
    };
}
