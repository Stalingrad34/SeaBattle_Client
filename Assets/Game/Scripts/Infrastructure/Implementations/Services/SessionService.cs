using System;
using Game.Scripts.Infrastructure.Core.Network;
using Game.Scripts.Infrastructure.Core.Services;

namespace Game.Scripts.Infrastructure.Implementations.Services
{
    // Persistence and token rotation are implemented in the recovery stage.
    public sealed class SessionService : ISessionService
    {
        public string MatchId { get; private set; }
        public string PlayerId { get; private set; }
        public string ConnectionId { get; private set; }
        public string ResumeToken { get; private set; }

        public void BeginConnection(string matchId, string playerId)
        {
            MatchId = matchId;
            PlayerId = playerId;
            ConnectionId = playerId;
            ResumeToken = null;
        }

        public void AcceptWelcome(ServerMessage welcome)
        {
            if (welcome.type != Protocol.Welcome || string.IsNullOrEmpty(welcome.matchId) || string.IsNullOrEmpty(welcome.playerId) || string.IsNullOrEmpty(welcome.connectionId) || string.IsNullOrEmpty(welcome.resumeToken))
                throw new ArgumentException("Invalid welcome.");
            MatchId = welcome.matchId;
            PlayerId = welcome.playerId;
            ConnectionId = welcome.connectionId;
            ResumeToken = welcome.resumeToken;
        }
    }
}
