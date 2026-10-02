using System;
using System.Collections.Generic;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Состояние сюжета - единственный источник правды о том, что уже произошло в мире.
    /// Хранит целые значения по строковым ключам (ключи выбираются из каталога, см. StoryKeyCatalogSO):
    ///  - флаг (StoryKeyKind.Flag) - 0/1: "дверь открыта", "игрок взял задание";
    ///  - сигнал (StoryKeyKind.Signal) - счётчик срабатываний: "игрок вошёл в комнату", "NPC поймал игрока".
    ///
    /// В отличие от SO-каналов событие не теряется, если сюжет ещё не начал его ждать: значение
    /// остаётся в состоянии и попадает в сейв. Для каждого ключа запоминается порядковый номер
    /// последнего изменения - по нему условие "сигнал после входа в ноду" отличает новые
    /// срабатывания от старых.
    /// </summary>
    public interface IStoryState
    {
        /// <summary>
        /// Порядковый номер последнего изменения любого ключа (растёт с каждым изменением).
        /// </summary>
        long Sequence { get; }

        /// <summary>
        /// Изменилось значение ключа (передаётся ключ).
        /// </summary>
        event Action<string> Changed;

        bool HasValue(string key);
        int GetValue(string key);
        bool GetFlag(string key);

        /// <summary>
        /// Номер изменения (<see cref="Sequence"/>), на котором ключ менялся последний раз; 0 - не менялся.
        /// </summary>
        long GetLastChangeSequence(string key);

        void SetValue(string key, int value);
        void SetFlag(string key, bool value);

        /// <summary>
        /// Сигнал: увеличивает счётчик ключа на 1 (всегда считается изменением).
        /// </summary>
        void RaiseSignal(string key);
    }

    /// <inheritdoc cref="IStoryState"/>
    public sealed class StoryState : IStoryState
    {
        private readonly Dictionary<string, int> m_Values = new();
        private readonly Dictionary<string, long> m_LastChange = new();

        public long Sequence { get; private set; }

        public event Action<string> Changed;

        public IEnumerable<KeyValuePair<string, int>> Values => m_Values;

        public bool HasValue(string key)
        {
            return !string.IsNullOrEmpty(key) && m_Values.ContainsKey(key);
        }

        public int GetValue(string key)
        {
            return !string.IsNullOrEmpty(key) && m_Values.TryGetValue(key, out int value) ? value : 0;
        }

        public bool GetFlag(string key)
        {
            return GetValue(key) != 0;
        }

        public long GetLastChangeSequence(string key)
        {
            return !string.IsNullOrEmpty(key) && m_LastChange.TryGetValue(key, out long sequence) ? sequence : 0;
        }

        public void SetValue(string key, int value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (m_Values.TryGetValue(key, out int current) && current == value)
            {
                return;
            }

            m_Values[key] = value;
            NotifyChanged(key);
        }

        public void SetFlag(string key, bool value)
        {
            SetValue(key, value ? 1 : 0);
        }

        public void RaiseSignal(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            m_Values[key] = GetValue(key) + 1;
            NotifyChanged(key);
        }

        /// <summary>
        /// Восстанавливает значения из сейва (без событий Changed - вызывается до запуска графов).
        /// </summary>
        public void Restore(IEnumerable<KeyValuePair<string, int>> values)
        {
            Clear();

            if (values == null)
            {
                return;
            }

            foreach (var pair in values)
            {
                if (!string.IsNullOrEmpty(pair.Key))
                {
                    m_Values[pair.Key] = pair.Value;
                }
            }
        }

        public void Clear()
        {
            m_Values.Clear();
            m_LastChange.Clear();
        }

        private void NotifyChanged(string key)
        {
            m_LastChange[key] = ++Sequence;
            Changed?.Invoke(key);
        }
    }
}
