using System;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// SO-канал событий ("Game Event" паттерн). Позволяет любому геймплейному
    /// скрипту (NPC, триггеру, квесту) сообщить story-механике о наступлении
    /// события, не имея прямой ссылки на StoryManager.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Events/Story Event Channel", fileName = "StoryEventChannel")]
    public class StoryEventChannelSO : ScriptableObject
    {
        private event Action m_OnRaised;

        public void Raise()
        {
            m_OnRaised?.Invoke();
            //UnsubscribeAll();
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

