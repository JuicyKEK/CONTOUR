using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Data;
using GBS.Save;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Runtime
{
    /// <summary>
    /// Проигрывает один граф GBS:
    ///   1) ждёт условие старта (если к Start-ноде подключено булево выражение);
    ///   2) входит в сюжетную ноду: поднимает GBSEvent'ы и последовательно
    ///      выполняет StoryAction'ы;
    ///   3) параллельно ждёт условия всех веток (race) и уходит в ту,
    ///      что сработала первой;
    ///   4) дойдя до End-ноды, помечает граф завершённым.
    /// </summary>
    public class GBSGraphRunner
    {
        private readonly GBSGraphSO m_Graph;
        private readonly StoryContext m_Context;
        private readonly GBSBoolEvaluator m_Evaluator;

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
                        CompleteGraph(endNode);
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

                    await RunEnterAsync(storyNode, token);

                    node = await WaitForNextNodeAsync(storyNode, token);
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

        private async UniTask RunEnterAsync(GBSStoryNodeData node, CancellationToken token)        {
            var events = node.OnEnterEvents;

            for (int i = 0; i < events.Count; i++)
            {
                events[i]?.Raise();
            }

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

        private async UniTask<GBSNodeData> WaitForNextNodeAsync(GBSStoryNodeData node, CancellationToken token)
        {
            var branches = node.Branches;

            if (branches.Count == 0)
            {
                Debug.LogWarning($"[GBS] Нода '{node.NodeName}' не имеет условий перехода - сюжет остановлен.");
                return null;
            }

            // 1) Переход без условия срабатывает мгновенно - уходим сразу.
            for (int i = 0; i < branches.Count; i++)
            {
                if (branches[i].Condition == null)
                {
                    return GetBranchTarget(node, branches[i]);
                }
            }

            // 2) Иначе ждём: побеждает то условие, которое выполнится первым.
            if (branches.Count == 1)
            {
                await WaitBranchAsync(node, branches[0], token);
                return GetBranchTarget(node, branches[0]);
            }

            using var raceCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            var raceTasks = new UniTask<bool>[branches.Count];

            for (int i = 0; i < branches.Count; i++)
            {
                raceTasks[i] = WaitBranchSafeAsync(node, branches[i], raceCts.Token);
            }

            var (winnerIndex, _) = await UniTask.WhenAny(raceTasks);

            raceCts.Cancel();

            return GetBranchTarget(node, branches[winnerIndex]);
        }

        private async UniTask<bool> WaitBranchSafeAsync(GBSStoryNodeData node, GBSBranchData branch, CancellationToken token)
        {
            try
            {
                await WaitBranchAsync(node, branch, token);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        private UniTask WaitBranchAsync(GBSStoryNodeData node, GBSBranchData branch, CancellationToken token)
        {
            if (branch.Condition != null)
            {
                return branch.Condition.WaitAsync(m_Context, token);
            }

            return UniTask.CompletedTask;
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

        private void CompleteGraph(GBSEndNodeData endNode)
        {
            CurrentNodeId = endNode.Id;
            IsCompleted = true;

            var events = endNode.OnCompleteEvents;

            for (int i = 0; i < events.Count; i++)
            {
                events[i]?.Raise();
            }

            NodeEntered?.Invoke(this);
            Completed?.Invoke(this);
        }
    }
}








