namespace GBS.Data
{
    /// <summary>
    /// Тип ноды графа сюжета.
    /// Flow-ноды (Start/Story/End) двигают сюжет, Bool-ноды (Condition/Logic/GraphCompleted)
    /// собирают логическое выражение, которое управляет переходами.
    /// </summary>
    public enum GBSNodeType
    {
        Start = 0,
        Story = 1,
        End = 2,
        Condition = 3,
        Logic = 4,
        GraphCompleted = 5
    }
}
