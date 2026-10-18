using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.InputController;
using UnityEngine;

namespace Game.Scripts.Story
{
    public enum StoryInputButton
    {
        F,
        E,
        R,
        Tab,
        MouseLeftDown,
        MouseLeftUp,
        ESC
    }

    /// <summary>
    /// Условие выполняется по нажатию конкретной кнопки игроком (через
    /// существующий IInputActions/InputController).
    /// Примечание: IInputActions не предоставляет отписку от Action,
    /// поэтому обработчик самоблокируется флагом isCompleted после первого срабатывания.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Conditions/Input Button Pressed", fileName = "InputButtonCondition")]
    public class InputButtonCondition : StoryCondition
    {
        [SerializeField] private StoryInputButton m_Button;

        public override UniTask WaitAsync(StoryContext context, CancellationToken token)
        {
            return StoryInput.WaitPressAsync(context.InputActions, m_Button, token);
        }
    }

    /// <summary>
    /// Ожидание нажатия кнопки через IInputActions (общий код для InputButtonCondition и условий графа GBS).
    /// Примечание: IInputActions не предоставляет отписку от Action,
    /// поэтому обработчик самоблокируется флагом isCompleted после первого срабатывания.
    /// </summary>
    public static class StoryInput
    {
        public static async UniTask WaitPressAsync(IInputActions inputActions, StoryInputButton button, CancellationToken token)
        {
            if (inputActions == null)
            {
                Debug.LogWarning("[Story] Ожидание кнопки: на сцене нет IInputActions.");
                await UniTask.Never(token);
                return;
            }

            var completionSource = new UniTaskCompletionSource();
            bool isCompleted = false;

            void Handler()
            {
                if (isCompleted)
                {
                    return;
                }

                isCompleted = true;
                completionSource.TrySetResult();
            }

            Subscribe(inputActions, button, Handler);

            using (token.Register(() =>
                   {
                       isCompleted = true;
                       completionSource.TrySetCanceled(token);
                   }))
            {
                await completionSource.Task;
            }
        }

        private static void Subscribe(IInputActions inputActions, StoryInputButton button, Action handler)
        {
            switch (button)
            {
                case StoryInputButton.F:
                    inputActions.AddPressingButtonFAction(handler);
                    break;
                case StoryInputButton.E:
                    inputActions.AddPressingButtonEAction(handler);
                    break;
                case StoryInputButton.R:
                    inputActions.AddPressingButtonRAction(handler);
                    break;
                case StoryInputButton.Tab:
                    inputActions.AddPressingButtonTabAction(handler);
                    break;
                case StoryInputButton.MouseLeftDown:
                    inputActions.AddPressingMouseLeftButtonDownAction(handler);
                    break;
                case StoryInputButton.MouseLeftUp:
                    inputActions.AddPressingMouseLeftButtonUpAction(handler);
                    break;
                case StoryInputButton.ESC:
                    inputActions.AddPressingMouseESCAction(handler);
                    break;
            }
        }
    }
}

