namespace Game.Scripts.Utilities.Save
{
    /// <summary>
    /// Механика, которая участвует в общем сохранении игры (<see cref="IGameSaveService"/>).
    /// Чтобы механика попала в пул сейвов, её бин JuicyDI реализует этот интерфейс -
    /// GameSaveController найдёт его сам.
    ///
    /// Загрузка - не здесь: каждая механика грузит свой сейв при старте сцены сама (у неё свой
    /// правильный момент - до того, как подписчики увидят стартовые значения).
    /// </summary>
    public interface ISaveParticipant
    {
        /// <summary>
        /// Есть ли у механики файл сохранения.
        /// </summary>
        bool HasSave { get; }

        /// <summary>
        /// Записать текущее состояние механики в её файл сохранения.
        /// </summary>
        void Save();

        /// <summary>
        /// Удалить файл сохранения механики (новая игра).
        /// </summary>
        void DeleteSave();
    }

    /// <summary>
    /// Общее сохранение игры: обходит все механики-участники (<see cref="ISaveParticipant"/>) на
    /// загруженных сценах. SaveGame вызывается на чекпоинтах - из кода ([Inject] IGameSaveService)
    /// или из графа сюжета (действие "Save/Save Game").
    /// </summary>
    public interface IGameSaveService
    {
        /// <summary>
        /// Есть ли сохранение хотя бы у одной механики (например, для кнопки "Продолжить").
        /// </summary>
        bool HasSave { get; }

        /// <summary>
        /// Сохранить состояние всех механик.
        /// </summary>
        void SaveGame();

        /// <summary>
        /// Удалить сохранения всех механик (новая игра).
        /// </summary>
        void DeleteSaves();
    }
}
