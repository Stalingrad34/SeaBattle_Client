using Game.Scripts.Infrastructure.Core.Services;
using Game.Scripts.Multiplayer.Generated;
using UniRx;

namespace Game.Scripts.Infrastructure.Implementations.Services
{
    public sealed class MatchService : IMatchService
    {
        private readonly ISessionService _session;
        private readonly ReactiveProperty<MatchState> _state = new();
        private long _revision = -1;
        public IReadOnlyReactiveProperty<MatchState> State => _state;

        public MatchService(ISessionService session)
        {
            _session = session;
        }

        public bool Apply(MatchState state)
        {
            if (state == null || string.IsNullOrEmpty(_session.MatchId) || state.matchId != _session.MatchId || state.players == null || !state.players.ContainsKey(_session.PlayerId) || state.revision <= _revision)
                return false;
            _revision = state.revision;
            // Colyseus patches the same instance in place; notify after each accepted patch.
            // Consumers read this state; only the server writes it.
            _state.SetValueAndForceNotify(state);
            return true;
        }

        public void Dispose()
        {
            _state.Dispose();
        }
    }
}
