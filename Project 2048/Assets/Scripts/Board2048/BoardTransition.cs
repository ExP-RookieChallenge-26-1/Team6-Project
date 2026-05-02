using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project2048.Board2048
{
    [Serializable]
    public class BoardTransition
    {
        public Direction Direction { get; set; }
        public int[,] Before { get; set; }
        public int[,] After { get; set; }
        public List<BoardTileMovement> Movements { get; } = new();
        public List<BoardTileSpawn> Spawns { get; } = new();
    }

    [Serializable]
    public class BoardTileMovement
    {
        public int Value { get; set; }
        public Vector2Int From { get; set; }
        public Vector2Int To { get; set; }
        public bool IsMergeParticipant { get; set; }
        public int ResultValue { get; set; }
    }

    [Serializable]
    public class BoardTileSpawn
    {
        public int Value { get; set; }
        public Vector2Int Position { get; set; }
    }
}
