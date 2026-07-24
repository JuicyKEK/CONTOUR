namespace Game.Scripts.InfectionZone.Runtime.Interfaces
{
    /// <summary>
    /// Объект, чья видимость/активность зависит от степени заражения зоны
    /// (аномалии, преграды, секретные двери и т.п.).
    /// </summary>
    public interface IInfectionRangeObject
    {
        /// <summary>
        /// Нижняя граница диапазона заражения (включительно), при которой объект активен.
        /// </summary>
        float MinInfectionPercent { get; }

        /// <summary>
        /// Верхняя граница диапазона заражения (включительно), при которой объект активен.
        /// </summary>
        float MaxInfectionPercent { get; }
    }
}
