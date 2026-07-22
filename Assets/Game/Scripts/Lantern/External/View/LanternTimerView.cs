using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Scripts.Lantern.View
{
    public class LanternTimerView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup m_LanternBarCG;
        [SerializeField] private Image m_LanternBackBar;
        [SerializeField] private Image m_LanternActiveBar;
        
        private float m_SprintDefaultBarHeightPercent = .015f;
        private float m_SprintDefaultBarWidthPercent = .2f;
        private bool m_HideBarWhenFull = true;
        private float m_DefaultWidth;

        public void Init()
        {
            m_LanternBackBar.gameObject.SetActive(true);
            m_LanternActiveBar.gameObject.SetActive(true);
            
            m_LanternBackBar.rectTransform.sizeDelta = new Vector3(Screen.width * m_SprintDefaultBarWidthPercent,
                Screen.height * m_SprintDefaultBarHeightPercent, 0f);
            m_LanternActiveBar.rectTransform.sizeDelta = new Vector3(Screen.width * m_SprintDefaultBarWidthPercent - 2,
                Screen.height * m_SprintDefaultBarHeightPercent - 2, 0f);
            
            m_DefaultWidth = m_LanternActiveBar.rectTransform.sizeDelta.x;

            if(m_HideBarWhenFull)
            {
                m_LanternBarCG.alpha = 0;
            }
        }

        public void SetActiveBarView(float barWidthPercent)
        {
            m_LanternActiveBar.rectTransform.sizeDelta = new Vector3(m_DefaultWidth * barWidthPercent,
                m_LanternActiveBar.rectTransform.sizeDelta.y, 0f);
        }

        public void ChangeBarBack(float barWidthPercent)
        {
            m_LanternBackBar.rectTransform.sizeDelta = new Vector3(Screen.width * m_SprintDefaultBarWidthPercent * barWidthPercent,
                Screen.height * m_SprintDefaultBarHeightPercent, 0f);
            m_LanternActiveBar.rectTransform.sizeDelta = new Vector3(Screen.width * m_SprintDefaultBarWidthPercent * barWidthPercent - 2,
                Screen.height * m_SprintDefaultBarHeightPercent - 2, 0f);
            
            m_DefaultWidth = m_LanternActiveBar.rectTransform.sizeDelta.x;
        }

        public void HideBar() //? мб сделать плавным?
        {
            m_LanternBarCG.alpha = 0;
        }
        
        public void ShowBar()
        {
            m_LanternBarCG.alpha = 1;
        }
    }
}