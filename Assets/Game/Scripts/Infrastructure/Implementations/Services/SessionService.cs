using Game.Scripts.Infrastructure.Core.Services;

namespace Game.Scripts.Infrastructure.Implementations.Services
{
    public sealed class SessionService : ISessionService
    {
        public string MatchId { get; private set; }
        public string PlayerId { get; private set; }

        public void BeginConnection(string matchId, string playerId)
        {
            MatchId = matchId;
            PlayerId = playerId;
        }
    }
}
