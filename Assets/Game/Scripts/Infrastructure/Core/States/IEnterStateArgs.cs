namespace Game.Scripts.Infrastructure.Core.States
{
    public interface IEnterStateArgs<in T> : IState
    {
        void Enter(T args);
    }
}