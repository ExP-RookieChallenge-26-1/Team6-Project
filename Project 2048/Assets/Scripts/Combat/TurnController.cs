namespace Project2048.Combat
{
    [System.Serializable]
    public class TurnController
    {
        public int TurnCount { get; private set; }

        public void Reset()
        {
            TurnCount = 0;
        }

        public void StartPlayerTurn()
        {
            TurnCount++;
        }

        public void StartEnemyTurn()
        {
        }
    }
}
