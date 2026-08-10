using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Player.View
{
    public class PlayerInteractiveView : MonoBehaviour
    {
        [SerializeField] private Image m_InteractiveImage;

        public void ShowInteractiveImage(bool isCanInteract)
        {
            m_InteractiveImage.gameObject.SetActive(isCanInteract);
        }
    }
}