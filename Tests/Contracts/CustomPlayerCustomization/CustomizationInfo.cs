using System.Text.Json.Serialization;

namespace CustomPlayerCustomization
{
    public class CustomizationInfo
    {
        public string Id { get; set; } = "";
        [JsonIgnore]
        public string ContentHash { get; internal set; }

        public string Name { get; set; } = "";
        [JsonIgnore]
        public string Path { get; set; } = "";
        public int Price { get; set; } = 0;
        public bool IsAvailable { get; set; } = true;
        public string Author { get; set; } = "";
        public CustomizationTransform MainMenuTransform { get; set; } = new();
        public CustomizationTransform InGameTransform { get; set; } = new();
    }
}
