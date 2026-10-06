using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Steps;
using Game.Scripts.Instructions.Data;
using Game.Scripts.Instructions.Interfaces;
using Game.Scripts.Story;
using UnityEngine;

namespace Game.Scripts.Instructions.Story
{
    /// <summary>
    /// Действие графа GBS: помечает задачу аудио-кассеты выполненной - в кассетном плеере у неё
    /// появится метка "выполнено". Кладётся в действия ноды, в которой задача считается сделанной.
    /// Пример действия, которое берёт свой сервис через context.Resolve - без правок StoryContext.
    /// </summary>
    [Serializable, GBSMenu("Audio Tapes/Mark Tape Completed")]
    public class MarkAudioTapeCompleted : GBSAction
    {
        [Tooltip("Кассета, задача из которой выполнена.")]
        [SerializeField] private AudioTapeDefinitionSO m_Tape;

        public MarkAudioTapeCompleted()
        {
        }

        public MarkAudioTapeCompleted(AudioTapeDefinitionSO tape)
        {
            m_Tape = tape;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            if (m_Tape == null)
            {
                Debug.LogError("[GBS] Mark Tape Completed: не назначена кассета.");
                return UniTask.CompletedTask;
            }

            var registry = context.Resolve<IAudioTapeFoundRegistry>();

            if (registry == null)
            {
                Debug.LogWarning($"[GBS] Mark Tape Completed: на сцене нет реестра кассет - '{m_Tape.TapeId}' не отмечена выполненной.");
                return UniTask.CompletedTask;
            }

            registry.MarkTapeCompleted(m_Tape.TapeId);
            return UniTask.CompletedTask;
        }
    }
}
