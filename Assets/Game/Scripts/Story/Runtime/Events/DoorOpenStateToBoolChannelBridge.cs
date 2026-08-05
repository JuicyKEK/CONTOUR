using System;
using Game.Scripts.InteractionObjects.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Какое именно reactive-свойство двери форвардить в канал.
    /// </summary>
    public enum DoorStateField
    {
        IsOpen,
        IsLocked
    }

    /// <summary>
    /// Готовый сценный мост "дверь -> StoryBoolChannelSO" - конкретный пример
    /// того, как подключить произвольный reactive-источник (в данном случае
    /// R3 ReadOnlyReactiveProperty&lt;bool&gt; двери проекта) к обобщённому
    /// механизму BoolChannelCondition/StoryBoolChannelSO. Живёт на сцене (не
    /// SO-ассет), поэтому МОЖЕТ держать прямую ссылку на конкретную дверь -
    /// в этом весь смысл: сценные ссылки остаются здесь, а StoryCondition
    /// продолжает работать только с SO-каналом.
    ///
    /// Для любого другого типа источника (не двери) пишется аналогичный
    /// маленький мост: подписаться на его состояние и вызвать
    /// m_Channel.SetValue(...). Если у источника уже есть UnityEvent на
    /// изменение bool - отдельный мост вообще не нужен, используйте
    /// универсальный StoryBoolChannelListener.
    /// </summary>
    public class DoorOpenStateToBoolChannelBridge : MonoBehaviour
    {
        [Tooltip("Компонент двери, реализующий IInteractionDoor или IInteractionSimpleDoor (InteractionDoor / NonInteractionDoor).")]
        [SerializeField] private MonoBehaviour m_Door;

        [SerializeField] private DoorStateField m_Field = DoorStateField.IsOpen;
        [SerializeField] private StoryBoolChannelSO m_Channel;

        private IDisposable m_Subscription;

        private void Start()
        {
            var property = ResolveProperty();

            if (property == null || m_Channel == null)
            {
                Debug.LogWarning($"{nameof(DoorOpenStateToBoolChannelBridge)} on '{name}': m_Door/m_Channel не назначены, либо дверь не реализует нужный интерфейс для поля {m_Field}.", this);
                return;
            }

            switch (m_Field)
            {
                case DoorStateField.IsLocked: //TODO: Какой мрак, переделать если не забуду
                    m_Subscription = property.Subscribe(value => m_Channel.SetValue(!value));
                    break;

                default:
                    m_Subscription = property.Subscribe(value => m_Channel.SetValue(value));
                    break;
            }
            
        }

        private void OnDisable()
        {
            m_Subscription?.Dispose();
            m_Subscription = null;
        }

        private ReadOnlyReactiveProperty<bool> ResolveProperty()
        {
            switch (m_Field)
            {
                case DoorStateField.IsLocked:
                    return (m_Door as IInteractionDoor)?.IsLocked;

                default:
                    if (m_Door is IInteractionDoor door)
                    {
                        return door.IsOpen;
                    }
                    return (m_Door as IInteractionSimpleDoor)?.IsOpen;
            }
        }
    }
}


