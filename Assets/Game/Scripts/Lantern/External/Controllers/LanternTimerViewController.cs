using Game.Scripts.Lantern.View;
using UnityEngine;

namespace Game.Scripts.Lantern
{
    public class LanternTimerViewController : MonoBehaviour
    {
        [SerializeField] private LanternTimerView m_LanternTimerView;

        public void Init()
        {
            m_LanternTimerView.Init();
        }

        public void SetBarTimer(float timePercent)
        {
            m_LanternTimerView.SetActiveBarView(timePercent);
        }

        public void ShowBar(bool isShow)
        {
            if (isShow)
            {
                m_LanternTimerView.ShowBar();
            }
            else
            {
                m_LanternTimerView.HideBar();
            }
        }
    }
}