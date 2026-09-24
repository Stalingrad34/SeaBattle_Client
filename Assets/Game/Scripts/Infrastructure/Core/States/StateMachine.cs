using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Scripts.Infrastructure.Core.States
{
    /// <summary>Transitions are serialized; a newer request cancels the previous state's work.</summary>
    public sealed class StateMachine : IDisposable
    {
        private readonly StateFactory _factory;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly CancellationTokenSource _lifetime = new();
        private CancellationTokenSource _stateLifetime;
        private IState _current;
        private int _version;
        private bool _disposed;
        public Type CurrentType => _current?.GetType();

        public StateMachine(StateFactory factory)
        {
            _factory = factory;
        }

        public async UniTask EnterAsync<T>(CancellationToken token)
            where T : class, IState
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StateMachine));
            var version = ++_version;
            _stateLifetime?.Cancel();
            using var request = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
            await _gate.WaitAsync(request.Token);
            try
            {
                request.Token.ThrowIfCancellationRequested();
                if (version != _version)
                    return;
                ExitCurrent();
                _stateLifetime = CancellationTokenSource.CreateLinkedTokenSource(token, _lifetime.Token);
                _current = _factory.Create<T>();
                await _current.EnterAsync(_stateLifetime.Token);
            }
            finally
            {
                _gate.Release();
            }
        }

        private void ExitCurrent()
        {
            _stateLifetime?.Cancel();
            _current?.Exit();
            _current = null;
            _stateLifetime?.Dispose();
            _stateLifetime = null;
        }

        public void Reset()
        {
            ++_version;
            ExitCurrent();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _lifetime.Cancel();
            ExitCurrent();
            // Waiters may still be unwinding; do not dispose their semaphore.
            _lifetime.Dispose();
        }
    }
}
