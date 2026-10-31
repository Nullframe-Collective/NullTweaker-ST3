namespace CustomNPCFramework
{
    public abstract class NPCData
    {
        public string DummyNPC;
        public float Health;
        public float Damage;
        public float Speed;
    }

    public class BotData : NPCData
    {
        public bool UseRagdoll;
        public float AttackRange;
        public float AttackSpeed;
        public bool UseRage;
        public int RageRequiredHp;
        public float RageAttackTime;
        public float RageRunSpeed;
    }

    public class BossBotData : NPCData
    {
        public float RunSpeed;
        public float AttackRange;
        public float AttackSpeed;
    }

    public class CustardBotData : NPCData
    {
        public float RunSpeed;
        public float JumpSpeed;
    }

    public class PlayerMonsterData : CustardBotData { }
}
