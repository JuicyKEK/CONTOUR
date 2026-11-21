using Game.Scripts.Instructions.Controllers;
using Game.Scripts.ItemInspection;
using Game.Scripts.ItemInspection.Controllers;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Кассета для осмотра при подборе (InspectOnFirstPickup на том же объекте): обычная кассета и злая
    /// (AudioTapeDefinitionSO.IsEvilCassette) - разные типы, поэтому первую обычную и первую злую кассету осматривают
    /// отдельно; поменяли кассету у AudioTapePickup - тип меняется сам. После осмотра кассета улетает в кассетный
    /// плеер (модель плеера у игрока, CassettePlayerModelView), а не в инвентарь.
    /// </summary>
    [JDIMonoController]
    [RequireComponent(typeof(InspectOnFirstPickup))]
    public class AudioTapeInspectionInfo : MonoBehaviour, IItemInspectionInfoSource, IItemInspectionFlyTargetSource
    {
        [Header("Обычная кассета")]
        [Tooltip("Тип для осмотра: осматривается первая подобранная кассета этого типа.")]
        [SerializeField] private string m_NormalTypeKey = "AudioTape";
        [Tooltip("Подпись под кассетой при осмотре.")]
        [SerializeField] private string m_NormalDisplayName = "Кассета";

        [Header("Злая кассета")]
        [SerializeField] private string m_EvilTypeKey = "AudioTape_Evil";
        [SerializeField] private string m_EvilDisplayName = "Злая кассета";

        [Inject] private CassettePlayerModelView m_CassettePlayerModel;

        private AudioTapePickup m_Pickup;

        public bool TryGetInspectionInfo(out string typeKey, out string displayName)
        {
            bool isEvil = IsEvil();
            typeKey = isEvil ? m_EvilTypeKey : m_NormalTypeKey;
            displayName = isEvil ? m_EvilDisplayName : m_NormalDisplayName;
            return true;
        }

        public Transform GetFlyTarget()
        {
            return m_CassettePlayerModel != null ? m_CassettePlayerModel.transform : null;
        }

        private bool IsEvil()
        {
            if (m_Pickup == null)
            {
                m_Pickup = GetComponent<AudioTapePickup>();
            }

            return m_Pickup != null && m_Pickup.TapeDefinition != null && m_Pickup.TapeDefinition.IsEvilCassette;
        }
    }
}
