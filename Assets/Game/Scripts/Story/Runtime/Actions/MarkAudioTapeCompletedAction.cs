using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Instructions.Data;
using Game.Scripts.Instructions.Interfaces;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// SO-версия действия "отметить задачу кассеты выполненной" (для старого StoryManager).
    /// В графах GBS используйте встроенное действие "Audio Tapes/Mark Tape Completed" - без SO-ассета;
    /// импортёр сам переносит этот ассет в него.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Actions/Mark Audio Tape Completed", fileName = "MarkAudioTapeCompletedAction")]
    public class MarkAudioTapeCompletedAction : StoryAction
    {
        [Tooltip("Кассета, задача из которой выполнена.")]
        [SerializeField] private AudioTapeDefinitionSO m_Tape;

        public AudioTapeDefinitionSO Tape => m_Tape;

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            if (m_Tape == null)
            {
                Debug.LogError($"[{nameof(MarkAudioTapeCompletedAction)}] {name}: не назначена кассета.", this);
                return UniTask.CompletedTask;
            }

            var registry = context.Resolve<IAudioTapeFoundRegistry>();

            if (registry == null)
            {
                Debug.LogWarning($"[{nameof(MarkAudioTapeCompletedAction)}] {name}: на сцене нет реестра кассет - " +
                                 $"'{m_Tape.TapeId}' не отмечена выполненной.", this);
                return UniTask.CompletedTask;
            }

            registry.MarkTapeCompleted(m_Tape.TapeId);
            return UniTask.CompletedTask;
        }
    }
}
