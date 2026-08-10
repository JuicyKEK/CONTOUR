namespace Game.Scripts.Inventory
{
    public interface IInventoryObjectData
    {
        IInventoryObject Object { get; }
		int ObjectAmount { get; }
    }
}