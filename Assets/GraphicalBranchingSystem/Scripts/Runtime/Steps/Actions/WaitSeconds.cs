using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Пауза между действиями ноды (например: показать подсказку -> подождать -> включить свет).
    /// </summary>
    [Serializable, GBSMenu("Flow/Wait Seconds")]
    public class WaitSeconds : GBSAction
    {
        [SerializeField, Min(0f)] private float m_Seconds = 1f;

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(m_Seconds), cancellationToken: token);
        }
    }
}
