using System.Collections.Generic;
using UnityEngine;

namespace Project2048.Story
{
    [CreateAssetMenu(menuName = "Game/Story Data")]
    public class StoryDataSO : ScriptableObject
    {
        public List<StoryStep> steps = new();
    }
}
