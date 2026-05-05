using System;
using UnityEngine;

namespace Project2048.Presentation
{
    public enum BoardTileEffectCueType
    {
        Move,
        Merge,
    }

    [Serializable]
    public readonly struct BoardTileEffectCue
    {
        public BoardTileEffectCue(BoardTileEffectCueType cueType, int tileValue, Vector2Int position)
        {
            CueType = cueType;
            TileValue = tileValue;
            Position = position;
        }

        public BoardTileEffectCueType CueType { get; }
        public int TileValue { get; }
        public Vector2Int Position { get; }
    }
}
