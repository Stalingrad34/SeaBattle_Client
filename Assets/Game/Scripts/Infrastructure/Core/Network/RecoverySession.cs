using System;

namespace Game.Scripts.Infrastructure.Core.Network
{
    [Serializable]
    public sealed class RecoverySession
    {
        public string endpoint;
        public string roomId;
        public string playerId;
        public string reconnectionToken;
        public FireCommand pending;
    }
}
