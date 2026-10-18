public interface IInteractionDoorView
{
    public void ChangeState(bool state);

    /// <summary>
    /// Створка ещё поворачивается - открывается или закрывается (дрожание запертой двери не в счёт).
    /// </summary>
    public bool IsMoving { get; }
}
