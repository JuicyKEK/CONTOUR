using Game.Scripts.InfectionZone.Runtime.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InfectionZone.Runtime.Controllers
{
    /// <summary>
    /// Объект, который появляется/исчезает в зависимости от степени заражения зоны.
    /// Используется для:
    /// - неинтерактивных аномалий, блокирующих проход игроку по сюжету (например 30-50%);
    /// - секретных аномалий/дверей, которые открываются, если игрок сам заразит зону
    ///   до нужного диапазона (например 60-70%).
    /// Объект не реализует IInteraction - игрок не может провзаимодействовать с ним напрямую,
    /// единственный способ убрать его - изменить степень заражения зоны.
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
        [Tooltip("Визуальные и физические объекты, которые нужно включать/выключать. По умолчанию используется сам GameObject.")]
        [SerializeField] private GameObject[] m_TargetObjects;

        private readonly CompositeDisposable m_Disposables = new();
        private IInfectionZone m_Zone;

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
        /// Включает/выключает целевые объекты в зависимости от того, попадает ли
        /// текущая степень заражения в заданный диапазон.
        /// </summary>
        private void UpdateVisibility(float infectionLevel)
        {
            bool isInRange = infectionLevel >= m_MinInfectionPercent && infectionLevel <= m_MaxInfectionPercent;

            foreach (var target in m_TargetObjects)
            {
                if (target != null)
                {
                    target.SetActive(isInRange);
                }
            }
        }
    }
}
