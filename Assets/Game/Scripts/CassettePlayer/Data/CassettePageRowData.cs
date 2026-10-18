using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Данные одной страницы кассет для построения тогла страницы и её содержимого.
    /// </summary>
    public readonly struct CassettePageRowData
    {
        public readonly string PageName;
        public readonly Color PageColor;
        public readonly IReadOnlyList<CassetteSectionRowData> Sections;

        public CassettePageRowData(string pageName, Color pageColor, IReadOnlyList<CassetteSectionRowData> sections)
        {
            PageName = pageName;
            PageColor = pageColor;
            Sections = sections;
        }
    }
}
