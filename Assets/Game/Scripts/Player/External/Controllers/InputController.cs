using System;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InputController
{
    [JDIMonoController]
    [SequenceParticipant(50)]
    public class InputController : MonoBehaviour, ISequence, IUpdateSequence, IInputActions, IInputSelectionActions
    {
        private Action m_PressingButtonF;
        private Action m_PressingButtonE;
        private Action m_PressingButtonR;
        private Action m_PressingMouseLeftButtonDown;
        private Action m_PressingMouseLeftButtonUp;
        private Action<float> m_ScrollMouse;
        private Action<int> m_PressKeyboardNumbersDown;

        public void MethodInit()
        {

        }

        public void MethodStart()
        {
            
        }

        public void CustomUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                m_PressingButtonF?.Invoke();
            }
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                m_PressingButtonE?.Invoke();
            } 
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                m_PressingButtonR?.Invoke();
            }
            
            if (Input.GetMouseButtonDown(0))
            {
                m_PressingMouseLeftButtonDown?.Invoke();
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                m_PressingMouseLeftButtonUp?.Invoke();
            }
            
            if (Input.mouseScrollDelta.y != 0)
            {
                m_ScrollMouse?.Invoke(Input.mouseScrollDelta.y);
            }

            IsPressKeyboardNumbers();
        }
        
        public void AddPressingButtonFAction(Action action)
        {
            m_PressingButtonF += action;
        }

        public void AddPressingButtonRAction(Action action)
        {
            m_PressingButtonR += action;
        }

        public void AddPressingButtonEAction(Action action)
        {
            m_PressingButtonE += action;
        }

        public void AddPressingMouseLeftButtonDownAction(Action action)
        {
            m_PressingMouseLeftButtonDown += action;
        }

        public void AddPressingMouseLeftButtonUpAction(Action action)
        {
            m_PressingMouseLeftButtonUp += action;
        }

        public void AddScrollMouseAction(Action<float> action)
        {
            m_ScrollMouse += action;
        }

        public void AddPressKeyboardNumbersAction(Action<int> action)
        {
            m_PressKeyboardNumbersDown += action;
        }

        private void IsPressKeyboardNumbers()
        {
            for (int i = 0; i < 5; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    m_PressKeyboardNumbersDown?.Invoke(i);
                }
            }
        }
    }
}