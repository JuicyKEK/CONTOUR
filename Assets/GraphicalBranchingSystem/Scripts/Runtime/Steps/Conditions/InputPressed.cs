using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Срабатывает, когда игрок нажал кнопку (после начала ожидания).
    /// </summary>
    [Serializable, GBSMenu("Input/Button Pressed")]
    public class InputPressed : GBSCondition
    {
        [SerializeField] private StoryInputButton m_Button;

        public InputPressed()
        {
        }

        public InputPressed(StoryInputButton button)
        {
            m_Button = button;
        }

        public override UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            return StoryInput.WaitPressAsync(context.Story.InputActions, m_Button, token);
        }
    }
}
