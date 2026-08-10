using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Data
{
    [Serializable]
    public struct NpcAnimationSoundEntry
    {
        [Tooltip("Логический ключ, по которому код запрашивает анимацию/звук (например 'Walk', 'Chase', 'Knock', 'Eat').")]
        public string Key;

        [Tooltip("Имя триггера/стейта Animator, который проигрывается по этому ключу.")]
        public string AnimatorTrigger;

        [Tooltip("Опциональный звук, проигрываемый одновременно с анимацией.")]
        public AudioClip Sound;

        [Tooltip("Зациклить звук (например для 'стука в дверь' или 'рычания' в погоне).")]
        public bool LoopSound;
    }

    /// <summary>
    /// Таблица "логический ключ -> анимация/звук" для одного типа бота. Позволяет
    /// менять набор анимаций/звуков конкретного NPC чисто данными, без правки кода
    /// состояний FSM (они запрашивают анимацию только по ключу, например "Chase").
    /// </summary>
    [CreateAssetMenu(menuName = "NPC/Animation Sound Profile", fileName = "NpcAnimationSoundProfile")]
    public class NpcAnimationSoundProfileSO : ScriptableObject
    {
        [SerializeField] private NpcAnimationSoundEntry[] m_Entries = Array.Empty<NpcAnimationSoundEntry>();

        private Dictionary<string, NpcAnimationSoundEntry> m_Lookup;

        private void EnsureLookup()
        {
            if (m_Lookup != null)
            {
                return;
            }

            m_Lookup = new Dictionary<string, NpcAnimationSoundEntry>(m_Entries.Length);

            foreach (var entry in m_Entries)
            {
                if (!string.IsNullOrEmpty(entry.Key))
                {
                    m_Lookup[entry.Key] = entry;
                }
            }
        }

        public bool TryGet(string key, out NpcAnimationSoundEntry entry)
        {
            EnsureLookup();
            return m_Lookup.TryGetValue(key, out entry);
        }
    }
}

