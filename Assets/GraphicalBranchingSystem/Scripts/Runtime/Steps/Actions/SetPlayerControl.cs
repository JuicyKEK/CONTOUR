using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Включает/выключает управление игрока (все IPlayerControlHandle на сцене).
    /// </summary>
    [Serializable, GBSMenu("Player/Set Player Control")]
    public class SetPlayerControl : GBSAction
    {
        [SerializeField] private bool m_IsEnabled = true;

        public SetPlayerControl()
        {
        }

        public SetPlayerControl(bool isEnabled)
        {
            m_IsEnabled = isEnabled;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            context.SetControlEnabled(m_IsEnabled);
            return UniTask.CompletedTask;
        }
    }
}
