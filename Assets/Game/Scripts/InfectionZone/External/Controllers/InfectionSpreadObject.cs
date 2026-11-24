using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using R3;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Интерактивное место заражения (гнездо) - обратная сторона <see cref="InfectionCleanupObject"/>. Если у игрока в
    /// инвентаре выбран предмет с ключом <see cref="m_RequiredItemKey"/>, предмет расходуется, заражение зоны
    /// увеличивается, а гнездо становится разросшимся (<see cref="IsSpread"/>). Как оно разрастается, показывает вид:
    /// InfectionSpreadGrowView. Так игрок сам может довести зону до нужного диапазона (например, чтобы открылись
    /// секретные аномалии/двери <see cref="Runtime.Controllers.InfectionRangeObject"/>). Что гнездо разрослось, запоминает
    /// InfectionSpreadStateSaver. Общая логика - в <see cref="InfectionItemInteraction"/>.
    /// </summary>
    [JDIMonoController]
    public class InfectionSpreadObject : InfectionItemInteraction, IInfectionSpreadObject
    {
        public ReadOnlyReactiveProperty<bool> IsSpread => m_IsSpread;
        public bool IsRestoringState => m_IsRestoringState;

        [Header("Настройки заражения")]
        [Tooltip("Ключ предмета инвентаря, которым игрок заражает зону.")]
        [SerializeField] private string m_RequiredItemKey;

        [Tooltip("На сколько процентов увеличится заражение зоны при использовании.")]
        [SerializeField] private float m_IncreaseAmount = 10f;

        [Tooltip("Заразить можно один раз: после этого гнездо разросшееся, его коллайдеры выключаются и с ним больше " +
                 "не взаимодействуют. Выключено - место можно использовать повторно, каждый раз расходуя предмет.")]
        [FormerlySerializedAs("m_DisableAfterUse")]
        [SerializeField] private bool m_SingleUse = true;

        private readonly ReactiveProperty<bool> m_IsSpread = new(false);
        private bool m_IsRestoringState;

        protected override string RequiredItemKey => m_RequiredItemKey;

        protected override bool CanInteract => !m_SingleUse || !m_IsSpread.Value;

        public void RestoreSpread()
        {
            m_IsRestoringState = true;

            try
            {
                Spread();
            }
            finally
            {
                m_IsRestoringState = false;
            }
        }

        protected override void ChangeInfection(IInfectionZone zone)
        {
            zone.IncreaseInfection(m_IncreaseAmount);
        }

        protected override void OnUsed()
        {
            Spread();
        }

        private void Spread()
        {
            m_IsSpread.Value = true;

            if (!m_SingleUse)
            {
                return;
            }

            foreach (var spreadCollider in GetComponents<Collider>())
            {
                spreadCollider.enabled = false;
            }
        }

        private void OnDestroy()
        {
            m_IsSpread.Dispose();
        }
    }
}
