using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Сценный "мост" к StoryBoolChannelSO (аналог StoryEventChannelListener,
    /// но для живого bool-состояния, а не одноразового события). Именно этот
    /// компонент решает проблему "нельзя перетащить объект сцены в поле
    /// ScriptableObject-условия": ссылку на реальный сценный источник состояния
    /// (дверь, рубильник, любой другой скрипт) держит этот MonoBehaviour - на
    /// сцене такие ссылки разрешены, в отличие от полей SO-ассетов. Сам канал
    /// и BoolChannelCondition продолжают работать только с SO-каналом и ничего
    /// не знают о конкретном сценном объекте.
    ///
    /// Использование:
    ///  1) Создать ассет StoryBoolChannelSO (Create -> Story/Events/Bool State Channel).
    ///  2) Повесить этот компонент на сцену рядом с источником состояния,
    ///     указать тот же канал.
    ///  3) Вызвать SetTrue()/SetFalse()/SetValue(bool) из UnityEvent источника,
    ///     если он у него есть (например кастомный триггер с UnityEvent&lt;bool&gt;
    ///     или два отдельных UnityEvent "Включили"/"Выключили").
    ///  4) Если у источника нет UnityEvent (состояние вычисляется, реактивное
    ///     R3-свойство и т.п.) - под конкретный тип источника пишется
    ///     маленький специализированный мост, который сам вызывает SetValue
    ///     (см. пример DoorOpenStateToBoolChannelBridge для дверей проекта).
    /// </summary>
    public class StoryBoolChannelListener : MonoBehaviour
    {
        [SerializeField] private StoryBoolChannelSO m_Channel;

        [Tooltip("Если true - при включении объекта канал сразу инициализируется значением m_InitialValue.")]
        [SerializeField] private bool m_PushInitialValueOnEnable;
        [SerializeField] private bool m_InitialValue;

        private void OnEnable()
        {
            if (m_PushInitialValueOnEnable)
            {
                SetValue(m_InitialValue);
            }
        }

        public void SetValue(bool value)
        {
            if (m_Channel == null)
            {
                Debug.LogWarning($"{nameof(StoryBoolChannelListener)} on '{name}': channel is not assigned.", this);
                return;
            }

            m_Channel.SetValue(value);
        }

        public void ChangeValue()
        {
            SetValue(!m_InitialValue);
        }
    }
}

