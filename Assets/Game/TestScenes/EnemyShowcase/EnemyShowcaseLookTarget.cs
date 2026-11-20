using UnityEngine;

namespace Game.TestScenes.EnemyShowcase
{
    /// <summary>
    /// Сцена просмотра противников: метка «игрок», которая плавно ходит туда-сюда перед рядом монстров. За ней следят
    /// головы экземпляров с включённым MonsterHeadLook - видно, что голова поворачивается, а анимация идёт своя.
    /// </summary>
    public class EnemyShowcaseLookTarget : MonoBehaviour
    {
        [Tooltip("Насколько метка уходит в каждую сторону от начального места, м.")]
        [SerializeField] private float m_Range = 4f;

        [Tooltip("Время одного прохода туда и обратно, с.")]
        [SerializeField] private float m_Period = 8f;

        [Tooltip("Насколько метка поднимается и опускается, м.")]
        [SerializeField] private float m_Bob = 0.4f;

        private Vector3 m_Start;

        private void Start()
        {
            m_Start = transform.localPosition;
        }

        private void Update()
        {
            float phase = Time.time / m_Period * Mathf.PI * 2f;
            transform.localPosition = m_Start + new Vector3(Mathf.Sin(phase) * m_Range, Mathf.Sin(phase * 2.3f) * m_Bob, 0f);
        }
    }
}
