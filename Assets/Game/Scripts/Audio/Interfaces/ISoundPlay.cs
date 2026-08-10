using R3;

namespace Game.Scripts.Audio.Interfaces
{
    public interface ISoundPlay
    {
        public Subject<Unit> IsPlaySound { get; }
    }
}