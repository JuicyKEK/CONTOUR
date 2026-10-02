using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Показывает нижнюю текстовую подсказку игроку (IStoryHintView): на заданное время или, с галочкой
    /// Until Node Exit, пока сюжет не уйдёт с ноды (сработает переход).
    /// </summary>
    [Serializable, GBSMenu("UI/Show Hint")]
    public class ShowHint : GBSAction
    {
        [SerializeField, TextArea(2, 5)] private string m_Text;

        [Tooltip("Держать подсказку, пока сюжет не уйдёт с этой ноды (сработает переход). " +
                 "Duration и Wait Until Hidden тогда не используются. В End-ноде подсказка исчезнет сразу - " +
                 "там показывайте её по времени.")]
        [SerializeField] private bool m_UntilNodeExit;

        [Tooltip("Сколько секунд показывать подсказку.")]
        [SerializeField, Min(0f)] private float m_Duration = 4f;

        [Tooltip("Не выполнять следующие действия ноды, пока подсказка не скроется (по Duration). " +
                 "Подсказку это НЕ удерживает - для этого галочка Until Node Exit.")]
        [SerializeField] private bool m_WaitUntilHidden;

        public ShowHint()
        {
        }

        public ShowHint(string text, float duration, bool waitUntilHidden)
        {
            m_Text = text;
            m_Duration = duration;
            m_WaitUntilHidden = waitUntilHidden;
        }

        public override UniTask ExecuteAsync(GBSActionContext context, CancellationToken token)
        {
            return ShowAsync(context.Story, token, context.NodeExitToken);
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            // Вне графа (без информации о ноде) "до выхода с ноды" = до остановки сюжета.
            return ShowAsync(context, token, token);
        }

        private async UniTask ShowAsync(StoryContext context, CancellationToken token, CancellationToken nodeExitToken)
        {
            var hintView = context.HintView;

            if (hintView == null)
            {
                Debug.LogWarning("[GBS] Show Hint: на сцене нет IStoryHintView (префаб StoryHintView под Canvas).");
                return;
            }

            if (m_UntilNodeExit)
            {
                // Не ждём: иначе нода не дошла бы до своих переходов и никогда бы не завершилась.
                hintView.ShowAsync(m_Text, float.PositiveInfinity, nodeExitToken).Forget();
                return;
            }

            var task = hintView.ShowAsync(m_Text, m_Duration, token);

            if (m_WaitUntilHidden)
            {
                await task;
            }
            else
            {
                task.Forget();
            }
        }
    }
}
