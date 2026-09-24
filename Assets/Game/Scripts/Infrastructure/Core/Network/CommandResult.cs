using System;

namespace Game.Scripts.Infrastructure.Core.Network
{
    [Serializable]
    public sealed class CommandResult
    {
        public string commandId;
        public string status;
        public string reason;
        public long turnId;
        public long revision;
    }
}
