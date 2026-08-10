using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Условие срабатывает спустя заданное время. Полезно как "таймаут игнора"
    /// в развилках (игрок не выполнил и не отказался от задания - через N секунд
    /// сюжет всё равно продолжается по ветке "проигнорировал").
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Conditions/Delay", fileName = "DelayCondition")]
    public class DelayCondition : StoryCondition
    {
        [SerializeField] private float m_Seconds = 1f;

        public override UniTask WaitAsync(StoryContext context, CancellationToken token)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(m_Seconds), cancellationToken: token);
        }
    }
}

