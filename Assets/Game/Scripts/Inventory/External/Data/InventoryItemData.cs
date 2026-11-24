namespace Game.Scripts.Inventory.External.Data
{
    /// <summary>
    /// Предмет в инвентаре: ключ предмета и ключ его иконки в Addressables. Создаётся при подборе (InventoryPickup)
    /// и при загрузке сохранения. Объекта на сцене у него нет; иконку по ключу грузит вид инвентаря.
    /// </summary>
    public class InventoryItemData : IInventoryObject
    {
        public string ObjectKey { get; }
        public string IconAddress { get; }

        public InventoryItemData(string objectKey, string iconAddress)
        {
            ObjectKey = objectKey;
            IconAddress = iconAddress;
        }

        public void InventoryObjectAction()
        {
        }
    }
}
