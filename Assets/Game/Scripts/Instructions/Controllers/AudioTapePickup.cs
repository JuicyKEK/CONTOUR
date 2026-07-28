using Game.Scripts.Instructions.Data;
using Game.Scripts.Instructions.Interfaces;
using Game.Scripts.Inventory;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Физический объект кассеты в мире. При взаимодействии игрока (через общую систему
    /// IInteraction/PlayerInteractiveController) отмечает кассету найденной в реестре
    /// <see cref="IAudioTapeFoundRegistry"/> и убирает себя со сцены - после этого её
    /// настоящее имя появится в списке кассетного плеера вместо "?????".
    /// </summary>
    [JDIMonoController]
    public class AudioTapePickup : MonoBehaviour, IInteraction
    {
        [Header("Кассета")]
        [Tooltip("Какую кассету подбирает этот объект.")]
        [SerializeField] private AudioTapeDefinitionSO m_TapeDefinition;

        [Inject] private IAudioTapeFoundRegistry m_Registry;

        private void Awake()
        {
            if (m_TapeDefinition == null)
            {
                Debug.LogError($"[AudioTapePickup] {name}: не назначен m_TapeDefinition.", this);
            }
        }

        public void Interact()
        {
            if (m_TapeDefinition == null)
            {
                return;
            }

            m_Registry?.MarkTapeFound(m_TapeDefinition.TapeId);
            gameObject.SetActive(false);
        }
    }
}

