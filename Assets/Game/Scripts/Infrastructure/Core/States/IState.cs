using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Scripts.Infrastructure.Core.States
{
    public interface IState
    {
        UniTask EnterAsync(CancellationToken token);
        void Exit();
    }
}
