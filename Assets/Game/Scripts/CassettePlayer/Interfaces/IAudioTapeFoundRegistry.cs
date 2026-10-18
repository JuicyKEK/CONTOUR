using System.Collections.Generic;

namespace Game.Scripts.Instructions.Interfaces
{
    /// <summary>
    /// Хранит прогресс аудио-кассет: какие кассеты уже найдены игроком, какие уже прослушаны
    /// и задачи каких кассет уже выполнены. Регистрируется как глобальный бин JuicyDI, чтобы объекты подбора кассет,
    /// UI плеера и сюжет (через StoryContext) могли получить доступ без прямых ссылок на сцене.
    /// </summary>
    public interface IAudioTapeFoundRegistry
    {
        /// <summary>
        /// Id всех кассет, доступных игроку на текущий момент (используется для сохранения).
        /// </summary>
        IReadOnlyCollection<string> FoundTapeIds { get; }

        /// <summary>
        /// Id кассет, которые игрок уже запускал - у них нет метки "НОВОЕ" (используется для сохранения).
        /// </summary>
        IReadOnlyCollection<string> ListenedTapeIds { get; }

        /// <summary>
        /// Id кассет, задачи из которых игрок уже выполнил (используется для сохранения).
        /// </summary>
        IReadOnlyCollection<string> CompletedTapeIds { get; }

        /// <summary>
        /// Найдена ли кассета с данным TapeId.
        /// </summary>
        bool IsTapeFound(string tapeId);

        /// <summary>
        /// Отметить кассету как найденную (вызывается объектом подбора кассеты в мире).
        /// </summary>
        void MarkTapeFound(string tapeId);

        /// <summary>
        /// Запускал ли игрок кассету с данным TapeId.
        /// </summary>
        bool IsTapeListened(string tapeId);

        /// <summary>
        /// Отметить кассету как прослушанную (вызывается плеером при запуске кассеты).
        /// </summary>
        void MarkTapeListened(string tapeId);

        /// <summary>
        /// Выполнена ли задача кассеты с данным TapeId.
        /// </summary>
        bool IsTapeCompleted(string tapeId);

        /// <summary>
        /// Отметить задачу кассеты как выполненную (вызывается из сюжета - MarkAudioTapeCompletedAction).
        /// </summary>
        void MarkTapeCompleted(string tapeId);

        /// <summary>
        /// Полностью заменяет набор найденных кассет (используется при загрузке сохранения).
        /// </summary>
        void RestoreFoundTapes(IEnumerable<string> tapeIds);

        /// <summary>
        /// Полностью заменяет набор прослушанных кассет (используется при загрузке сохранения).
        /// </summary>
        void RestoreListenedTapes(IEnumerable<string> tapeIds);

        /// <summary>
        /// Полностью заменяет набор выполненных кассет (используется при загрузке сохранения).
        /// </summary>
        void RestoreCompletedTapes(IEnumerable<string> tapeIds);
    }
}

