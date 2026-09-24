using UnityEngine;

namespace Game.Scripts.Infrastructure.Core.Network
{
    [CreateAssetMenu(menuName = "SeaBattle/Connection Config")]
    public sealed class ConnectionConfig : ScriptableObject
    {
        public string Endpoint = "ws://127.0.0.1:2567";
        public string RoomName = "connection";
        [Min(1)]
        public int TimeoutSeconds = 10;
        [Min(0.1f)]
        public float HeartbeatIntervalSeconds = 2;
        [Min(1)]
        public float SilenceTimeoutSeconds = 8;
        [Min(0.1f)]
        public float RetryDelaySeconds = 1;
        [Min(0)]
        public int LatencyMilliseconds;
        [Min(0)]
        public int JitterMilliseconds;
        [Range(0, 1)]
        public float LossProbability;
        [Range(0, 1)]
        public float DuplicateProbability;
        public int NetworkSeed = 42;
        public void Validate()
        {
            if (!System.Uri.TryCreate(Endpoint, System.UriKind.Absolute, out var uri) || (uri.Scheme != "ws" && uri.Scheme != "wss") || string.IsNullOrWhiteSpace(RoomName) || TimeoutSeconds < 1 || HeartbeatIntervalSeconds <= 0 || SilenceTimeoutSeconds <= HeartbeatIntervalSeconds || RetryDelaySeconds <= 0 || LatencyMilliseconds < 0 || JitterMilliseconds < 0 || LossProbability < 0 || LossProbability > 1 || DuplicateProbability < 0 || DuplicateProbability > 1)
                throw new System.InvalidOperationException("Invalid connection configuration.");
        }
    }
}
