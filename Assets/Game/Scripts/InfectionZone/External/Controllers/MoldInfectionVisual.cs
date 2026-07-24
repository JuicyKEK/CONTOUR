using Game.Scripts.InfectionZone.Runtime.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Связывает визуальную интенсивность шейдера "Custom/MoldInfection" (чёрный пульсирующий
    /// грибок на стене) со степенью заражения зоны <see cref="IInfectionZone"/>.
    /// Вешается на объект стены (с MeshRenderer, использующим материал на основе MoldInfection.shader).
    /// Изменяет только параметр _InfectionAmount через MaterialPropertyBlock, не трогая
    /// оригинальный ассет материала - поэтому один материал можно переиспользовать
    /// на множестве стен с разной текущей степенью заражения.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class MoldInfectionVisual : MonoBehaviour
    {
        private static readonly int InfectionAmountId = Shader.PropertyToID("_InfectionAmount");

        [Header("Ссылка на зону")]
        [Tooltip("Зона, степень заражения которой отображает грибок на этой стене.")]
        [SerializeField] private MonoBehaviour m_ZoneSource; // должен реализовывать IInfectionZone

        [Header("Настройки отображения")]
        [Tooltip("Плавность перехода видимого % грибка при изменении заражения (0 = мгновенно).")]
        [SerializeField] private float m_SmoothTime = 0.5f;

        [Header("Ручное тестирование (для отладки в инспекторе)")]
        [Tooltip("Если включено - значение берётся из m_ManualTestAmount, а не из зоны заражения. " +
                 "Удобно крутить прямо в инспекторе в Play Mode, не привязываясь к сюжетной зоне.")]
        [SerializeField] private bool m_UseManualOverride;

        [Tooltip("Ручное значение % грибка (0..100) - используется, если включён m_UseManualOverride.")]
        [Range(0f, 100f)]
        [SerializeField] private float m_ManualTestAmount;

        private readonly CompositeDisposable m_Disposables = new();
        private MaterialPropertyBlock m_PropertyBlock;
        private Renderer m_Renderer;
        private IInfectionZone m_Zone;

        private float m_TargetAmount01;
        private float m_CurrentAmount01;
        private float m_Velocity;

        private void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_PropertyBlock = new MaterialPropertyBlock();

            m_Zone = m_ZoneSource as IInfectionZone;

            if (m_Zone == null && !m_UseManualOverride)
            {
                Debug.LogWarning($"[MoldInfectionVisual] {name}: m_ZoneSource не назначен или не реализует IInfectionZone. " +
                                  "Включите m_UseManualOverride, чтобы тестировать грибок вручную через m_ManualTestAmount.", this);
            }
        }

        private void OnEnable()
        {
            if (m_Zone != null)
            {
                // Сразу подхватываем текущую степень заражения без плавного перехода при активации.
                m_TargetAmount01 = m_Zone.InfectionLevel.CurrentValue / 100f;
                m_CurrentAmount01 = m_TargetAmount01;
                ApplyAmount(m_CurrentAmount01);

                m_Zone.InfectionLevel
                    .Subscribe(level => m_TargetAmount01 = level / 100f)
                    .AddTo(m_Disposables);
            }
            else
            {
                m_TargetAmount01 = m_ManualTestAmount / 100f;
                m_CurrentAmount01 = m_TargetAmount01;
                ApplyAmount(m_CurrentAmount01);
            }
        }

        private void OnDisable()
        {
            m_Disposables.Clear();
        }

        private void Update()
        {
            // Ручной режим имеет приоритет - позволяет крутить значение прямо в инспекторе
            // в Play Mode независимо от того, назначена зона или нет.
            if (m_UseManualOverride)
            {
                m_TargetAmount01 = m_ManualTestAmount / 100f;
            }
            else if (m_Zone == null)
            {
                // Ни зоны, ни ручного режима нет - применять нечего.
                return;
            }

            // Плавно подтягиваем видимый % грибка к целевому значению, чтобы разрастание/очистка
            // выглядели органично, а не скачком.
            m_CurrentAmount01 = Mathf.SmoothDamp(m_CurrentAmount01, m_TargetAmount01, ref m_Velocity, m_SmoothTime);
            ApplyAmount(m_CurrentAmount01);
        }

        private void ApplyAmount(float amount01)
        {
            m_Renderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetFloat(InfectionAmountId, amount01);
            m_Renderer.SetPropertyBlock(m_PropertyBlock);
        }
    }
}
