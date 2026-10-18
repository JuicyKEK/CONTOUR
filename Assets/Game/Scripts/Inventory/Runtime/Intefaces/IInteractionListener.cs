namespace Game.Scripts.Inventory
{
    /// <summary>
    /// Компонент на том же объекте, что и <see cref="IInteraction"/>: узнаёт о каждом взаимодействии
    /// игрока с объектом (сразу после Interact) - без UnityEvent и без правок самого объекта.
    /// Вызывает PlayerInteractiveController. Пример - StorySignalInteract (сигнал сюжету).
    /// </summary>
    public interface IInteractionListener
    {
        void OnInteracted(IInteraction interaction);
    }
}
