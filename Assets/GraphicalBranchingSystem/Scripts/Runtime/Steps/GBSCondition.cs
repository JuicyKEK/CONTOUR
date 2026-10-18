using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;

namespace GBS.Steps
{
    /// <summary>
    /// Контекст ожидания условия.
    /// </summary>
    public readonly struct GBSConditionContext
    {
        public readonly StoryContext Story;

        /// <summary>
        /// Номер изменения состояния сюжета (<see cref="IStoryState.Sequence"/>) в момент, когда началось
        /// ожидание: для переходов Story-ноды - вход в ноду (до её действий), для ноды Condition - первый
        /// запрос значения. По нему условие отличает "новые" сигналы от случившихся раньше.
        /// </summary>
        public readonly long StartSequence;

        public GBSConditionContext(StoryContext story, long startSequence)
        {
            Story = story;
            StartSequence = startSequence;
        }

        public StoryState State => Story.State;
    }

    /// <summary>
    /// Условие перехода / ноды Condition. Хранится прямо внутри графа ([SerializeReference]),
    /// как и <see cref="GBSAction"/>. WaitAsync завершается, когда условие выполнено.
    /// Если условие уже выполнено - задача должна завершиться синхронно: так при одновременной
    /// готовности нескольких переходов побеждает верхний.
    /// </summary>
    [Serializable]
    public abstract class GBSCondition
    {
        public abstract UniTask WaitAsync(GBSConditionContext context, CancellationToken token);

        /// <summary>
        /// Ждёт, пока предикат по состоянию сюжета станет истинным: проверка сразу, затем при каждом
        /// изменении состояния. Уже истинный предикат - синхронное завершение.
        /// </summary>
        protected static UniTask WaitStateAsync(IStoryState state, Func<bool> predicate, CancellationToken token)
        {
            if (predicate())
            {
                return UniTask.CompletedTask;
            }

            return WaitStateChangesAsync(state, predicate, token);
        }

        private static async UniTask WaitStateChangesAsync(IStoryState state, Func<bool> predicate, CancellationToken token)
        {
            var completionSource = new UniTaskCompletionSource();

            void Handler(string key)
            {
                if (predicate())
                {
                    completionSource.TrySetResult();
                }
            }

            state.Changed += Handler;

            try
            {
                using (token.Register(() => completionSource.TrySetCanceled(token)))
                {
                    await completionSource.Task;
                }
            }
            finally
            {
                state.Changed -= Handler;
            }
        }
    }
}
