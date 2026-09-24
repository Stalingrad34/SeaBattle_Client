using System;
using UnityEngine;

namespace Game.Scripts.Infrastructure.Core.Configs
{
    [CreateAssetMenu(menuName = "SeaBattle/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Min(2)]
        public int BoardSize = 6;
        public int[] ShipLengths =
        {
            3,
            2,
            2,
            1
        };
        [Min(1)]
        public int TurnDurationSeconds = 15;
        [Min(1)]
        public int ReconnectGraceSeconds = 120;
        public void Validate()
        {
            if (BoardSize < 2 || BoardSize > 32 || TurnDurationSeconds < 1 || ReconnectGraceSeconds < 1)
                throw new InvalidOperationException("Invalid game configuration.");
            if (ShipLengths == null || ShipLengths.Length == 0)
                throw new InvalidOperationException("At least one ship is required.");
            var cells = 0;
            foreach (var length in ShipLengths)
            {
                if (length < 1 || length > BoardSize)
                    throw new InvalidOperationException("Invalid ship length.");
                cells += length;
            }

            if (cells > BoardSize * BoardSize)
                throw new InvalidOperationException("Fleet exceeds board capacity.");
        }
    }
}
