using System.Collections.Generic;

namespace Game.Scripts.ItemInspection
{
    /// <summary>
    /// Какие типы предметов игрок уже осматривал при подборе (ItemInspectionRegistry). Сохраняется в свой файл
    /// (ItemInspectionSaveController), отдельно от инвентаря и кассет.
    /// </summary>
    public interface IItemInspectionRegistry
    {
        bool WasInspected(string typeKey);

        void MarkInspected(string typeKey);

        /// <summary>
        /// Заполняет список всеми осмотренными типами (список предварительно очищается).
        /// </summary>
        void GetInspectedKeys(List<string> result);

        void ClearInspected();
    }
}
