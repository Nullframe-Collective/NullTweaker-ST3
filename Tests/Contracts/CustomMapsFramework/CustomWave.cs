using UnityEngine;
using System.Text.Json.Serialization;

namespace CustomMapsFramework
{
    public class CustomWave
    {
        public int MaxNPC = 15;
        public CustomWaveInfo WaveInfo;

        public class CustomWaveInfo
        {
            public int TotalCount = 5;
            [JsonIgnore]
            public AudioClip Music;
            public string DefaultNPC = "Newborn_Bot";
            public SpecialNPC[] NPCs;
        }

        public class SpecialNPC
        {
            public string NPCName;
            public int SpawnChance;
            public bool IsBoss;
        }
    }
}
