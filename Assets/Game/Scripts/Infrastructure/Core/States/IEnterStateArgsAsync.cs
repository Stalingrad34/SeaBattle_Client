using Cysharp.Threading.Tasks;

namespace Game.Scripts.Infrastructure.Core.States
{
  public interface IEnterStateArgsAsync<in T> : IState
  {
    UniTask Enter(T args);
  }
}