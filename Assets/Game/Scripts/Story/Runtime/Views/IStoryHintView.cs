using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Scripts.Story
{
    public interface IStoryHintView
    {
        UniTask ShowAsync(string text, float duration, CancellationToken token);
        void Hide();
    }
}

