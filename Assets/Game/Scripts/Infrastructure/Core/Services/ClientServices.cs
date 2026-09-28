using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.Network;
using UniRx;
using Game.Scripts.Multiplayer.Generated;

namespace Game.Scripts.Infrastructure.Core.Services
{
    public interface ITransportService : IService, IDisposable
    {
        MatchState CurrentState { get; }
        IReadOnlyReactiveProperty<ConnectionStatus> Status { get; }
        bool CanResume { get; }

        IObservable<CommandResult> CommandResults { get; }

        IObservable<MatchState> StateChanged { get; }

        UniTask ConnectAsync(string roomName, bool create, CancellationToken token);
        UniTask<bool> ResumeAsync(CancellationToken token);
        UniTask SendAsync(FireCommand command, CancellationToken token);
        void Disconnect();
        void Leave();
    }

    public interface ISessionService : IService
    {
        string MatchId { get; }

        string PlayerId { get; }
        RecoverySession Recovery { get; }

        void SaveConnection(string endpoint, string matchId, string playerId, string reconnectionToken);
        void SavePending(FireCommand command);
        void Clear();
    }

    public interface ISessionStorage
    {
        string Read();
        void Write(string value);
        void Clear();
    }

    public interface IMatchService : IService, IDisposable
    {
        IReadOnlyReactiveProperty<MatchState> State { get; }

        bool Apply(MatchState state);
        void Reset();
    }

    public interface IServerTimeService : IService
    {
        double NowMs { get; }

        void Synchronize(double serverTimeMs, double roundTripMs);
        double RemainingSeconds(double deadlineMs);
    }

    public interface INetworkDiagnostics : IService, IDisposable
    {
        void Record(string direction, string messageType);
    }
}
