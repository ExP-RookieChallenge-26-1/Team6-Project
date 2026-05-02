namespace Project2048.Enemy
{
    [System.Serializable]
    public class EnemyIntent
    {
        public EnemyIntentType intentType;
        public int value;
        public DebuffType debuffType;

        public EnemyIntent Clone()
        {
            return new EnemyIntent
            {
                intentType = intentType,
                value = value,
                debuffType = debuffType,
            };
        }
    }
}
