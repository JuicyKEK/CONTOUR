using System.Collections.Generic;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.InfectionZone.Runtime.Controllers
{
    /// <summary>
    /// Текущая зона заражения игрока. Корректно обрабатывает:
    ///  - перекрывающиеся на стыке триггеры соседних зон и вложенные зоны - текущей считается
    ///    последняя зона, в которую игрок вошёл и из которой ещё не вышел (при выходе из неё
    ///    текущей снова становится предыдущая);
    ///  - зону из нескольких триггеров/коллайдеров - переход между ними не считается сменой зоны.
    /// </summary>
    [JDIMonoController]
    public class PlayerInfectionZoneTracker : MonoBehaviour, IPlayerInfectionZoneTracker
    {
        // Зоны, внутри которых сейчас игрок, в порядке входа (последняя - текущая).
        private readonly List<IInfectionZone> m_ZonesInside = new();
        // Сколько триггеров каждой зоны сейчас пересекает игрок.
        private readonly Dictionary<IInfectionZone, int> m_OverlapCounts = new();
        private readonly ReactiveProperty<IInfectionZone> m_CurrentZone = new(null);

        private bool m_IsDestroyed;

        public ReadOnlyReactiveProperty<IInfectionZone> CurrentZone => m_CurrentZone;

        public void EnterZone(IInfectionZone zone)
        {
            if (m_IsDestroyed || zone == null)
            {
                return;
            }

            m_OverlapCounts.TryGetValue(zone, out int overlapCount);
            m_OverlapCounts[zone] = overlapCount + 1;

            if (overlapCount == 0)
            {
                m_ZonesInside.Add(zone);
                UpdateCurrentZone();
            }
        }

        public void ExitZone(IInfectionZone zone)
        {
            if (m_IsDestroyed || zone == null || !m_OverlapCounts.TryGetValue(zone, out int overlapCount))
            {
                return;
            }

            if (overlapCount > 1)
            {
                m_OverlapCounts[zone] = overlapCount - 1;
                return;
            }

            m_OverlapCounts.Remove(zone);
            m_ZonesInside.Remove(zone);
            UpdateCurrentZone();
        }

        private void UpdateCurrentZone()
        {
            m_CurrentZone.Value = m_ZonesInside.Count > 0 ? m_ZonesInside[m_ZonesInside.Count - 1] : null;
        }

        private void OnDestroy()
        {
            // Триггеры могут сообщить о выходе уже после уничтожения трекера (выгрузка сцены) - игнорируем.
            m_IsDestroyed = true;
            m_CurrentZone.Dispose();
        }
    }
}
