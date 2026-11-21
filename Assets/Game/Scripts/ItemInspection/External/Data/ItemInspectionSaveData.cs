using System;
using System.Collections.Generic;

namespace Game.Scripts.ItemInspection.Data
{
    /// <summary>
    /// Файл сохранения осмотров (item_inspection.json): типы предметов, которые игрок уже осматривал при подборе.
    /// </summary>
    [Serializable]
    public class ItemInspectionSaveData
    {
        public int Version = 1;
        public List<string> InspectedTypes = new();
    }
}
