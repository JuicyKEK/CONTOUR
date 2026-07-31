using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.InputController;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Главный контроллер сюжета. Хранит стартовую ноду, собирает зависимости
    /// через JuicyDI и последовательно проигрывает граф StoryNodeSO:
    ///   1) выполняет OnEnterActions текущей ноды по очереди (await);
    ///   2) параллельно ждёт условия всех веток (Branches) и выбирает ту,
    ///      что выполнилась первой, отменяя остальные;
    ///   3) переходит в следующую ноду и повторяет цикл.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(200)]
    public class StoryManager : MonoBehaviour, ISequence
    {
        [SerializeField] private StoryNodeSO m_StartNode;

        [Inject] private IInputActions m_InputActions;
        [Inject] private IInputSelectionActions m_InputSelectionActions;
        [Inject] private List<IPlayerControlHandle> m_PlayerControlHandles;
        [Inject] private IStoryHintView m_HintView;
        [Inject] private ICutsceneDirector m_CutsceneDirector;
        [Inject] private ICameraDirector m_CameraDirector;
        [Inject] private IScreenFader m_ScreenFader;

        public event Action<StoryNodeSO> NodeStarted;

        public StoryNodeSO CurrentNode { get; private set; }

        private StoryContext m_Context;
        private CancellationTokenSource m_LifetimeCts;

        public void MethodInit()
        {
            m_Context = new StoryContext(
                m_InputActions,
                m_InputSelectionActions,
                m_PlayerControlHandles,
                m_HintView,
                m_CutsceneDirector,
                m_CameraDirector,
                m_ScreenFader);
        }

        public void MethodStart()
        {
            if (m_StartNode == null)
            {
                Debug.LogWarning($"{nameof(StoryManager)}: start node is not assigned.");
                return;
            }

            m_LifetimeCts = new CancellationTokenSource();
            RunAsync(m_LifetimeCts.Token).Forget();
        }

        private void OnDestroy()
        {
            m_LifetimeCts?.Cancel();
            m_LifetimeCts?.Dispose();
        }

        private async UniTask RunAsync(CancellationToken token)
        {
            var node = m_StartNode;

            while (node != null)
            {
                token.ThrowIfCancellationRequested();

                CurrentNode = node;
                NodeStarted?.Invoke(node);

                await RunEnterActionsAsync(node, token);

                node = await WaitForNextNodeAsync(node, token);
            }
        }

        private async UniTask RunEnterActionsAsync(StoryNodeSO node, CancellationToken token)
        {
            var actions = node.OnEnterActions;

            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i] == null)
                {
                    continue;
                }

                await actions[i].ExecuteAsync(m_Context, token);
            }
        }

        private async UniTask<StoryNodeSO> WaitForNextNodeAsync(StoryNodeSO node, CancellationToken token)
        {
            var branches = node.Branches;

            if (branches.Count == 0)
            {
                return null;
            }

            if (branches.Count == 1)
            {
                await branches[0].Condition.WaitAsync(m_Context, token);
                return branches[0].NextNode;
            }

            using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var raceTasks = new UniTask<bool>[branches.Count];

            for (int i = 0; i < branches.Count; i++)
            {
                raceTasks[i] = WaitBranchSafeAsync(branches[i], raceCts.Token);
            }

            var (winnerIndex, _) = await UniTask.WhenAny(raceTasks);
            raceCts.Cancel();

            return branches[winnerIndex].NextNode;
        }

        private async UniTask<bool> WaitBranchSafeAsync(StoryBranch branch, CancellationToken token)
        {
            try
            {
                await branch.Condition.WaitAsync(m_Context, token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }
    }
}


