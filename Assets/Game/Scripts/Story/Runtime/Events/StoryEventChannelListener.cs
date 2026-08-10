using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Сценный "мост" от StoryEventChannelSO к конкретным методам сценных
    /// скриптов. Вешается на любой GameObject на сцене (например, рядом с
    /// NPCMainLocationSpawner), в инспекторе указывается канал (тот же ассет,
    /// что и в RaiseEventChannelAction в ноде сюжета) и настраивается
    /// m_OnRaised как обычный UnityEvent - можно перетащить сценный
    /// NPCMainLocationSpawner и выбрать его метод (например AddNpcToSpawnList),
    /// ровно так же, как если бы UnityEvent висел прямо на ноде сюжета.
    ///
    /// Это позволяет не менять StoryAction/StoryNodeSO/StoryManager: SO-граф
    /// продолжает работать только с ассетами, а связывание с конкретными
    /// сценными объектами происходит здесь, на уровне сцены.
    /// </summary>
    public class StoryEventChannelListener : MonoBehaviour
    {
        [SerializeField] private UnityEventAction m_Channel;
        [SerializeField] private UnityEvent m_OnRaised;

        private void OnEnable()
        {
            if (m_Channel == null)
            {
                Debug.LogWarning($"{nameof(StoryEventChannelListener)} on '{name}': channel is not assigned.", this);
                return;
            }

            m_Channel.Subscribe(HandleRaised);
        }

        private void OnDisable()
        {
            if (m_Channel == null)
            {
                return;
            }

            m_Channel.Unsubscribe(HandleRaised);
        }

        private void HandleRaised()
        {
            m_OnRaised?.Invoke();
        }
    }
}

