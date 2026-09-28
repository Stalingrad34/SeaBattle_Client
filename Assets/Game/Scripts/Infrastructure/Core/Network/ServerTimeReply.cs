using System;

namespace Game.Scripts.Infrastructure.Core.Network
{
    [Serializable]
    public sealed class ServerTimeReply
    {
        public long requestId;
        public double serverTimeMs;
    }
}
