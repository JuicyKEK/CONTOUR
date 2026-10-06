using System;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// SO-канал событий ("Game Event" паттерн). Позволяет любому геймплейному
    /// скрипту (NPC, триггеру, квесту) сообщить story-механике о наступлении
    /// события, не имея прямой ссылки на StoryManager.
    ///
    /// Устаревший способ: для новых сигналов используйте ключи состояния сюжета
    /// (StorySignalEmitter / StorySignals.Raise). Raise() дополнительно поднимает сигнал
    /// "Legacy/&lt;имя ассета&gt;", поэтому старые каналы работают и с графами GBS.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Events/Story Event Channel", fileName = "StoryEventChannel")]
    public class StoryEventChannelSO : ScriptableObject
    {
        private event Action m_OnRaised;

        public void Raise()
        {
            m_OnRaised?.Invoke();

            // Мост на новую систему: графы GBS ждут этот канал как сигнал "Legacy/<имя ассета>".
            StorySignals.Raise(StorySignals.LegacyKey(this));
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

