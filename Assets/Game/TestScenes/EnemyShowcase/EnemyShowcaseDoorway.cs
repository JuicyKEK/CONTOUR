using Game.Scripts.NPC.Runtime.Data;
using UnityEngine;

namespace Game.TestScenes.EnemyShowcase
{
    /// <summary>
    /// Сцена просмотра противников: монстр проходит дверной проём анимацией прохода (Door_Pass, Door_Squeeze и их
    /// варианты бегом). Клипы сделаны на месте: в игре бота через проём ведёт NpcDoorwayTraversal по данным из профиля
    /// анимаций и звуков монстра (NpcAnimationSoundProfileSO, "Проход в дверной проём"), здесь этот объект ведёт
    /// монстра по тем же данным и той же формуле. Позиция этого объекта - центр проёма на полу, его ось Z -
    /// направление прохода. В начале клипа монстр стоит за StartDistance метров до проёма и за клип проходит
    /// Travel метров по кривой Progress. Время берётся из состояния Animator монстра, поэтому монстр всегда в такт
    /// анимации; при новом проигрывании клипа (EnemyShowcaseClip, повтор) он снова перед проёмом.
    /// Только для показа.
    /// </summary>
    public class EnemyShowcaseDoorway : MonoBehaviour
    {
        [Tooltip("Animator монстра, который проходит этот проём.")]
        [SerializeField] private Animator m_Animator;

        [Tooltip("Профиль анимаций и звуков монстра: из него берутся состояние, расстояния и кривая прохода.")]
        [SerializeField] private NpcAnimationSoundProfileSO m_Profile;

        [Tooltip("Проход бегом (иначе шагом).")]
        [SerializeField] private bool m_Run;

        private NpcDoorwayClip m_Clip;
        private bool m_HasClip;
        private int m_StateHash;

        private void Start()
        {
            m_HasClip = m_Animator != null && m_Profile != null && m_Profile.TryGetDoorwayClip(m_Run, out m_Clip);
            if (m_HasClip)
            {
                m_StateHash = Animator.StringToHash(m_Clip.AnimatorState);
                Place(0f);
            }
        }

        private void LateUpdate()
        {
            if (!m_HasClip)
            {
                return;
            }

            // пока играет клип прохода - монстр там, где его поставил бы NpcDoorwayTraversal; между повторами стоит
            // там, где клип закончился
            AnimatorStateInfo info = m_Animator.GetCurrentAnimatorStateInfo(0);
            if (info.shortNameHash == m_StateHash)
            {
                Place(Mathf.Clamp01(info.normalizedTime));
            }
        }

        private void Place(float u)
        {
            float progress = m_Clip.Progress != null && m_Clip.Progress.length > 0 ? m_Clip.Progress.Evaluate(u) : u;
            float along = m_Clip.Travel * progress - m_Clip.StartDistance;   // метры за плоскостью проёма
            m_Animator.transform.SetPositionAndRotation(transform.position + transform.forward * along, transform.rotation);
        }
    }
}
