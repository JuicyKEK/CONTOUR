using Game.Scripts.InfectionZone.Runtime.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InfectionZone.Runtime.Controllers
{
    /// <summary>
    /// Объекты, которые появляются/исчезают в зависимости от степени заражения зоны.
    /// Используется для:
    /// - неинтерактивных аномалий, блокирующих проход игроку по сюжету (например 30-50%);
    /// - секретных аномалий/дверей, которые открываются, если игрок сам заразит зону
    ///   до нужного диапазона (например 60-70%).
    /// Лежит в контроллере сцены: один компонент - один диапазон заражения, в его списке - все объекты локации
    /// с этим диапазоном. Сами объекты ничего не знают о заражении.
    ///
    /// Заражение вошло в диапазон - объект включается; если у него есть анимация появления
    /// (<see cref="IInfectionObjectAnimation"/>), она запускается. Вышло из диапазона - если у включённого объекта есть
    /// такая анимация, объект исчезает через неё (например, плавится) и она сама его выключает; нет - объект просто
    /// выключается. Состояние на старте (из сохранения) применяется ко всем объектам сразу, без анимаций.
    /// Пока заражение меняется внутри диапазона (или вне его), объекты не трогаются.
    /// </summary>
    public class InfectionRangeObject : MonoBehaviour, IInfectionRangeObject
    {
        [Header("Ссылка на зону")]
        [Tooltip("Зона, за степенью заражения которой следит этот объект.")]
        [SerializeField] private MonoBehaviour m_ZoneSource; // должен реализовывать IInfectionZone

        [Header("Диапазон появления (%)")]
        [Tooltip("Нижняя граница диапазона заражения, при которой объект становится активным.")]
        [Range(0f, 100f)]
        [SerializeField] private float m_MinInfectionPercent = 30f;

        [Tooltip("Верхняя граница диапазона заражения, при которой объект остаётся активным.")]
        [Range(0f, 100f)]
        [SerializeField] private float m_MaxInfectionPercent = 50f;

        [Header("Что скрывать/показывать")]
        [Tooltip("Объекты, которые появляются в диапазоне и исчезают вне его. Есть у объекта анимация появления и " +
                 "исчезновения (IInfectionObjectAnimation, например InfectionCleanupMeltView) - запускается она. " +
                 "По умолчанию используется сам GameObject.")]
        [SerializeField] private GameObject[] m_TargetObjects;

        private readonly CompositeDisposable m_Disposables = new();
        private IInfectionZone m_Zone;
        private bool m_HasState;
        private bool m_IsInRange;

        public float MinInfectionPercent => m_MinInfectionPercent;
        public float MaxInfectionPercent => m_MaxInfectionPercent;

        private void Awake()
        {
            m_Zone = m_ZoneSource as IInfectionZone;

            if (m_Zone == null)
            {
                Debug.LogError($"[InfectionRangeObject] {name}: m_ZoneSource должен реализовывать IInfectionZone.", this);
            }

            if (m_TargetObjects == null || m_TargetObjects.Length == 0)
            {
                m_TargetObjects = new[] { gameObject };
            }
        }

        private void OnEnable()
        {
            if (m_Zone == null)
            {
                return;
            }

            m_Zone.InfectionLevel
                .Subscribe(UpdateVisibility)
                .AddTo(m_Disposables);
        }

        private void OnDisable()
        {
            m_Disposables.Clear();
        }

        /// <summary>
        /// Включает/выключает целевые объекты, когда степень заражения входит в заданный диапазон или выходит из него.
        /// </summary>
        private void UpdateVisibility(float infectionLevel)
        {
            bool isInRange = infectionLevel >= m_MinInfectionPercent && infectionLevel <= m_MaxInfectionPercent;

            if (m_HasState && isInRange == m_IsInRange)
            {
                return;
            }

            bool isFirst = !m_HasState;
            m_HasState = true;
            m_IsInRange = isInRange;

            foreach (var target in m_TargetObjects)
            {
                if (target == null)
                {
                    continue;
                }

                if (isFirst)
                {
                    target.SetActive(isInRange);
                }
                else if (isInRange)
                {
                    Appear(target);
                }
                else
                {
                    Disappear(target);
                }
            }
        }

        private static void Appear(GameObject target)
        {
            target.SetActive(true);

            if (target.TryGetComponent(out IInfectionObjectAnimation animation))
            {
                animation.PlayAppear();
            }
        }

        private static void Disappear(GameObject target)
        {
            if (target.activeInHierarchy && target.TryGetComponent(out IInfectionObjectAnimation animation))
            {
                animation.PlayDisappear();
                return;
            }

            target.SetActive(false);
        }
    }
}
