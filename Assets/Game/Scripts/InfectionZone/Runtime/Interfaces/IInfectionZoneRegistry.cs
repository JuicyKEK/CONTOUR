using System.Collections.Generic;

namespace Game.Scripts.InfectionZone.Runtime.Interfaces
{
    /// <summary>
    /// Глобальный реестр всех зон заражения на сцене.
    /// Используется сюжетными событиями/квестовыми системами, чтобы найти нужную зону по Id
    /// и изменить её степень заражения, не имея прямой ссылки на объект зоны на сцене.
    /// </summary>
    public interface IInfectionZoneRegistry
    {
        /// <summary>
        /// Все зарегистрированные сейчас зоны (используется сохранением).
        /// </summary>
        IReadOnlyCollection<IInfectionZone> Zones { get; }

        /// <summary>
        /// Найти зону по её идентификатору. Возвращает null, если зона с таким Id не найдена.
        /// </summary>
        IInfectionZone GetZone(string zoneId);

        /// <summary>
        /// Зарегистрировать зону в реестре (вызывается самой зоной при инициализации).
        /// </summary>
        void RegisterZone(IInfectionZone zone);

        /// <summary>
        /// Убрать зону из реестра (вызывается при уничтожении зоны).
        /// </summary>
        void UnregisterZone(IInfectionZone zone);
    }
}
