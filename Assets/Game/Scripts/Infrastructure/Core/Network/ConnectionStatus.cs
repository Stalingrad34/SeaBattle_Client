namespace Game.Scripts.Infrastructure.Core.Network
{
    public enum ConnectionStatus
    {
        Idle,
        Connecting,
        Connected,
        Reconnecting,
        Failed,
        SessionExpired
    }
}
