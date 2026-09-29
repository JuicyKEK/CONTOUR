using System.Collections.Generic;

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
        /// Id всех кассет, доступных игроку на текущий момент (используется для сохранения).
        /// </summary>
        IReadOnlyCollection<string> FoundTapeIds { get; }

        /// <summary>
        /// Найдена ли кассета с данным TapeId.
        /// </summary>
        bool IsTapeFound(string tapeId);

        /// <summary>
        /// Отметить кассету как найденную (вызывается объектом подбора кассеты в мире).
        /// </summary>
        void MarkTapeFound(string tapeId);

        /// <summary>
        /// Полностью заменяет набор найденных кассет (используется при загрузке сохранения).
        /// </summary>
        void RestoreFoundTapes(IEnumerable<string> tapeIds);
    }
}

