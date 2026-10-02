using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Data;
using GBS.Save;
using GBS.Steps;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Runtime
{
    /// <summary>
    /// Проигрывает один граф GBS:
    ///   1) ждёт условие старта (если к Start-ноде подключено булево выражение);
    ///   2) входит в сюжетную ноду и последовательно выполняет её действия;
    ///   3) параллельно ждёт условия всех переходов (race) и уходит в тот, что сработал первым
    ///      (если готовы сразу несколько - побеждает верхний в списке);
    ///   4) дойдя до End-ноды, выполняет её действия и помечает граф завершённым.
    /// </summary>
    public class GBSGraphRunner
    {
        private readonly GBSGraphSO m_Graph;
        private readonly StoryContext m_Context;
        private readonly GBSBoolEvaluator m_Evaluator;

        // Номер изменения состояния сюжета на входе в текущую ноду (для условий "сигнал после входа").
        private long m_NodeEnterSequence;

        public GBSGraphRunner(GBSGraphSO graph, StoryContext context, IGBSGraphProgressProvider progress)
        {
            m_Graph = graph;
            m_Context = context;
            m_Evaluator = new GBSBoolEvaluator(graph, context, progress);
        }

        public event Action<GBSGraphRunner> NodeEntered;
        public event Action<GBSGraphRunner> Completed;

        public GBSGraphSO Graph => m_Graph;
        public bool IsCompleted { get; private set; }
        public bool IsRunning { get; private set; }
        public string CurrentNodeId { get; private set; }

        public GBSGraphProgressData CaptureProgress()
        {
            var progress = new GBSGraphProgressData
            {
                GraphId = m_Graph.GraphId,
                GraphName = m_Graph.GraphName,
                CurrentNodeId = CurrentNodeId,
                IsCompleted = IsCompleted,
                LatchedNodes = new List<string>(m_Evaluator.LatchedNodes)
            };

            return progress;
        }

        public void RestoreProgress(GBSGraphProgressData progress)
        {
            if (progress == null)
            {
                return;
            }

            IsCompleted = progress.IsCompleted;
            CurrentNodeId = progress.CurrentNodeId;
            m_Evaluator.RestoreLatched(progress.LatchedNodes);
        }

        public void ResetProgress()
        {
            IsCompleted = false;
            CurrentNodeId = null;
            m_Evaluator.Reset();
        }

        public async UniTask RunAsync(CancellationToken token)
        {
            if (IsRunning)
            {
                return;
            }

            if (IsCompleted)
            {
                return;
            }

            IsRunning = true;
            m_Evaluator.SetLifetimeToken(token);

            try
            {
                var node = await ResolveEntryNodeAsync(token);

                while (node != null)
                {
                    token.ThrowIfCancellationRequested();

                    if (node is GBSEndNodeData endNode)
                    {
                        await CompleteGraphAsync(endNode, token);
                        return;
                    }

                    if (node is GBSGraphCompletedNodeData waitNode)
                    {
                        node = await WaitForGraphAsync(waitNode, token);
                        continue;
                    }

                    if (node is not GBSStoryNodeData storyNode)
                    {
                        Debug.LogWarning($"[GBS] Граф '{m_Graph.GraphName}': нода '{node.NodeName}' не может быть частью потока сюжета.");
                        return;
                    }

                    CurrentNodeId = storyNode.Id;
                    NodeEntered?.Invoke(this);

                    using (var nodeScope = CancellationTokenSource.CreateLinkedTokenSource(token))
                    {
                        try
                        {
                            await RunEnterAsync(storyNode, token, nodeScope.Token);

                            node = await WaitForNextNodeAsync(storyNode, token);
                        }
                        finally
                        {
                            // Уход с ноды: всё, что жило "пока мы на ноде" (подсказки и т.п.), завершается.
                            nodeScope.Cancel();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Нормальная остановка графа.
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async UniTask<GBSNodeData> ResolveEntryNodeAsync(CancellationToken token)
        {
            // Продолжение с сейва.
            var resumeNode = m_Graph.GetNode(CurrentNodeId);

            if (resumeNode is GBSStoryNodeData || resumeNode is GBSEndNodeData || resumeNode is GBSGraphCompletedNodeData)
            {
                return resumeNode;
            }

            var startNode = m_Graph.GetStartNode();

            if (startNode == null)
            {
                Debug.LogWarning($"[GBS] В графе '{m_Graph.GraphName}' нет Start-ноды.");
                return null;
            }

            var gateEdge = m_Graph.GetEdgeTo(startNode.Id, GBSPortId.Gate);

            if (gateEdge != null)
            {
                await m_Evaluator.WaitAsync(gateEdge.FromNodeId, token);
            }

            var flowEdge = m_Graph.GetEdgeFrom(startNode.Id, GBSPortId.Out);

            if (flowEdge == null)
            {
                Debug.LogWarning($"[GBS] Start-нода графа '{m_Graph.GraphName}' ни с чем не соединена.");
                return null;
            }

            return m_Graph.GetNode(flowEdge.ToNodeId);
        }

        /// <summary>
        /// Ожидание завершения другого графа прямо внутри потока сюжета.
        /// Если граф в ноде не указан - проходим её насквозь без ожидания.
        /// </summary>
        private async UniTask<GBSNodeData> WaitForGraphAsync(GBSGraphCompletedNodeData node, CancellationToken token)
        {
            CurrentNodeId = node.Id;
            NodeEntered?.Invoke(this);

            await m_Evaluator.WaitAsync(node.Id, token);

            var edge = m_Graph.GetEdgeFrom(node.Id, GBSPortId.FlowOut);

            if (edge == null)
            {
                Debug.LogWarning($"[GBS] Нода '{node.NodeName}' ждала граф, но flow-выход ни с чем не соединён.");
                return null;
            }

            return m_Graph.GetNode(edge.ToNodeId);
        }

        private UniTask RunEnterAsync(GBSStoryNodeData node, CancellationToken token, CancellationToken nodeExitToken)
        {
            // Запоминаем момент входа ДО действий: сигнал, пришедший пока идут действия ноды
            // (подсказка, катсцена), тоже считается "после входа" и не теряется.
            m_NodeEnterSequence = m_Context.State.Sequence;

            return RunActionsAsync(node.GetActions(), node, token, nodeExitToken);
        }

        private async UniTask RunActionsAsync(IReadOnlyList<GBSAction> actions, GBSNodeData node,
            CancellationToken token, CancellationToken nodeExitToken)
        {
            var actionContext = new GBSActionContext(m_Context, nodeExitToken);

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];

                if (action == null)
                {
                    continue;
                }

                try
                {
                    await action.ExecuteAsync(actionContext, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    // Ошибка одного действия не должна останавливать весь сюжет.
                    Debug.LogError($"[GBS] Граф '{m_Graph.GraphName}', нода '{node.NodeName}': " +
                                   $"ошибка в действии {action.GetType().Name}.");
                    Debug.LogException(exception);
                }
            }
        }

        private async UniTask<GBSNodeData> WaitForNextNodeAsync(GBSStoryNodeData node, CancellationToken token)
        {
            var branches = node.Branches;

            if (branches.Count == 0)
            {
                Debug.LogWarning($"[GBS] Нода '{node.NodeName}' не имеет условий перехода - сюжет остановлен.");
                return null;
            }

            var conditionContext = new GBSConditionContext(m_Context, m_NodeEnterSequence);

            if (branches.Count == 1)
            {
                await WaitBranchAsync(branches[0], conditionContext, token);
                return GetBranchTarget(node, branches[0]);
            }

            // Ждём все переходы параллельно: побеждает выполнившийся первым. Уже выполненные условия
            // (и переходы без условия) завершаются синхронно, а WhenAny среди готовых выбирает
            // наименьший индекс - поэтому при одновременной готовности побеждает верхний переход
            // (ветвление по флагам: "Flag Is ..." выше, переход без условия - "иначе" - ниже).
            using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            var raceTasks = new UniTask<bool>[branches.Count];

            for (int i = 0; i < branches.Count; i++)
            {
                raceTasks[i] = WaitBranchSafeAsync(branches[i], conditionContext, raceCts.Token);
            }

            var (winnerIndex, _) = await UniTask.WhenAny(raceTasks);

            raceCts.Cancel();

            return GetBranchTarget(node, branches[winnerIndex]);
        }

        private async UniTask<bool> WaitBranchSafeAsync(GBSBranchData branch, GBSConditionContext context, CancellationToken token)
        {
            try
            {
                await WaitBranchAsync(branch, context, token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private static UniTask WaitBranchAsync(GBSBranchData branch, GBSConditionContext context, CancellationToken token)
        {
            var condition = branch.GetCondition();
            return condition != null ? condition.WaitAsync(context, token) : UniTask.CompletedTask;
        }

        private GBSNodeData GetBranchTarget(GBSStoryNodeData node, GBSBranchData branch)
        {
            var edge = m_Graph.GetEdgeFrom(node.Id, branch.Id);

            if (edge == null)
            {
                Debug.LogWarning($"[GBS] Условие перехода '{branch.BranchName}' ноды '{node.NodeName}' не соединено ребром со следующей нодой.");
                return null;
            }

            return m_Graph.GetNode(edge.ToNodeId);
        }

        private async UniTask CompleteGraphAsync(GBSEndNodeData endNode, CancellationToken token)
        {
            CurrentNodeId = endNode.Id;

            // У End-ноды нет переходов: "уход с ноды" - сразу после её действий.
            using (var nodeScope = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                try
                {
                    await RunActionsAsync(endNode.GetActions(), endNode, token, nodeScope.Token);
                }
                finally
                {
                    nodeScope.Cancel();
                }
            }

            IsCompleted = true;
            NodeEntered?.Invoke(this);
            Completed?.Invoke(this);
        }
    }
}








