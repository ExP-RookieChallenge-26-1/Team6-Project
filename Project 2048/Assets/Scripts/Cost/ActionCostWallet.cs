using System;

namespace Project2048.Cost
{
    [Serializable]
    public class ActionCostWallet
    {
        public int CurrentCost { get; private set; }

        public event Action<int> OnCostChanged;

        public void SetCost(int value)
        {
            CurrentCost = Math.Max(0, value);
            OnCostChanged?.Invoke(CurrentCost);
        }

        public void AddCost(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentCost += amount;
            OnCostChanged?.Invoke(CurrentCost);
        }

        public bool CanSpend(int amount)
        {
            return amount >= 0 && CurrentCost >= amount;
        }

        public bool Spend(int amount)
        {
            if (!CanSpend(amount))
            {
                return false;
            }

            CurrentCost -= amount;
            OnCostChanged?.Invoke(CurrentCost);
            return true;
        }

        public void Clear()
        {
            CurrentCost = 0;
            OnCostChanged?.Invoke(CurrentCost);
        }
    }
}
