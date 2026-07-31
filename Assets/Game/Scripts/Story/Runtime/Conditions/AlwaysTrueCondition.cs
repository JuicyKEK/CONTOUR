using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Всегда выполняется мгновенно. Удобно для стартовой ноды сюжета
    /// или веток без реального условия ожидания.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Conditions/Always True", fileName = "AlwaysTrueCondition")]
    public class AlwaysTrueCondition : StoryCondition
    {
        public override UniTask WaitAsync(StoryContext context, CancellationToken token)
        {
            return UniTask.CompletedTask;
        }
    }
}

