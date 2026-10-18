using JuicyDI.Attributes;
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
    /// Устаревший способ: для новых шагов используйте StorySceneReactions (реакции по ключам,
    /// без SO-ассета на каждый шаг). Для графов GBS этот слушатель работает как реакция сцены
    /// с ключом "Legacy/&lt;имя ассета канала&gt;" - импортированный старый сюжет работает без
    /// переделки сцены.
    /// </summary>
    [JDIMonoController]
    public class StoryEventChannelListener : MonoBehaviour, IStorySceneReactionSource
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

        public int InvokeReaction(string key)
        {
            if (m_Channel == null || key != StorySignals.LegacyKey(m_Channel))
            {
                return 0;
            }

            HandleRaised();
            return 1;
        }

        private void HandleRaised()
        {
            m_OnRaised?.Invoke();
        }
    }
}
