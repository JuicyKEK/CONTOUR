using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Действие ноды, которое просто "стреляет" в StoryEventChannelSO.
    /// Используется, когда вход в ноду должен запустить какой-то процесс
    /// на сцене (например, расширить список спавна NPC), но сама нода/действие
    /// не может и не должна хранить прямую ссылку на сценные объекты.
    ///
    /// Схема использования:
    ///  1) Создать ассет StoryEventChannelSO (Create -> Story/Events/Story Event Channel).
    ///  2) Добавить в ноду это действие и указать в нём этот же ассет-канал.
    ///  3) На сцене повесить StoryEventChannelListener, указать тот же канал
    ///     и настроить его UnityEvent на нужные методы сценных скриптов
    ///     (например NPCMainLocationSpawner.AddNpcToSpawnList).
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Actions/Raise Event Channel", fileName = "RaiseEventChannelAction")]
    public class RaiseEventChannelAction : StoryAction
    {
        [SerializeField] private StoryEventChannelSO m_Channel;

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            if (m_Channel == null)
            {
                Debug.LogWarning($"{nameof(RaiseEventChannelAction)}: channel is not assigned on '{name}'.");
                return UniTask.CompletedTask;
            }

            m_Channel.Raise();
            return UniTask.CompletedTask;
        }
    }
}

