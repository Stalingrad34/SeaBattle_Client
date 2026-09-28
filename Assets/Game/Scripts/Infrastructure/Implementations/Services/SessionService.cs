using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Infrastructure.Core.Network;
using UnityEngine;

namespace Game.Scripts.Infrastructure.Implementations.Services
{
    public sealed class SessionService : ISessionService
    {
        private readonly ISessionStorage _storage;
        public RecoverySession Recovery { get; private set; } = new();
        public string MatchId => Recovery.roomId;
        public string PlayerId => Recovery.playerId;

        public SessionService(ISessionStorage storage = null)
        {
            _storage = storage;
            var json = storage?.Read();
            if (string.IsNullOrEmpty(json))
                return;
            try
            {
                Recovery = JsonUtility.FromJson<RecoverySession>(json) ?? new RecoverySession();
                // JsonUtility restores an inline null class as an empty object.
                if (string.IsNullOrEmpty(Recovery.pending?.commandId))
                    Recovery.pending = null;
            }
            catch (System.ArgumentException)
            {
                storage.Clear();
            }
        }

        public void SaveConnection(string endpoint, string matchId, string playerId, string reconnectionToken)
        {
            Recovery.roomId = matchId;
            Recovery.playerId = playerId;
            Recovery.endpoint = endpoint;
            Recovery.reconnectionToken = reconnectionToken;
            Save();
        }

        public void SavePending(FireCommand command)
        {
            Recovery.pending = command;
            Save();
        }

        public void Clear()
        {
            Recovery = new RecoverySession();
            _storage?.Clear();
        }

        private void Save()
        {
            _storage?.Write(JsonUtility.ToJson(Recovery));
        }
    }
}
