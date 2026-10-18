using System;
using Game.Scripts.InteractionObjects.Interfaces;
using JuicyDI;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Держит флаг сюжета равным состоянию двери (открыта/заперта). Замена связке
    /// "DoorOpenStateToBoolChannelBridge + StoryBoolChannelSO": SO-канал не нужен,
    /// сюжет ждёт флаг условием графа "Flag Is".
    /// </summary>
    [JDIMonoController]
    public class DoorStoryFlagBridge : MonoBehaviour, ISequence
    {
        [Tooltip("Компонент двери, реализующий IInteractionDoor или IInteractionSimpleDoor (InteractionDoor / NonInteractionDoor).")]
        [SerializeField] private MonoBehaviour m_Door;
        [SerializeField] private DoorStateField m_Field = DoorStateField.IsOpen;
        [Tooltip("Записывать в флаг инвертированное значение (например 'дверь НЕ заперта').")]
        [SerializeField] private bool m_Invert;
        [SerializeField, StoryKey(StoryKeyKind.Flag)] private string m_FlagKey;

        [Inject] private IStoryState m_State;

        private IDisposable m_Subscription;

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            // Подписка в MethodStart, а не в Start: к этому моменту JuicyDI уже прокинул IStoryState.
            var property = ResolveProperty();

            if (property == null || string.IsNullOrEmpty(m_FlagKey))
            {
                Debug.LogWarning($"[{nameof(DoorStoryFlagBridge)}] {name}: не назначена дверь или ключ флага, " +
                                 $"либо дверь не реализует нужный интерфейс для поля {m_Field}.", this);
                return;
            }

            m_Subscription = property.Subscribe(value => m_State?.SetFlag(m_FlagKey, value != m_Invert));
        }

        private void OnDestroy()
        {
            m_Subscription?.Dispose();
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
