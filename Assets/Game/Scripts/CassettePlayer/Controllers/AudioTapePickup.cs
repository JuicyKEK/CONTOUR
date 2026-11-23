using Game.Scripts.Instructions.Data;
using Game.Scripts.Instructions.Interfaces;
using Game.Scripts.Pickup.Controllers;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Физический объект кассеты в мире. При подборе отмечает кассету найденной в реестре
    /// <see cref="IAudioTapeFoundRegistry"/> - после этого её настоящее имя появится в списке кассетного плеера вместо
    /// "?????". Убирает объект со сцены вид на нём (PickupHideView).
    /// </summary>
    [JDIMonoController]
    public class AudioTapePickup : PickupObject
    {
        [Header("Кассета")]
        [Tooltip("Какую кассету подбирает этот объект.")]
        [SerializeField] private AudioTapeDefinitionSO m_TapeDefinition;

        [Inject] private IAudioTapeFoundRegistry m_Registry;

        /// <summary>
        /// Какую кассету подбирает этот объект (например, подсветка берёт отсюда, злая ли кассета).
        /// </summary>
        public AudioTapeDefinitionSO TapeDefinition => m_TapeDefinition;

        private void Awake()
        {
            if (m_TapeDefinition == null)
            {
                Debug.LogError($"[AudioTapePickup] {name}: не назначен m_TapeDefinition.", this);
            }
        }

        protected override bool TryReceive()
        {
            if (m_TapeDefinition == null)
            {
                return false;
            }

            m_Registry?.MarkTapeFound(m_TapeDefinition.TapeId);
            return true;
        }
    }
}
