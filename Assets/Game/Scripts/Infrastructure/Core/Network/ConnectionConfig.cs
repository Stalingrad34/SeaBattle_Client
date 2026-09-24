using UnityEngine;

namespace Game.Scripts.Infrastructure.Core.Network
{
    [CreateAssetMenu(menuName = "SeaBattle/Connection Config")]
    public sealed class ConnectionConfig : ScriptableObject
    {
        public string Endpoint = "ws://127.0.0.1:2567";
        public string RoomName = "connection";
        [Min(1)] public int TimeoutSeconds = 10;
    }
}

