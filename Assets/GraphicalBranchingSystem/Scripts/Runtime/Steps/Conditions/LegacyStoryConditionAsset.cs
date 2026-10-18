using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Старый SO-ассет StoryCondition внутри графа - для условий без встроенного аналога
    /// (свои наследники StoryCondition). Встроенные импортёр переносит в граф сам, без ассета.
    /// </summary>
    [Serializable, GBSMenu("Legacy/Story Condition Asset")]
    public class LegacyStoryConditionAsset : GBSCondition
    {
        [SerializeField] private StoryCondition m_Asset;

        public LegacyStoryConditionAsset()
        {
        }

        public LegacyStoryConditionAsset(StoryCondition asset)
        {
            m_Asset = asset;
        }

        public override UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            return m_Asset != null ? m_Asset.WaitAsync(context.Story, token) : UniTask.CompletedTask;
        }
    }
}
