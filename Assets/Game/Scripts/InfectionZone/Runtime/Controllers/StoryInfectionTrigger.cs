using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InfectionZone.Runtime.Controllers
{
    /// <summary>
    /// Пример компонента-триггера сюжетного события заражения.
    /// Повесьте на объект-триггер сюжетного события (кат-сцена, диалог, скрипт квеста)
    /// и вызовите <see cref="TriggerInfection"/>, чтобы увеличить заражение нужной зоны
    /// на заданное в инспекторе значение. Число заражения определяется в момент
    /// наступления события - то есть настраивается прямо здесь, для каждого события отдельно.
    /// </summary>
    [JDIMonoController]
    public class StoryInfectionTrigger : MonoBehaviour
    {
        [Header("Настройки сюжетного заражения")]
        [Tooltip("Id зоны, которую заразит это сюжетное событие.")]
        [SerializeField] private string m_TargetZoneId;

        [Tooltip("На сколько процентов увеличится заражение зоны при срабатывании события.")]
        [SerializeField] private float m_InfectionAmount = 20f;

        [Inject] private IInfectionZoneRegistry m_Registry;

        /// <summary>
        /// Вызывается сюжетным/квестовым событием (например из UnityEvent в таймлайне,
        /// из диалоговой системы или напрямую из кода квеста).
        /// </summary>
        public void TriggerInfection()
        {
            var zone = m_Registry?.GetZone(m_TargetZoneId);

            if (zone == null)
            {
                Debug.LogError($"[StoryInfectionTrigger] Зона с Id '{m_TargetZoneId}' не найдена в реестре.", this);
                return;
            }

            zone.IncreaseInfection(m_InfectionAmount);
        }
    }
}
