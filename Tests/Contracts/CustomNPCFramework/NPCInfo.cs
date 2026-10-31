using System.Text.Json.Serialization;
using UnityEngine;

namespace CustomNPCFramework
{
    public class NPCInfo
    {
        public string Name;
        public string Version = "1.0.0";
        public string Id { get; set; }
        public BotData Bot;
        public BossBotData BossBot;
        public CustardBotData CustardBot;
        public PlayerMonsterData PlayerMonster;
        public bool UseDummyModel { get; set; }
        public float ModelScale { get; set; } = 1f;
        public string ModelColor { get; set; }

        [JsonIgnore] public Sprite IconSprite;
        [JsonIgnore] public string Path;
        [JsonIgnore] public string ContentHash { get; internal set; }
        [JsonIgnore] public string Error { get; internal set; }
        [JsonIgnore] public bool IsAvailable => Error == null;
        [JsonIgnore] public bool IsBoss => BossBot != null && Bot == null;
        [JsonIgnore] public NPCType Type => Bot != null ? NPCType.Bot : BossBot != null ? NPCType.BossBot :
            CustardBot != null ? NPCType.CustardBot : NPCType.PlayerMonster;
        [JsonIgnore] internal NPCData Data => (NPCData)Bot ?? BossBot ?? (NPCData)CustardBot ?? PlayerMonster;
    }
}
