using DG.Tweening;
using Game.Scripts.InfectionZone.External.View;
using UnityEngine;

namespace Game.TestScenes.EffectsShowcase
{
    /// <summary>
    /// Сцена эффектов: плавление очищенной аномалии так же, как в игре (InfectionCleanupMeltView - тот же материал
    /// и пузыри). По кругу: объект целый, плавится и исчезает, пауза, снова целый. Стоит на родителе показываемого
    /// объекта: сам объект при исчезновении выключается. Только для показа.
    /// </summary>
    public class CleanseMeltShowcase : MonoBehaviour
    {
        [Tooltip("Вид плавления на показываемом объекте (у него нет InfectionCleanupObject - плавление запускает этот скрипт).")]
        [SerializeField] private InfectionCleanupMeltView m_View;

        [Tooltip("Пауза перед плавлением, секунды.")]
        [SerializeField] private float m_Hold = 1.5f;

        [Tooltip("Сколько секунд от начала плавления до появления целого объекта (больше длительности плавления).")]
        [SerializeField] private float m_Cycle = 7f;

        private Sequence m_Sequence;

        private void Start()
        {
            m_Sequence = DOTween.Sequence()
                .AppendInterval(m_Hold)
                .AppendCallback(m_View.Melt)
                .AppendInterval(m_Cycle)
                .AppendCallback(() => m_View.gameObject.SetActive(true))
                .SetLoops(-1)
                .SetLink(gameObject);
        }

        private void OnDestroy()
        {
            m_Sequence?.Kill();
        }
    }
}
