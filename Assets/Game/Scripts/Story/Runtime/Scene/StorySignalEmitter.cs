using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Сообщает сюжету о событии мира по ключу - вызывается из UnityEvent любого объекта сцены
    /// (дверь открылась, кнопку нажали, предмет подобран). Заменяет связку "StoryEventChannelSO +
    /// вызов Raise из UnityEvent": SO-ассет на каждый сигнал больше не нужен.
    ///
    /// Raise() - сигнал (счётчик +1), SetFlag/SetFlagTrue/SetFlagFalse - флаг да/нет.
    /// </summary>
    [JDIMonoController]
    public class StorySignalEmitter : MonoBehaviour
    {
        [Tooltip("Ключ сигнала или флага сюжета.")]
        [SerializeField, StoryKey] private string m_Key;

        [Inject] private IStoryState m_State;

        public void Raise()
        {
            m_State?.RaiseSignal(m_Key);
        }

        public void SetFlag(bool value)
        {
            m_State?.SetFlag(m_Key, value);
        }

        public void SetFlagTrue()
        {
            SetFlag(true);
        }

        public void SetFlagFalse()
        {
            SetFlag(false);
        }
    }
}
