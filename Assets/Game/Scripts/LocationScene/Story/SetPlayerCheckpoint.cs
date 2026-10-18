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
    /// Действие графа GBS: назначает чекпоинт игрока - точку (LocationCheckpoint), в которой он появится
    /// при загрузке игры или после смерти. Чекпоинт может стоять и на локации, которая сейчас не
    /// загружена; локация чекпоинта определяется сама. Само действие в файл ничего не пишет: чекпоинт
    /// попадает в сохранение при сохранении игры, поэтому в ноде-чекпоинте ставьте это действие перед
    /// "Save/Save Game".
    /// </summary>
    [Serializable, GBSMenu("Save/Set Player Checkpoint")]
    public class SetPlayerCheckpoint : GBSAction
    {
        [Tooltip("Ключ чекпоинта - тот же, что у LocationCheckpoint в сцене локации (сцена может быть не загружена).")]
        [SerializeField, StoryKey(StoryKeyKind.Checkpoint)] private string m_CheckpointId;

        public SetPlayerCheckpoint()
        {
        }

        public SetPlayerCheckpoint(string checkpointId)
        {
            m_CheckpointId = checkpointId;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var checkpointService = context.Resolve<IPlayerCheckpointService>();

            if (checkpointService == null)
            {
                Debug.LogWarning($"[GBS] Set Player Checkpoint: на сцене нет PlayerCheckpointController - чекпоинт '{m_CheckpointId}' не назначен.");
                return UniTask.CompletedTask;
            }

            checkpointService.SetCheckpoint(m_CheckpointId);
            return UniTask.CompletedTask;
        }
    }
}
