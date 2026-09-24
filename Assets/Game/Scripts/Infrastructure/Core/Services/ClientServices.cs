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
        string RoomId { get; }

        string SessionId { get; }

        IObservable<string> Messages { get; }

        IObservable<MatchState> StateChanged { get; }

        IObservable<int> Disconnected { get; }

        UniTask ConnectAsync(CancellationToken token);
        UniTask ProbeAsync(CancellationToken token);
        UniTask SendAsync(ClientMessage message, CancellationToken token);
    }

    public interface ISessionService : IService
    {
        string MatchId { get; }

        string PlayerId { get; }

        string ConnectionId { get; }

        string ResumeToken { get; }

        void BeginConnection(string matchId, string playerId);
        void AcceptWelcome(ServerMessage welcome);
    }

    public interface IMatchService : IService, IDisposable
    {
        IReadOnlyReactiveProperty<MatchState> State { get; }

        bool Apply(MatchState state);
    }

    public interface IServerTimeService : IService
    {
        double NowMs { get; }

        void Synchronize(double serverTimeMs, double roundTripMs);
        double RemainingSeconds(double deadlineMs);
    }

    public interface INetworkDiagnostics : IService, IDisposable
    {
        IObservable<string> Entries { get; }

        void Record(string direction, string messageType);
    }
}
