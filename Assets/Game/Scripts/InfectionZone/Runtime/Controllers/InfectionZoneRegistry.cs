using System;
using System.Collections.Generic;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InfectionZone.Runtime.Controllers
{
    /// <summary>
    /// Глобальный реестр зон заражения. Регистрируется как JuicyDI бин (глобальный контекст),
    /// чтобы сюжетные события/квестовые системы могли получить доступ к любой зоне по её Id
    /// через [Inject] без прямых ссылок на сцене.
    /// </summary>
    [JDIMonoController(Context = typeof(JuicyDI.Context.GlobalBean))]
    public class InfectionZoneRegistry : MonoBehaviour, IInfectionZoneRegistry
    {
        private readonly Dictionary<string, IInfectionZone> m_ZonesById = new();

        public IReadOnlyCollection<IInfectionZone> Zones => m_ZonesById.Values;

        public event Action<IInfectionZone> ZoneRegistered;
        public event Action<IInfectionZone> ZoneUnregistering;

        public IInfectionZone GetZone(string zoneId)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                return null;
            }

            m_ZonesById.TryGetValue(zoneId, out var zone);
            return zone;
        }

        public void RegisterZone(IInfectionZone zone)
        {
            if (zone == null || string.IsNullOrEmpty(zone.ZoneId))
            {
                return;
            }

            if (m_ZonesById.ContainsKey(zone.ZoneId))
            {
                Debug.LogWarning($"[InfectionZoneRegistry] Зона с Id '{zone.ZoneId}' уже зарегистрирована. Проверьте уникальность ZoneId на сцене.");
                return;
            }

            m_ZonesById.Add(zone.ZoneId, zone);
            ZoneRegistered?.Invoke(zone);
        }

        public void UnregisterZone(IInfectionZone zone)
        {
            if (zone == null || string.IsNullOrEmpty(zone.ZoneId))
            {
                return;
            }

            // Снимаем с учёта только ту зону, что зарегистрирована. Зона из сцены, которая догрузилась, но не
            // запускалась (переход отменили), в реестр не попадала - её уничтожение не должно убрать зону с тем же Id.
            if (!m_ZonesById.TryGetValue(zone.ZoneId, out var registeredZone) || !ReferenceEquals(registeredZone, zone))
            {
                return;
            }

            ZoneUnregistering?.Invoke(zone);
            m_ZonesById.Remove(zone.ZoneId);
        }
    }
}
