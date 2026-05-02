using NUnit.Framework;
using Project2048.Cost;

namespace Project2048.Tests
{
    public class ActionCostWalletTests
    {
        [Test]
        public void Spend_DoesNotChangeCurrentCost_WhenInsufficient()
        {
            var wallet = new ActionCostWallet();
            wallet.SetCost(3);

            var spent = wallet.Spend(5);

            Assert.That(spent, Is.False);
            Assert.That(wallet.CurrentCost, Is.EqualTo(3));
        }
    }
}
