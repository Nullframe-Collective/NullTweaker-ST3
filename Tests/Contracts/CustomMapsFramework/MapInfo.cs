using Il2Cpp;
using System.Text.Json.Serialization;
using UnityEngine;

namespace CustomMapsFramework
{
    /// <summary>
    /// Example file:
    /// {
    ///   "MapName": "MyMap",
    ///   "UseDayAndNight": false,
    ///   "Size": 0,
    ///   "CollectMonster": "Newborn_Bot",
    ///   "VersusMonster": "Newborn_Bot",
    ///   "SurvivalMonster": "Newborn_Bot",
    ///   "IsOutside": false,
    ///   "Version": "1.0.0",
    ///   "Dependencies": []
    /// }
    /// </summary>
    public class MapJson
    {
        [JsonPropertyName("MapName")]
        public string MapName { get; set; } = "";

        [JsonPropertyName("Id")]
        public string Id { get; set; }

        [JsonPropertyName("UseDayAndNight")]
        public bool UseDayAndNight { get; set; } = false;

        [JsonPropertyName("Size")]
        public int Size { get; set; } = 0;

        [JsonPropertyName("CollectMonster")]
        public string CollectMonster { get; set; } = "Newborn_Bot";

        [JsonPropertyName("VersusMonster")]
        public string VersusMonster { get; set; } = "Newborn_Bot";

        [JsonPropertyName("SurvivalMonster")]
        public string SurvivalMonster { get; set; } = "Newborn_Bot";

        [JsonPropertyName("IsOutside")]
        public bool IsOutside { get; set; } = false;

        [JsonPropertyName("Version")]
        public string Version { get; set; } = "1.0.0";

        [JsonPropertyName("Dependencies")]
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    public class MapInfo
    {
        public string Id { get; internal set; }
        public string Name => Map?.mapName;
        public string Path => AssetsPath;
        public string ContentHash { get; internal set; }
        public bool IsAvailable => Error == null;
        public string Error { get; internal set; }
        public LobbyMenu.AllMaps Map;
        public List<CustomWave> Waves;
        public AudioClip Ambient;

        /// <summary>
        /// [0] = CollectMonster, [1] = VersusMonster, [2] = SurvivalMonster
        /// </summary>
        public string[] Monsters;

        public bool IsOutside;
        public string Version;
        public List<string> Dependencies;
        public string BundlePath;
        public string AssetsPath;
        public string[] Scenes;
        public TriggerSettings[] Triggers;
        internal bool MusicLoaded;
    }
}
