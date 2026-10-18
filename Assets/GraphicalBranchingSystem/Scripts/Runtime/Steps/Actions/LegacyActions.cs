using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Events;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Старый SO-ассет StoryAction внутри графа - для действий, у которых нет встроенного аналога
    /// (свои наследники StoryAction). Встроенные (подсказка, камера, флаги...) импортёр переносит в
    /// граф сам, без ассета.
    /// </summary>
    [Serializable, GBSMenu("Legacy/Story Action Asset")]
    public class LegacyStoryActionAsset : GBSAction
    {
        [SerializeField] private StoryAction m_Asset;

        public LegacyStoryActionAsset()
        {
        }

        public LegacyStoryActionAsset(StoryAction asset)
        {
            m_Asset = asset;
        }

        public StoryAction Asset => m_Asset;

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            return m_Asset != null ? m_Asset.ExecuteAsync(context, token) : UniTask.CompletedTask;
        }
    }

    /// <summary>
    /// Старый SO-сигнал GBSEvent (слушатель на сцене - UnityGameEventListener).
    /// Для новых шагов используйте "Invoke Scene Reaction".
    /// </summary>
    [Serializable, GBSMenu("Legacy/GBS Event Asset")]
    public class LegacyGBSEventAsset : GBSAction
    {
        [SerializeField] private GBSEvent m_Event;

        public LegacyGBSEventAsset()
        {
        }

        public LegacyGBSEventAsset(GBSEvent gbsEvent)
        {
            m_Event = gbsEvent;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            if (m_Event != null)
            {
                m_Event.Raise();
            }

            return UniTask.CompletedTask;
        }
    }
}
