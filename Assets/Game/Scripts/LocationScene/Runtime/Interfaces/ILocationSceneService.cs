using Game.Scripts.LocationScene.Runtime.Data;
using R3;

namespace Game.Scripts.LocationScene.Runtime.Interfaces
{
    /// <summary>
    /// Загрузка локаций по мере движения игрока. Держит загруженными текущую локацию и все её
    /// переходные сцены; пока игрок в переходе - ещё и локацию на другом его конце вместе с её
    /// переходными сценами. Всё остальное выгружается. О движении игрока через переход сообщают
    /// триггеры на концах переходной сцены (LocationTransitionGate).
    /// </summary>
    public interface ILocationSceneService
    {
        /// <summary>
        /// Каталог локаций и переходов игры.
        /// </summary>
        LocationCatalogSO Catalog { get; }

        /// <summary>
        /// Локация, в которой находится игрок (null - игра ещё не выбрала стартовую локацию).
        /// </summary>
        ReadOnlyReactiveProperty<LocationDefinitionSO> CurrentLocation { get; }

        /// <summary>
        /// Соседняя локация, которая подгружена или подгружается, пока игрок в переходе к ней
        /// (null - игрок не в переходе).
        /// </summary>
        LocationDefinitionSO PendingLocation { get; }

        /// <summary>
        /// Загружена ли сцена локации и готова ли она к игре (её бины зарегистрированы и запущены).
        /// </summary>
        bool IsLocationLoaded(LocationDefinitionSO location);

        /// <summary>
        /// Локация загрузилась и готова к игре: в неё можно входить (например, открыть дверь в конце перехода).
        /// Тем, кто подписался позже, уже загруженные локации не приходят - их состояние даёт
        /// <see cref="IsLocationLoaded"/>. В сцене удобнее компонент LocationLoadEvents: он учитывает и то, и другое.
        /// </summary>
        Observable<LocationDefinitionSO> LocationLoaded { get; }

        /// <summary>
        /// Локация начала выгружаться: входить в неё больше нельзя (например, закрыть дверь в конце перехода).
        /// Приходит только для локаций, о загрузке которых сообщалось в <see cref="LocationLoaded"/>.
        /// </summary>
        Observable<LocationDefinitionSO> LocationUnloaded { get; }

        /// <summary>
        /// Игрок задел триггер на конце перехода со стороны локации <paramref name="gateLocation"/>.
        /// Если это текущая локация - игрок входит в переход: подгружается локация на другом конце.
        /// Если нет - игрок дошёл до выхода из перехода: эта локация становится текущей, прежняя выгружается.
        /// </summary>
        void PlayerTouchedGate(LocationTransitionDefinitionSO transition, LocationDefinitionSO gateLocation);

        /// <summary>
        /// Игрок вышел из триггера на конце перехода со стороны локации <paramref name="gateLocation"/>.
        /// </summary>
        /// <param name="isInLocation">
        /// true - игрок вышел в саму локацию (соседняя локация больше не нужна: её загрузка
        /// останавливается, а уже загруженная выгружается); false - игрок ушёл внутрь перехода.
        /// </param>
        void PlayerLeftGate(LocationTransitionDefinitionSO transition, LocationDefinitionSO gateLocation, bool isInLocation);
    }
}
