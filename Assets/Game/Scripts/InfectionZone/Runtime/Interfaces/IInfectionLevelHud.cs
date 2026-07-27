namespace Game.Scripts.InfectionZone.Runtime.Interfaces
{
    /// <summary>
    /// HUD, показывающий вверху экрана надпись "Уровень заражения {Имя зоны}:" и слайдер
    /// с текущей степенью заражения (0..100). Используется, когда игрок входит в локацию
    /// (показывает статичный текущий уровень) и когда уровень заражения зоны меняется,
    /// пока игрок в ней находится (показывает анимированный переход от старого значения
    /// к новому). В обоих случаях HUD автоматически скрывается спустя некоторое время.
    /// </summary>
    public interface IInfectionLevelHud
    {
        /// <summary>
        /// Показать текущий уровень заражения зоны без анимации значения (например при входе
        /// игрока в локацию) - слайдер сразу выставляется на <paramref name="level"/>.
        /// </summary>
        void ShowCurrentLevel(string zoneDisplayName, float level);

        /// <summary>
        /// Показать изменение уровня заражения зоны (например после очистки аномалии) -
        /// слайдер сначала показывает <paramref name="fromLevel"/>, затем плавно
        /// анимируется к <paramref name="toLevel"/>.
        /// </summary>
        void ShowLevelChange(string zoneDisplayName, float fromLevel, float toLevel);
    }
}

