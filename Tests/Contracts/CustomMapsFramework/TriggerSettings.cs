using System.Text.Json.Serialization;

namespace CustomMapsFramework
{
    public enum TriggerKind { Event, Damage, Kill, Water, Ambient }

    public sealed class TriggerSettings
    {
        public string Id { get; set; }
        public string Type { get; set; } = "Event";
        public string Scene { get; set; } = "";
        public string Object { get; set; } = "";
        public float[] Position { get; set; } = [0f, 0f, 0f];
        public float[] Rotation { get; set; } = [0f, 0f, 0f];
        public float[] Size { get; set; } = [1f, 1f, 1f];
        public float Damage { get; set; } = 5f;
        public float CoolDown { get; set; } = 1f;
        public float Gravity { get; set; } = 4f;
        public float MaxFallSpeed { get; set; } = 10f;
        public bool Blur { get; set; } = true;
        public string WaterColor { get; set; } = "#35D8FFFF";
        public int Priority { get; set; }
        public string AmbientColor { get; set; } = "#808080";
        public string FogColor { get; set; } = "#808080";
        public bool Fog { get; set; } = true;
        public string FogMode { get; set; } = "ExponentialSquared";
        public float FogDensity { get; set; } = 0.01f;
        public float FogStart { get; set; }
        public float FogEnd { get; set; } = 300f;
        public float TransitionSpeed { get; set; } = 4f;

        [JsonIgnore]
        public TriggerKind Kind => Enum.Parse<TriggerKind>(Type, true);

        internal static bool Vector(float[] value, bool positive = false) => value is { Length: 3 } &&
            value.All(number => float.IsFinite(number) && Math.Abs(number) <= 1000000f && (!positive || number > 0f));

        private static bool Color(string value) => value != null && value.Length is 7 or 9 && value[0] == '#' && value[1..].All(Uri.IsHexDigit);

        internal static void ValidateAll(TriggerSettings[] settings)
        {
            if (settings == null || settings.Length > 256) throw new InvalidDataException("Use at most 256 triggers.");

            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (TriggerSettings item in settings)
            {
                if (item == null || !MapContent.Name(item.Id) || !ids.Add(item.Id) ||
                    !Enum.TryParse(item.Type, true, out TriggerKind kind) || !Enum.IsDefined(typeof(TriggerKind), kind) ||
                    item.Scene == null || item.Scene.Length > 128 || item.Object == null || item.Object.Length > 1024 ||
                    !Vector(item.Position) || !Vector(item.Rotation) || !Vector(item.Size, true) ||
                    !float.IsFinite(item.Damage) || item.Damage is < 0f or > 1000000f ||
                    !float.IsFinite(item.CoolDown) || item.CoolDown is <= 0f or > 3600f ||
                    !float.IsFinite(item.Gravity) || item.Gravity is < 0f or > 1000f ||
                    !float.IsFinite(item.MaxFallSpeed) || item.MaxFallSpeed is <= 0f or > 1000f ||
                    !Color(item.WaterColor) || !Color(item.AmbientColor) || !Color(item.FogColor) ||
                    item.FogMode is not ("Linear" or "Exponential" or "ExponentialSquared") ||
                    !float.IsFinite(item.FogDensity) || item.FogDensity is < 0f or > 1f ||
                    !float.IsFinite(item.FogStart) || !float.IsFinite(item.FogEnd) || item.FogStart < 0f || item.FogEnd < item.FogStart ||
                    !float.IsFinite(item.TransitionSpeed) || item.TransitionSpeed is <= 0f or > 100f)
                {
                    throw new InvalidDataException("Invalid or duplicate trigger: " + item?.Id);
                }
            }
        }
    }
}
