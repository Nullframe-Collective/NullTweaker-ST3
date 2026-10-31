using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CustomNPCFramework
{
    internal static class NPCManifest
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        internal static NPCInfo Read(string directory)
        {
            string json = File.ReadAllText(System.IO.Path.Combine(directory, "npc.json"));
            NPCInfo info = JsonSerializer.Deserialize<NPCInfo>(json, Options);

            Validate(info);
            ValidateStatistics(info);

            info.Path = directory;
            info.ContentHash = HashContent(json, info.UseDummyModel ? null : System.IO.Path.Combine(directory, "bundle"));
            return info;
        }

        private static void Validate(NPCInfo info)
        {
            if (info == null || string.IsNullOrWhiteSpace(info.Name) || info.Name.Length > 128 ||
                info.Name.Any(character => char.IsControl(character) || "/\\".Contains(character)))
            {
                throw new InvalidDataException("The NPC must have a valid Name.");
            }

            info.Id ??= "legacy:" + string.Concat(info.Name.ToLowerInvariant().Select(character =>
                character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.' ? character : '_'));

            if (info.Id.Length is < 3 or > 128 || !info.Id.Contains(':') ||
                info.Id.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or ':' or '-' or '_' or '.')))
            {
                throw new InvalidDataException("The NPC Id must use a lowercase namespace:name identifier.");
            }

            if (new object[] { info.Bot, info.BossBot, info.CustardBot, info.PlayerMonster }.Count(value => value != null) != 1)
            {
                throw new InvalidDataException("Exactly one NPC type must be specified.");
            }
        }

        private static void ValidateStatistics(NPCInfo info)
        {
            NPCData data = info.Data;

            if (string.IsNullOrWhiteSpace(data.DummyNPC) || data.DummyNPC.Length > 128 ||
                data.DummyNPC.Contains("..") || data.DummyNPC.Any(char.IsControl) ||
                !Positive(info.ModelScale) || info.ModelScale > 10f ||
                (info.Bot != null || info.BossBot != null) && !Positive(data.Health) || !Nonnegative(data.Damage) || !Nonnegative(data.Speed))
            {
                throw new InvalidDataException("Invalid dummy prefab, scale or NPC statistics.");
            }

            if (info.Bot is { } bot && (!Positive(bot.AttackRange) || !Positive(bot.AttackSpeed) ||
                bot.UseRage && (!Nonnegative(bot.RageRunSpeed) || !Positive(bot.RageAttackTime) || bot.RageRequiredHp < 0)) ||
                info.BossBot is { } boss && (!Positive(boss.AttackRange) || !Positive(boss.AttackSpeed) || !Nonnegative(boss.RunSpeed)) ||
                info.CustardBot is { } custard && (!Nonnegative(custard.RunSpeed) || !Nonnegative(custard.JumpSpeed)) ||
                info.PlayerMonster is { } player && (!Nonnegative(player.RunSpeed) || !Nonnegative(player.JumpSpeed)))
            {
                throw new InvalidDataException("Invalid attack, rage or movement statistics.");
            }
        }

        private static string HashContent(string json, string bundlePath)
        {
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            hash.AppendData(Encoding.UTF8.GetBytes(json));

            if (bundlePath != null)
            {
                using FileStream bundle = File.OpenRead(bundlePath);
                byte[] buffer = new byte[81920];
                int count;

                while ((count = bundle.Read(buffer)) != 0)
                {
                    hash.AppendData(buffer, 0, count);
                }
            }

            return Convert.ToHexString(hash.GetHashAndReset());
        }

        private static bool Positive(float value) => float.IsFinite(value) && value > 0;
        private static bool Nonnegative(float value) => float.IsFinite(value) && value >= 0;
    }
}
