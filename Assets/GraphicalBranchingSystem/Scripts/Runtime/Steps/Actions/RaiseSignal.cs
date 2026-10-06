using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Поднимает сигнал сюжета - например, чтобы этот граф "разбудил" ветку другого графа
    /// (условие "Signal Raised" с тем же ключом).
    /// </summary>
    [Serializable, GBSMenu("Story State/Raise Signal")]
    public class RaiseSignal : GBSAction
    {
        [SerializeField, StoryKey(StoryKeyKind.Signal)] private string m_Key;

        public RaiseSignal()
        {
        }

        public RaiseSignal(string key)
        {
            m_Key = key;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            context.State.RaiseSignal(m_Key);
            return UniTask.CompletedTask;
        }
    }
}
