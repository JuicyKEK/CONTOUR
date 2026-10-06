using System;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// SO-канал "живого" bool-состояния - расширение паттерна StoryEventChannelSO
    /// (Game Event) для случая, когда важен не разовый сигнал, а ТЕКУЩЕЕ
    /// значение чего-либо (открыта ли дверь, поднят ли рубильник, заполнена ли
    /// шкала и т.п.).
    ///
    /// Ключевое отличие от StoryEventChannelSO: подписка через Subscribe()
    /// сразу синхронно получает текущее значение (аналог R3 ReactiveProperty),
    /// а затем - каждое следующее изменение через SetValue(). Это даёт
    /// BoolChannelCondition поведение "мгновенная проверка при входе в ноду +
    /// дальнейшее ожидание изменения" без какого-либо специального кода -
    /// оба случая обрабатываются одной и той же подпиской.
    ///
    /// Кто заполняет канал реальным значением - см. StoryBoolChannelListener
    /// (универсальный сценный мост, вызывается из UnityEvent любого источника)
    /// и, для примера, DoorOpenStateToBoolChannelBridge (готовый мост для
    /// дверей проекта). Сам канал ничего не знает о конкретном источнике -
    /// подходит для любых bool-состояний, не только дверей.
    ///
    /// Устаревший способ: для новых состояний используйте флаги сюжета по ключам
    /// (DoorStoryFlagBridge / StorySignalEmitter). SetValue() дополнительно пишет флаг
    /// "Legacy/&lt;имя ассета&gt;", поэтому старые каналы работают и с графами GBS.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Events/Bool State Channel", fileName = "BoolStateChannel")]
    public class StoryBoolChannelSO : ScriptableObject
    {
        [Tooltip("Значение, актуальное до первого вызова SetValue во время игры.")]
        [SerializeField] private bool m_DefaultValue;

        private bool m_HasRuntimeValue;
        private bool m_CurrentValue;
        private event Action<bool> m_Changed;

        public bool CurrentValue => m_HasRuntimeValue ? m_CurrentValue : m_DefaultValue;

        public void SetValue(bool value)
        {
            m_HasRuntimeValue = true;
            m_CurrentValue = value;
            m_Changed?.Invoke(value);

            // Мост на новую систему: графы GBS видят канал как флаг "Legacy/<имя ассета>".
            StorySignals.SetFlag(StorySignals.LegacyKey(this), value);
        }

        /// <summary>
        /// Подписка сразу же синхронно получает текущее значение канала
        /// (в момент вызова Subscribe), а затем - каждое следующее изменение
        /// через SetValue. Верните полученный IDisposable в using/Dispose(),
        /// чтобы отписаться.
        /// </summary>
        public IDisposable Subscribe(Action<bool> handler)
        {
            handler.Invoke(CurrentValue);
            m_Changed += handler;
            return new Unsubscriber(this, handler);
        }

        private void Unsubscribe(Action<bool> handler)
        {
            m_Changed -= handler;
        }

        // SO-ассет переживает выход из play mode, поэтому сбрасываем рантайм-
        // состояние при OnDisable (вызывается Unity при остановке игры), чтобы
        // при повторном запуске канал не "помнил" значение из прошлого сеанса.
        private void OnDisable()
        {
            m_HasRuntimeValue = false;
            m_Changed = null;
        }

        private class Unsubscriber : IDisposable
        {
            private readonly StoryBoolChannelSO m_Owner;
            private readonly Action<bool> m_Handler;
            private bool m_Disposed;

            public Unsubscriber(StoryBoolChannelSO owner, Action<bool> handler)
            {
                m_Owner = owner;
                m_Handler = handler;
            }

            public void Dispose()
            {
                if (m_Disposed)
                {
                    return;
                }

                m_Disposed = true;
                m_Owner.Unsubscribe(m_Handler);
            }
        }
    }
}

