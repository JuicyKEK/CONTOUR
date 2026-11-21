using System;

namespace Game.Scripts.InputController
{
    /// <summary>
    /// Компонент на объекте взаимодействия, который может показать что-то до самого взаимодействия - например, осмотр
    /// предмета при первом подборе (InspectOnFirstPickup). PlayerInteractiveController спрашивает его перед
    /// IInteraction.Interact(): вернул true - взаимодействие (и оповещение слушателей) выполнится, когда он вызовет
    /// proceed; false - взаимодействие выполняется сразу, как обычно.
    /// </summary>
    public interface IInteractionInterceptor
    {
        bool TryIntercept(Action proceed);
    }
}
