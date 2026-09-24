using System;

namespace Game.Scripts.Infrastructure.Core.Network
{
    [Serializable]
    public sealed class FireCommand
    {
        public string commandId;
        public long turnId;
        public int x;
        public int y;
    }
}
