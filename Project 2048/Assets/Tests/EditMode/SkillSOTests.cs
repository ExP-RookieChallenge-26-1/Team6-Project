using System.Reflection;
using NUnit.Framework;
using Project2048.Skills;
using UnityEngine;

namespace Project2048.Tests
{
    public class SkillSOTests
    {
        [Test]
        public void OnValidate_ClampsReusableVfxParameters()
        {
            var skill = ScriptableObject.CreateInstance<SkillSO>();
            try
            {
                skill.vfxScale = 0f;
                skill.vfxIntensity = -1f;
                skill.vfxRepeatCount = 0;

                typeof(SkillSO)
                    .GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(skill, null);

                Assert.That(skill.vfxScale, Is.EqualTo(0.01f).Within(0.0001f));
                Assert.That(skill.vfxIntensity, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(skill.vfxRepeatCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(skill);
            }
        }
    }
}
