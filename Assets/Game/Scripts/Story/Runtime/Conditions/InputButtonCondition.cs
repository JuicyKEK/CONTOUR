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

        public override async UniTask WaitAsync(StoryContext context, CancellationToken token)
        {
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

            Subscribe(context.InputActions, Handler);

            using (token.Register(() =>
                   {
                       isCompleted = true;
                       completionSource.TrySetCanceled(token);
                   }))
            {
                await completionSource.Task;
            }
        }

        private void Subscribe(IInputActions inputActions, Action handler)
        {
            switch (m_Button)
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

