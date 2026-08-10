using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Scripts.Story
{
    public interface IScreenFader
    {
        UniTask FadeOutAsync(float duration, CancellationToken token);
        UniTask FadeInAsync(float duration, CancellationToken token);
    }
}

