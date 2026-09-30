namespace Game.Scripts.InfectionZone.Runtime.Interfaces
{
    /// <summary>
    /// Сохранение/загрузка уровней заражения зон (JSON-файл: Id зоны -> уровень заражения).
    /// Бин сцены JuicyDI - систему чекпоинтов можно подключить через [Inject].
    /// </summary>
    public interface IInfectionZoneSaveService
    {
        /// <summary>
        /// Есть ли файл сохранения зон.
        /// </summary>
        bool HasSave { get; }

        /// <summary>
        /// Сохраняет уровни заражения всех зон в JSON-файл. Вызывается на чекпоинтах.
        /// </summary>
        void Save();

        /// <summary>
        /// Загружает сохранение и выставляет зонам сохранённые уровни заражения.
        /// Возвращает false, если сохранения нет или его не удалось прочитать (зоны тогда не меняются).
        /// </summary>
        bool Load();

        /// <summary>
        /// Удаляет файл сохранения зон (например, при старте новой игры).
        /// </summary>
        void DeleteSave();
    }
}
