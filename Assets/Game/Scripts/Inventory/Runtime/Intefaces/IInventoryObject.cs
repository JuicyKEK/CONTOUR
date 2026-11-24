namespace Game.Scripts.Inventory
{
    public interface IInventoryObject
    {
        string ObjectKey { get; }
        string IconAddress { get; }
        void InventoryObjectAction();
    }
}
