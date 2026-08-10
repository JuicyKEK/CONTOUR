using System;
using DG.Tweening;

namespace Game.Scripts.Lantern
{
    public class LanternPowerTweenTimer : ILanternPowerTimer
    {
        private float m_AccelerationCoefficient;
        private Tween m_CurrentTween;

        public void StartTimer(float delayInSeconds, Action callback, float accelerationCoefficient = 1)
        {
            m_AccelerationCoefficient = accelerationCoefficient;
            
            m_CurrentTween?.Kill();
            m_CurrentTween = DOVirtual.DelayedCall(delayInSeconds, () =>
            {
                callback?.Invoke();
            });
        }
        
        public void SetFastTimer(bool isFast)
        {
            if (m_CurrentTween == null)
            {
                return;
            }
            
            m_CurrentTween.timeScale = isFast ? m_AccelerationCoefficient : 1f;
        }

        public void StopTimer()
        {
            m_CurrentTween?.Kill();
            m_CurrentTween = null;
        }
        
        public float GetRemainingTime()
        {
            if (m_CurrentTween == null || !m_CurrentTween.IsActive())
                return 0f;

            return m_CurrentTween.Duration() - m_CurrentTween.Elapsed();
        }
    }
}