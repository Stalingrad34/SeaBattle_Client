using System;
using System.Diagnostics;
using Game.Scripts.Infrastructure.Core.Services;

namespace Game.Scripts.Infrastructure.Implementations.Services
{
    public sealed class ServerTimeService : IServerTimeService
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private double _offset;
        public double NowMs => _clock.Elapsed.TotalMilliseconds + _offset;

        public void Synchronize(double serverTimeMs, double roundTripMs)
        {
            if (double.IsNaN(serverTimeMs) || double.IsInfinity(serverTimeMs) || double.IsNaN(roundTripMs) || double.IsInfinity(roundTripMs) || roundTripMs < 0)
                throw new ArgumentOutOfRangeException(nameof(serverTimeMs));
            _offset = serverTimeMs + roundTripMs / 2 - _clock.Elapsed.TotalMilliseconds;
        }

        public double RemainingSeconds(double deadlineMs)
        {
            return Math.Max(0, (deadlineMs - NowMs) / 1000);
        }
    }
}
