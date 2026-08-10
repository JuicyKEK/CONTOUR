using System;
using UnityEngine;

namespace Game.Scripts.Lantern
{
    public class LanternPowerTimerServices : ILanternPowerTimerServices
    {
        private const float m_AccelerationCoefficient = 5; //мб в конструкторе передавать?
        
        private readonly ILanternPowerTimer m_LanternPowerTimer = new LanternPowerTweenTimer();
        private readonly LanternTimerViewController m_LanternTimerViewController;
        private readonly Action m_CallbackOnTimerEnd;

        private float m_FullTime;

        public float RemainingTime => m_LanternPowerTimer.GetRemainingTime();
        
        public LanternPowerTimerServices(Action callbackOnTimerEnd, LanternTimerViewController viewController)
        {
            m_CallbackOnTimerEnd = callbackOnTimerEnd;
            m_LanternTimerViewController = viewController;
            m_LanternTimerViewController.Init();
        }
        
        public void StopTimer()
        {
            m_LanternPowerTimer.StopTimer();
            m_LanternTimerViewController.ShowBar(false);
        }
        
        public void SetFastTimer(bool fastTimer)
        {
            m_LanternPowerTimer.SetFastTimer(fastTimer);
        }

        public void StartTimer(float allTime, float remainingTime)
        {
            m_LanternPowerTimer.StartTimer(remainingTime, m_CallbackOnTimerEnd, m_AccelerationCoefficient);
            m_LanternTimerViewController.ShowBar(true);
            m_FullTime = allTime;
        }

        public void UpdateTimeView()
        {
            m_LanternTimerViewController.SetBarTimer(RemainingTime / m_FullTime);
        }
    }
}