using System.Collections.Generic;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Данные одного раздела списка кассет для построения списка.
    /// </summary>
    public readonly struct CassetteSectionRowData
    {
        public readonly string DisplayName;
        public readonly IReadOnlyList<CassetteTapeRowData> Tapes;

        public CassetteSectionRowData(string displayName, IReadOnlyList<CassetteTapeRowData> tapes)
        {
            DisplayName = displayName;
            Tapes = tapes;
        }
    }
}