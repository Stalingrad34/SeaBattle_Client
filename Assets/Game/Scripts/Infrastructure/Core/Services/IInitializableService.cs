using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Scripts.Infrastructure.Core.Services
{
    public interface IInitializableService : IService
    {
        UniTask InitAsync(CancellationToken token);
    }
}
