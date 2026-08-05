using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Простое действие для дизайнера - вызвать UnityEvent (открыть дверь,
    /// проиграть звук, включить визуальный эффект и т.п.), как и предполагалось
    /// в исходной идее с UnityEvent на ноде.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Actions/Unity Event", fileName = "UnityEventAction")]
    public class UnityEventAction : StoryAction
    {
        private event Action m_OnRaised;

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            m_OnRaised?.Invoke();
            return UniTask.CompletedTask;
        }
        
        public void Subscribe(Action handler)
        {
            m_OnRaised += handler;
        }

        public void Unsubscribe(Action handler)
        {
            m_OnRaised -= handler;
        }
        
        public void UnsubscribeAll()
        {
            m_OnRaised = null;
        }
    }
}

