using System.Collections.Generic;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.ItemInspection.Controllers
{
    /// <summary>
    /// Какие типы предметов игрок уже осматривал при подборе (<see cref="IItemInspectionRegistry"/>). Лежит в префабе
    /// игрока рядом с ItemInspectionController; в файл пишет и читает ItemInspectionSaveController.
    /// </summary>
    [JDIMonoController]
    public class ItemInspectionRegistry : MonoBehaviour, IItemInspectionRegistry
    {
        private readonly HashSet<string> m_InspectedTypes = new();

        public bool WasInspected(string typeKey)
        {
            return !string.IsNullOrEmpty(typeKey) && m_InspectedTypes.Contains(typeKey);
        }

        public void MarkInspected(string typeKey)
        {
            if (!string.IsNullOrEmpty(typeKey))
            {
                m_InspectedTypes.Add(typeKey);
            }
        }

        public void GetInspectedKeys(List<string> result)
        {
            result.Clear();
            result.AddRange(m_InspectedTypes);
        }

        public void ClearInspected()
        {
            m_InspectedTypes.Clear();
        }
    }
}
