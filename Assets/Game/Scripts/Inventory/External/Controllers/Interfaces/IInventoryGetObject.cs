namespace Game.Scripts.Inventory.External.Controllers.Interfaces
{
    public interface IInventoryGetObject
    {
        void SelectedInventoryObject(int selectedIndex);
        IInventoryObject GetInventorySelectedObject();
        void DeleteSelectedInventoryObject();
    }
}