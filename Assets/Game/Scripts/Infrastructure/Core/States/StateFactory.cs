using Zenject;

namespace Game.Scripts.Infrastructure.Core.States
{
    public sealed class StateFactory
    {
        private readonly DiContainer _container;
        public StateFactory(DiContainer container)
        {
            _container = container;
        }

        public T Create<T>()
            where T : class, IState
        {
            return _container.Resolve<T>();
        }
    }
}
