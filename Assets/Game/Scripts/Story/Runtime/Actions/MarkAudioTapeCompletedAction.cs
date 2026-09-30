using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Instructions.Data;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Помечает задачу аудио-кассеты выполненной - в кассетном плеере у неё появится метка
    /// "выполнено". Кладётся в On Enter Actions ноды сюжета, в которой задача считается сделанной.
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Actions/Mark Audio Tape Completed", fileName = "MarkAudioTapeCompletedAction")]
    public class MarkAudioTapeCompletedAction : StoryAction
    {
        [Tooltip("Кассета, задача из которой выполнена.")]
        [SerializeField] private AudioTapeDefinitionSO m_Tape;

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            if (m_Tape == null)
            {
                Debug.LogError($"[{nameof(MarkAudioTapeCompletedAction)}] {name}: не назначена кассета.", this);
                return UniTask.CompletedTask;
            }

            if (context.AudioTapes == null)
            {
                Debug.LogWarning($"[{nameof(MarkAudioTapeCompletedAction)}] {name}: в StoryContext нет реестра кассет - " +
                                 $"'{m_Tape.TapeId}' не отмечена выполненной.", this);
                return UniTask.CompletedTask;
            }

            context.AudioTapes.MarkTapeCompleted(m_Tape.TapeId);
            return UniTask.CompletedTask;
        }
    }
}
