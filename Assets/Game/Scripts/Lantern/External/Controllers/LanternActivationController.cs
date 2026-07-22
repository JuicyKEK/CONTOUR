using UnityEngine;

namespace Game.Scripts.Lantern
{
    public class LanternActivationController : MonoBehaviour
    {
        [SerializeField] private GameObject m_DefaultLanternMode;
        [SerializeField] private GameObject m_FastLanternMode;
        
        public void ActivateLantern()
        {
            m_DefaultLanternMode.SetActive(true);
            m_FastLanternMode.SetActive(false);
        }
        
        public void DeactivateLantern()
        {
            m_DefaultLanternMode.SetActive(false);
            m_FastLanternMode.SetActive(false);
        }

        public void SetFastLantern(bool isFast)
        {
            m_DefaultLanternMode.SetActive(!isFast);
            m_FastLanternMode.SetActive(isFast);
        }
    }
}