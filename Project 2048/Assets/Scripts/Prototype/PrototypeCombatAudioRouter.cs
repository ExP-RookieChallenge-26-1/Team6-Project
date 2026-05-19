using System.Collections.Generic;
using Project2048.Board2048;
using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Prototype
{
    public enum PrototypeCombatEventSoundCue
    {
        None,
        Victory,
        Defeat,
        RewardRest,
        RewardEnhance,
    }

    /// <summary>
    /// Converts board transition data into data-driven board effect cues.
    /// Keep this outside combat core so final audio/UI can replace it cleanly.
    /// </summary>
    public class PrototypeCombatAudioRouter
    {
        public IReadOnlyList<BoardTileEffectCue> GetBoardTileEffectCues(BoardTransition transition)
        {
            if (transition?.Movements == null || transition.Movements.Count == 0)
            {
                return System.Array.Empty<BoardTileEffectCue>();
            }

            var cues = new List<BoardTileEffectCue>();
            var mergedTargets = new HashSet<MergeTargetKey>();
            var addedMoveCue = false;
            foreach (var movement in transition.Movements)
            {
                if (movement == null || movement.Value <= 0)
                {
                    continue;
                }

                if (!addedMoveCue)
                {
                    cues.Add(new BoardTileEffectCue(BoardTileEffectCueType.Move, movement.Value, movement.To));
                    addedMoveCue = true;
                }

                if (!movement.IsMergeParticipant || movement.ResultValue <= 0)
                {
                    continue;
                }

                var key = new MergeTargetKey(movement.To, movement.ResultValue);
                if (mergedTargets.Add(key))
                {
                    cues.Add(new BoardTileEffectCue(BoardTileEffectCueType.Merge, movement.ResultValue, movement.To));
                }
            }

            return cues;
        }

        private readonly struct MergeTargetKey
        {
            public MergeTargetKey(Vector2Int position, int resultValue)
            {
                Position = position;
                ResultValue = resultValue;
            }

            private Vector2Int Position { get; }
            private int ResultValue { get; }
        }
    }
}
