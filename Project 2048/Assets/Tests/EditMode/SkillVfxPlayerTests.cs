using NUnit.Framework;
using Project2048.Presentation;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class SkillVfxPlayerTests
    {
        private static SpriteRenderer MakeUnitSprite(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(8, 8);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8f); // 1 unit, center pivot
            return sr;
        }

        [Test]
        public void ResolvePlacement_EnemyFeet_IsBelowEnemyCenter()
        {
            var player = MakeUnitSprite("P", new Vector3(-1, 0, 0));
            var enemy = MakeUnitSprite("E", new Vector3(1, 0, 0));
            var ctx = new SkillVfxContext(player.transform, enemy.transform, SkillVfxTrigger.ChargeRelease);
            var placement = new SkillVfxPlacement { target = SkillVfxTarget.Enemy, vertical = SkillVfxVertical.Feet };

            var pos = SkillVfxPlayer.ResolvePlacementWorldPosition(placement, ctx);

            Assert.That(pos.y, Is.LessThan(enemy.bounds.center.y - 0.1f)); // feet below center
            Assert.That(pos.x, Is.EqualTo(1f).Within(0.001f));

            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(enemy.gameObject);
        }
    }
}
