namespace Game.Scripts.Instructions.Interfaces
{
    /// <summary>
    /// Хранит, какие аудио-кассеты уже найдены игроком по ходу игры. Регистрируется как
    /// глобальный бин JuicyDI, чтобы объекты подбора кассет и UI плеера могли получить
    /// доступ через [Inject] без прямых ссылок на сцене.
    /// </summary>
    public interface IAudioTapeFoundRegistry
    {
        /// <summary>
        /// Найдена ли кассета с данным TapeId.
        /// </summary>
        bool IsTapeFound(string tapeId);

        /// <summary>
        /// Отметить кассету как найденную (вызывается объектом подбора кассеты в мире).
        /// </summary>
        void MarkTapeFound(string tapeId);
    }
}

