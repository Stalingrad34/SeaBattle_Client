using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Game.Scripts.Infrastructure.Core.States;
using Game.Scripts.Infrastructure.Core.UI;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Game.Tests
{
    public sealed class StateMachineTests
    {
        private GameObject _uiObject;
        private DiContainer _container;

        [SetUp]
        public void SetUp()
        {
            _uiObject = new GameObject("Test UIManager");
            _container = new DiContainer();
            _container.BindInstance(_uiObject.AddComponent<UIManager>());
            _container.Bind<StateFactory>().AsSingle();
            _container.Bind<StateMachine>().AsSingle();
            _container.Bind<ExitingState>().AsSingle();
            _container.Bind<FinalState>().AsSingle();
            _container.Bind<NestedState>().AsSingle();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_uiObject);
        }

        [Test]
        public async Task TransitionWaitsForAsyncExitBeforeSyncExitAndNextEnter()
        {
            var machine = _container.Resolve<StateMachine>();
            await machine.EnterAsync<ExitingState>();
            var first = _container.Resolve<ExitingState>();
            var next = _container.Resolve<FinalState>();
            var transition = machine.EnterAsync<FinalState>();
            Assert.That(first.Events, Is.EqualTo(new[] { "enter", "exit-start" }));
            Assert.That(next.EnterCount, Is.Zero);
            first.ExitCompleted.TrySetResult();
            await transition;
            Assert.That(first.Events, Is.EqualTo(new[] { "enter", "exit-start", "exit-end", "exit" }));
            Assert.That(next.EnterCount, Is.EqualTo(1));
        }

        [Test]
        public async Task StateCanAwaitNextStateAndSceneResetExitsItOnce()
        {
            var machine = _container.Resolve<StateMachine>();
            await machine.EnterAsync<NestedState>();
            var final = _container.Resolve<FinalState>();
            Assert.That(final.EnterCount, Is.EqualTo(1));
            machine.Reset();
            machine.Reset();
            Assert.That(final.ExitCount, Is.EqualTo(1));
        }

        public sealed class ExitingState : IEnterStateAsync, IExitStateAsync, IExitState
        {
            public readonly List<string> Events = new();
            public readonly UniTaskCompletionSource ExitCompleted = new();

            public UniTask Enter()
            {
                Events.Add("enter");
                return UniTask.CompletedTask;
            }

            public async UniTask ExitAsync()
            {
                Events.Add("exit-start");
                await ExitCompleted.Task;
                Events.Add("exit-end");
            }

            public void Exit()
            {
                Events.Add("exit");
            }
        }

        public sealed class FinalState : IEnterStateAsync, IExitState
        {
            public int EnterCount;
            public int ExitCount;

            public UniTask Enter()
            {
                EnterCount++;
                return UniTask.CompletedTask;
            }

            public void Exit()
            {
                ExitCount++;
            }
        }

        public sealed class NestedState : IEnterStateAsync
        {
            private readonly StateMachine _states;

            public NestedState(StateMachine states)
            {
                _states = states;
            }

            public async UniTask Enter()
            {
                await _states.EnterAsync<FinalState>();
            }
        }
    }
}
