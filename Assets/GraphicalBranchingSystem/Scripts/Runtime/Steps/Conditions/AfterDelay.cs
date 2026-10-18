using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Срабатывает через N секунд после начала ожидания - например "таймаут игнора" в развилке.
    /// </summary>
    [Serializable, GBSMenu("Time/After Delay")]
    public class AfterDelay : GBSCondition
    {
        [SerializeField, Min(0f)] private float m_Seconds = 1f;

        public AfterDelay()
        {
        }

        public AfterDelay(float seconds)
        {
            m_Seconds = seconds;
        }

        public override UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(m_Seconds), cancellationToken: token);
        }
    }
}
