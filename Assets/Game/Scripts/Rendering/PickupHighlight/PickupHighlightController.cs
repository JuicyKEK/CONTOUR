using Game.Scripts.Story;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Rendering
{
    /// <summary>
    /// Подсветка подбираемых предметов видна, только пока игрок управляет: когда управление отключают (осмотр предмета,
    /// катсцена - все IPlayerControlHandle), подсветка гаснет, иначе она рисуется поверх размытого фона осмотра.
    /// Лежит в префабе игрока.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(116)]
    public class PickupHighlightController : MonoBehaviour, IPlayerControlHandle, ISequence
    {
        public void MethodInit()
        {
            PickupHighlight.IsShown = true;
        }

        public void MethodStart()
        {
        }

        public void SetControlEnabled(bool isEnabled)
        {
            PickupHighlight.IsShown = isEnabled;
        }

        private void OnDestroy()
        {
            PickupHighlight.IsShown = true;
        }
    }
}
