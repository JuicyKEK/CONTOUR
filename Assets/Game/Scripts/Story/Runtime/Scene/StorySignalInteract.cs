using Game.Scripts.Inventory;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Сообщает сюжету, что игрок взаимодействовал с объектом (нажал E на любом IInteraction: дверь,
    /// кассета, предмет, NPC). Вешается на тот же объект, что и IInteraction, - без UnityEvent и без
    /// правок самого объекта (его оповещает PlayerInteractiveController через IInteractionListener).
    ///
    /// Срабатывает на каждую попытку взаимодействия - в том числе с запертой дверью. Если сюжету нужен
    /// результат (дверь действительно открылась), используйте мост результата, например DoorStoryFlagBridge.
    /// Можно повесить несколько компонентов (например, сигнал + флаг).
    /// </summary>
    public class StorySignalInteract : MonoBehaviour, IInteractionListener
    {
        public enum StoryWrite
        {
            [Tooltip("Сигнал: счётчик +1 - событие \"игрок взаимодействовал\".")]
            RaiseSignal,

            [Tooltip("Флаг = true - факт \"игрок это сделал\".")]
            SetFlagTrue,

            [Tooltip("Флаг = false.")]
            SetFlagFalse
        }

        [Tooltip("Ключ сигнала или флага сюжета.")]
        [SerializeField, StoryKey] private string m_Key;

        [Tooltip("Что записать в состояние сюжета при взаимодействии.")]
        [SerializeField] private StoryWrite m_Write = StoryWrite.RaiseSignal;

        [Tooltip("Срабатывать только на первое взаимодействие (до перезагрузки сцены).")]
        [SerializeField] private bool m_Once;

        private bool m_IsTriggered;

        public void OnInteracted(IInteraction interaction)
        {
            if (m_Once && m_IsTriggered)
            {
                return;
            }

            var state = StorySignals.Current;

            if (state == null)
            {
                Debug.LogWarning($"[Story] StorySignalInteract '{name}': на сцене нет GBSStarter - '{m_Key}' не записан.", this);
                return;
            }

            if (string.IsNullOrEmpty(m_Key))
            {
                Debug.LogWarning($"[Story] StorySignalInteract '{name}': не задан ключ.", this);
                return;
            }

            m_IsTriggered = true;

            switch (m_Write)
            {
                case StoryWrite.RaiseSignal:
                    state.RaiseSignal(m_Key);
                    break;

                case StoryWrite.SetFlagTrue:
                    state.SetFlag(m_Key, true);
                    break;

                case StoryWrite.SetFlagFalse:
                    state.SetFlag(m_Key, false);
                    break;
            }
        }

        private void Reset()
        {
            if (GetComponent<IInteraction>() == null)
            {
                Debug.LogWarning($"[Story] StorySignalInteract '{name}': на объекте нет IInteraction - " +
                                 "повесьте компонент на объект, с которым взаимодействует игрок.", this);
            }
        }
    }
}
