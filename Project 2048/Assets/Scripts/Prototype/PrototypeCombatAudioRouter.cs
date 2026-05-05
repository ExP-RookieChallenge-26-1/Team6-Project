using System.Collections.Generic;
using System.Linq;
using Project2048.Board2048;
using Project2048.Combat;
using Project2048.Presentation;
using UnityEngine;

namespace Project2048.Prototype
{
    /// <summary>
    /// Temporary prototype cue names consumed by <see cref="CombatUiView"/>.
    /// These are not game audio rules; they only describe visible combat changes.
    /// </summary>
    public enum PrototypeCombatSoundCue
    {
        PlayerHit,
        EnemyHit,
        BoardMove,
        BoardMerge,
    }

    /// <summary>
    /// Converts combat snapshots and board transition data into temporary UI SFX cues.
    /// Keep this outside combat core so final audio/UI can replace it cleanly.
    /// </summary>
    public class PrototypeCombatAudioRouter
    {
        private CombatSnapshot previousSnapshot;

        public void Reset(CombatSnapshot snapshot)
        {
            previousSnapshot = snapshot;
        }

        public IReadOnlyList<PrototypeCombatSoundCue> GetSnapshotCues(CombatSnapshot nextSnapshot)
        {
            var cues = new List<PrototypeCombatSoundCue>();
            if (previousSnapshot == null || nextSnapshot == null)
            {
                previousSnapshot = nextSnapshot;
                return cues;
            }

            if (PlayerWasHit(previousSnapshot, nextSnapshot))
            {
                cues.Add(PrototypeCombatSoundCue.PlayerHit);
            }

            if (EnemyWasHit(previousSnapshot, nextSnapshot))
            {
                cues.Add(PrototypeCombatSoundCue.EnemyHit);
            }

            previousSnapshot = nextSnapshot;
            return cues;
        }

        public IReadOnlyList<PrototypeCombatSoundCue> GetBoardTransitionCues(BoardTransition transition)
        {
            if (transition?.Movements == null ||
                !transition.Movements.Any(movement => movement != null))
            {
                return System.Array.Empty<PrototypeCombatSoundCue>();
            }

            var cues = new List<PrototypeCombatSoundCue>
            {
                PrototypeCombatSoundCue.BoardMove,
            };

            if (transition.Movements.Any(movement => movement != null && movement.IsMergeParticipant))
            {
                cues.Add(PrototypeCombatSoundCue.BoardMerge);
            }

            return cues;
        }

        public IReadOnlyList<BoardTileEffectCue> GetBoardTileEffectCues(BoardTransition transition)
        {
            if (transition?.Movements == null || transition.Movements.Count == 0)
            {
                return System.Array.Empty<BoardTileEffectCue>();
            }

            var cues = new List<BoardTileEffectCue>();
            var mergedTargets = new HashSet<MergeTargetKey>();
            foreach (var movement in transition.Movements)
            {
                if (movement == null || movement.Value <= 0)
                {
                    continue;
                }

                cues.Add(new BoardTileEffectCue(BoardTileEffectCueType.Move, movement.Value, movement.To));

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

        private static bool PlayerWasHit(CombatSnapshot previous, CombatSnapshot next)
        {
            if (previous.Player == null || next.Player == null)
            {
                return false;
            }

            if (next.Player.CurrentHp < previous.Player.CurrentHp)
            {
                return true;
            }

            return next.Phase == CombatPhase.EnemyTurn &&
                next.Player.Block < previous.Player.Block;
        }

        private static bool EnemyWasHit(CombatSnapshot previous, CombatSnapshot next)
        {
            if (previous.Enemies == null || next.Enemies == null)
            {
                return false;
            }

            foreach (var nextEnemy in next.Enemies)
            {
                var previousEnemy = previous.Enemies.FirstOrDefault(enemy => enemy.EnemyIndex == nextEnemy.EnemyIndex);
                if (previousEnemy != null &&
                    (nextEnemy.CurrentHp < previousEnemy.CurrentHp || nextEnemy.Block < previousEnemy.Block))
                {
                    return true;
                }
            }

            return false;
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
