using System;

namespace Game.Scripts.InputController
{
    public interface IInputSelectionActions
    {
        void AddScrollMouseAction(Action<float> action);
        void AddPressKeyboardNumbersAction(Action<int> action);
    }
}