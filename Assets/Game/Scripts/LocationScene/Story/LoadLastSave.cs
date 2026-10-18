using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Steps;
using Game.Scripts.LocationScene.Runtime.Interfaces;
using Game.Scripts.Story;
using UnityEngine;

namespace Game.Scripts.LocationScene.Story
{
    /// <summary>
    /// Действие графа GBS: возвращает игру к последнему сохранению (например, в ноде смерти игрока).
    /// Игра перезапускается с сохранённой локации и чекпоинта, сюжет и остальные механики заново читают
    /// свои сохранения - всё, что случилось после сохранения, теряется. Действия после этого в ноде
    /// не выполняются: граф останавливается вместе со сценой.
    /// </summary>
    [Serializable, GBSMenu("Save/Load Last Save")]
    public class LoadLastSave : GBSAction
    {
        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var checkpointService = context.Resolve<IPlayerCheckpointService>();

            if (checkpointService == null)
            {
                Debug.LogWarning("[GBS] Load Last Save: на сцене нет PlayerCheckpointController - игра не перезапущена.");
                return UniTask.CompletedTask;
            }

            checkpointService.ReturnToLastSave();
            return UniTask.CompletedTask;
        }
    }
}
