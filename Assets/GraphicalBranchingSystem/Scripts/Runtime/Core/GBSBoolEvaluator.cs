using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Data;
using GBS.Steps;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Runtime
{
    /// <summary>
    /// Вычислитель булевой части графа.
    ///
    /// Как это работает:
    ///  - нода Condition оборачивает условие (GBSCondition). Ожидание запускается лениво,
    ///    в момент, когда выражение впервые понадобилось. Сработав, условие
    ///    "защёлкивается" (latched) и дальше считается истинным - это позволяет
    ///    строить NOT/NAND и сохранять состояние в сейв;
    ///  - нода Logic комбинирует значения входов (And/Or/Not/Nand/Nor/Xor/Xnor);
    ///  - нода GraphCompleted возвращает "другой граф пройден до конца".
    /// </summary>
    public class GBSBoolEvaluator
    {
        private const int MaxDepth = 64;

        private readonly GBSGraphSO m_Graph;
        private readonly StoryContext m_Context;
        private readonly IGBSGraphProgressProvider m_Progress;

        private readonly HashSet<string> m_Latched = new HashSet<string>();
        private readonly HashSet<string> m_Running = new HashSet<string>();

        private CancellationToken m_LifetimeToken;

        public GBSBoolEvaluator(GBSGraphSO graph, StoryContext context, IGBSGraphProgressProvider progress)
        {
            m_Graph = graph;
            m_Context = context;
            m_Progress = progress;
        }

        public IReadOnlyCollection<string> LatchedNodes => m_Latched;

        public void SetLifetimeToken(CancellationToken token)
        {
            m_LifetimeToken = token;
        }

        public void RestoreLatched(IEnumerable<string> nodeIds)
        {
            m_Latched.Clear();

            if (nodeIds == null)
            {
                return;
            }

            foreach (var nodeId in nodeIds)
            {
                if (!string.IsNullOrEmpty(nodeId))
                {
                    m_Latched.Add(nodeId);
                }
            }
        }

        public void Reset()
        {
            m_Latched.Clear();
            m_Running.Clear();
        }

        /// <summary>Ждёт, пока выражение с корнем в rootNodeId станет истинным.</summary>
        public async UniTask WaitAsync(string rootNodeId, CancellationToken token)
        {
            StartSources(rootNodeId, 0);

            if (Evaluate(rootNodeId, 0))
            {
                return;
            }

            await UniTask.WaitUntil(() => Evaluate(rootNodeId, 0), PlayerLoopTiming.Update, token);
        }

        /// <summary>Мгновенная проверка выражения без запуска ожиданий.</summary>
        public bool EvaluateNow(string rootNodeId)
        {
            return Evaluate(rootNodeId, 0);
        }

        public bool Evaluate(string nodeId, int depth)
        {
            if (depth > MaxDepth)
            {
                Debug.LogError($"[GBS] Обнаружен цикл в булевой части графа '{m_Graph.GraphName}'.");
                return false;
            }

            var node = m_Graph.GetNode(nodeId);

            switch (node)
            {
                case null:
                    return false;

                case GBSConditionNodeData conditionNode:
                    return m_Latched.Contains(conditionNode.Id);

                case GBSGraphCompletedNodeData graphNode:
                    // Граф не задан - считаем условие выполненным (старт без ожидания).
                    return graphNode.TargetGraph == null
                           || (m_Progress != null && m_Progress.IsGraphCompleted(graphNode.TargetGraph));

                case GBSLogicNodeData logicNode:
                    return EvaluateLogic(logicNode, depth);

                default:
                    return false;
            }
        }

        private bool EvaluateLogic(GBSLogicNodeData logicNode, int depth)
        {
            var connectedCount = 0;
            var trueCount = 0;
            var firstValue = false;

            for (int i = 0; i < logicNode.InputCount; i++)
            {
                var edge = m_Graph.GetEdgeTo(logicNode.Id, GBSPortId.LogicInput(i));

                if (edge == null)
                {
                    continue;
                }

                var value = Evaluate(edge.FromNodeId, depth + 1);

                if (connectedCount == 0)
                {
                    firstValue = value;
                }

                connectedCount++;

                if (value)
                {
                    trueCount++;
                }
            }

            if (connectedCount == 0)
            {
                return false;
            }

            var allTrue = trueCount == connectedCount;
            var anyTrue = trueCount > 0;
            var oddTrue = (trueCount & 1) == 1;

            switch (logicNode.Operation)
            {
                case GBSLogicOperation.And: return allTrue;
                case GBSLogicOperation.Or: return anyTrue;
                case GBSLogicOperation.Not: return !firstValue;
                case GBSLogicOperation.Nand: return !allTrue;
                case GBSLogicOperation.Nor: return !anyTrue;
                case GBSLogicOperation.Xor: return oddTrue;
                case GBSLogicOperation.Xnor: return !oddTrue;
                default: return false;
            }
        }

        /// <summary>Запускает ожидание всех условий, от которых зависит выражение.</summary>
        private void StartSources(string nodeId, int depth)
        {
            if (depth > MaxDepth)
            {
                return;
            }

            var node = m_Graph.GetNode(nodeId);

            switch (node)
            {
                case GBSConditionNodeData conditionNode:
                    TryRunCondition(conditionNode);
                    return;

                case GBSLogicNodeData logicNode:
                    for (int i = 0; i < logicNode.InputCount; i++)
                    {
                        var edge = m_Graph.GetEdgeTo(logicNode.Id, GBSPortId.LogicInput(i));

                        if (edge != null)
                        {
                            StartSources(edge.FromNodeId, depth + 1);
                        }
                    }

                    return;
            }
        }

        private void TryRunCondition(GBSConditionNodeData conditionNode)
        {
            var condition = conditionNode.GetCondition();

            if (condition == null)
            {
                return;
            }

            if (m_Latched.Contains(conditionNode.Id) || m_Running.Contains(conditionNode.Id))
            {
                return;
            }

            m_Running.Add(conditionNode.Id);
            RunConditionAsync(conditionNode, condition).Forget();
        }

        private async UniTaskVoid RunConditionAsync(GBSConditionNodeData conditionNode, GBSCondition condition)
        {
            try
            {
                // Ожидание стартует лениво - "после начала" для ноды Condition значит после первого
                // запроса её значения.
                var context = new GBSConditionContext(m_Context, m_Context.State.Sequence);
                await condition.WaitAsync(context, m_LifetimeToken);
                m_Latched.Add(conditionNode.Id);
            }
            catch (System.OperationCanceledException)
            {
                // Граф остановлен - ничего не защёлкиваем.
            }
            finally
            {
                m_Running.Remove(conditionNode.Id);
            }
        }
    }
}



