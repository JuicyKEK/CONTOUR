namespace Game.Scripts.Story
{
    /// <summary>
    /// Реализуется контроллерами игрока (ввод, взаимодействие, движение и т.д.),
    /// которые story-механика должна уметь временно отключать/включать
    /// (например на время катсцены).
    /// </summary>
    public interface IPlayerControlHandle
    {
        void SetControlEnabled(bool isEnabled);
    }
}

