namespace Game.Scripts.InteractionHints
{
    /// <summary>
    /// Условие подсказки при взаимодействии: компонент на том же объекте, что и InteractionHint. Подсказка показывается,
    /// только если все условия разрешают (например, LockedDoorHintCondition - пока дверь заперта).
    /// </summary>
    public interface IInteractionHintCondition
    {
        bool CanShowHint();
    }
}
