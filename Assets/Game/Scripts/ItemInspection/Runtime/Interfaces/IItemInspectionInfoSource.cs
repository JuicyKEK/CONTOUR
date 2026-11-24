namespace Game.Scripts.ItemInspection
{
    /// <summary>
    /// Компонент на том же объекте, что и InspectOnFirstPickup, который сам решает тип и подпись предмета по его
    /// данным (например, AudioTapeInspectionInfo: обычная кассета и злая кассета - разные типы).
    /// </summary>
    public interface IItemInspectionInfoSource
    {
        /// <summary>
        /// Тип предмета и подпись при осмотре. false - пусть InspectOnFirstPickup берёт свои поля.
        /// </summary>
        bool TryGetInspectionInfo(out string typeKey, out string displayName);
    }
}
