namespace Game.Scripts.InteractionObjects.Interfaces
{
    /// <summary>
    /// Кто именно открывает дверь - влияет только на то, какая анимация/звук
    /// откроют её во View (см. InteractionDoorView/DefaultDoorView). Например,
    /// мимик до разоблачения открывает дверь как обычный человек, а после
    /// деформации во время погони - как монстр (другая, более резкая анимация).
    /// </summary>
    public enum DoorOpenerKind
    {
        Human = 0,
        Monster = 1
    }
}

