using NUnit.Framework;
using Project2048.Combat;
using UnityEngine;

namespace Project2048.Tests
{
    public class PlayerCombatControllerTests
    {
        [Test]
        public void Init_StoresPlayerData_ForUiPortraitBinding()
        {
            var playerObject = new GameObject("Player");
            var playerData = ScriptableObject.CreateInstance<PlayerSO>();

            try
            {
                var player = playerObject.AddComponent<PlayerCombatController>();

                player.Init(playerData);

                Assert.That(player.Data, Is.SameAs(playerData));
            }
            finally
            {
                Object.DestroyImmediate(playerData);
                Object.DestroyImmediate(playerObject);
            }
        }
    }
}
