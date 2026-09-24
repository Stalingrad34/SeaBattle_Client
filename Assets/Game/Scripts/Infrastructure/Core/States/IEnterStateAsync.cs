using Cysharp.Threading.Tasks;

namespace Game.Scripts.Infrastructure.Core.States
{
  public interface IEnterStateAsync : IState
  {
    UniTask Enter();
  }
}