public interface IInteractionDoorView
{
    public void ChangeState(bool state);

    /// <summary>
    /// Створка ещё поворачивается - открывается или закрывается (дрожание запертой двери не в счёт).
    /// </summary>
    public bool IsMoving { get; }

    /// <summary>
    /// Открыть ли створку в обратную сторону, чтобы при открытии с разбега она ушла от того, кто стоит в этой точке
    /// (а не пролетела сквозь него). Спрашивается у закрытой двери перед открытием.
    /// </summary>
    public bool ShouldOpenReversed(UnityEngine.Vector3 openerPosition);
}
