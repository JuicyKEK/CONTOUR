namespace Game.Scripts.Lantern
{
    public interface ILanternPowerTimerServices
    {
        void StartTimer(float allTime, float remainingTime);
        void UpdateTimeView();
        void SetFastTimer(bool fastTimer);
        void StopTimer();
        float RemainingTime { get; }
    }
}