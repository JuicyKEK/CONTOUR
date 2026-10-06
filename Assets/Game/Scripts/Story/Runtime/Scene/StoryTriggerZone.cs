using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Сюжетная триггер-зона: когда игрок входит в неё - поднимает сигнал сюжета (и/или ставит флаг).
    /// Типовой сюжетный триггер "игрок дошёл до места" без кода и без SO-ассетов.
    /// При добавлении компонента коллайдер становится триггером, а объект - слоем Ignore Raycast
    /// (чтобы зона не перехватывала лучи взаимодействия игрока).
    /// </summary>
    [JDIMonoController]
    public class StoryTriggerZone : MonoBehaviour
    {
        private const int IgnoreRaycastLayer = 2;

        [Tooltip("Сигнал, который поднимается при входе игрока. Можно оставить пустым.")]
        [SerializeField, StoryKey(StoryKeyKind.Signal)] private string m_EnterSignal;

        [Tooltip("Флаг, который выставляется в true при входе игрока. Можно оставить пустым.")]
        [SerializeField, StoryKey(StoryKeyKind.Flag)] private string m_EnterFlag;

        [Tooltip("Срабатывать только при первом входе.")]
        [SerializeField] private bool m_Once = true;

        [SerializeField] private string m_PlayerTag = "Player";

        [Inject] private IStoryState m_State;

        private bool m_IsTriggered;

        private void Reset()
        {
            var trigger = GetComponent<Collider>();

            if (trigger == null)
            {
                trigger = gameObject.AddComponent<BoxCollider>();
            }

            trigger.isTrigger = true;
            gameObject.layer = IgnoreRaycastLayer;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_State == null || (m_Once && m_IsTriggered) || !other.CompareTag(m_PlayerTag))
            {
                return;
            }

            m_IsTriggered = true;

            if (!string.IsNullOrEmpty(m_EnterFlag))
            {
                m_State.SetFlag(m_EnterFlag, true);
            }

            if (!string.IsNullOrEmpty(m_EnterSignal))
            {
                m_State.RaiseSignal(m_EnterSignal);
            }
        }
    }
}
