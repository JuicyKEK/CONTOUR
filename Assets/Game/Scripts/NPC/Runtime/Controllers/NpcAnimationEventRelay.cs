using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Принимает события анимации бота и передаёт их NpcController. Вешается на объект с Animator модели
    /// (события анимации вызывают функции только у компонентов этого объекта).
    /// В клипах анимации: событие "Footstep" - шаг (если в профиле звуков включены шаги из анимации),
    /// событие "AnimationAction" со строкой - звук действия по ключу профиля (например 'Swing').
    /// </summary>
    public class NpcAnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private NpcController m_Owner;

        private void Awake()
        {
            if (m_Owner == null)
            {
                m_Owner = GetComponentInParent<NpcController>();
            }
        }

        public void Footstep()
        {
            m_Owner?.PlayFootstep();
        }

        public void AnimationAction(string key)
        {
            m_Owner?.PlayAnimationSound(key);
        }
    }
}
