using System;

namespace Game.Scripts.Lantern
{
    public interface ILanternPowerTimer
    {
        void StartTimer(float delayInSeconds, Action callback, float accelerationCoefficient);
        void SetFastTimer(bool fastTimer);
        void StopTimer();
        float GetRemainingTime();
    }
}