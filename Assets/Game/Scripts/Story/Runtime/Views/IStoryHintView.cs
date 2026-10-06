using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Scripts.Story
{
    public interface IStoryHintView
    {
        /// <summary>
        /// Показывает подсказку на <paramref name="duration"/> секунд или до отмены <paramref name="token"/>.
        /// <see cref="float.PositiveInfinity"/> - показывать, пока не отменят token.
        /// </summary>
        UniTask ShowAsync(string text, float duration, CancellationToken token);
        void Hide();
    }
}

