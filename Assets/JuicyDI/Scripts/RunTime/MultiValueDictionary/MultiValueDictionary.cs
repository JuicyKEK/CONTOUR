using System.Collections.Generic;
using System.Linq;

namespace JuicyDI.Utils
{
    public class MultiValueDictionary<TKey, TValue>
    {
        private readonly Dictionary<TKey, List<TValue>> m_Dictionary = new();

        public List<TValue> this[TKey key]
        {
            get
            {
                return Values(key);
            }
            set
            {
                Add(key, value);
            }
        }
        
        public void Add(TKey key, TValue value)
        {
            if (!m_Dictionary.ContainsKey(key))
            {
                m_Dictionary[key] = new List<TValue>();
            }
        
            m_Dictionary[key].Add(value);
        }
        
        public void Add(TKey key, List<TValue> value)
        {
            if (!m_Dictionary.ContainsKey(key))
            {
                m_Dictionary[key] = new List<TValue>();
            }
        
            m_Dictionary[key].AddRange(value);
        }

        public List<TValue> Values(TKey key)
        {
            return m_Dictionary.TryGetValue(key, out List<TValue> values) ? values : null;
        }
        
        public TValue Value(TKey key)
        {
            var listValues = m_Dictionary.TryGetValue(key, out List<TValue> values) ? values : null;
            
            return listValues != null && listValues.Count > 0 ? listValues[0] : default(TValue);
        }

        public List<TKey> GetKeys()
        {
            return m_Dictionary.Keys.ToList();
        }

        public void RemoveKey(TKey key)
        {
            m_Dictionary.Remove(key);
        }
        
        public void RemoveValue(TKey key, TValue value)
        {
            m_Dictionary[key].Remove(value);
        }
        
        public void RemoveValues(TKey key, List<TValue> value)
        {
            List<TValue> newValues = m_Dictionary[key].Except(value).ToList();
            RemoveKey(key);
            Add(key, newValues);
        }
        
        public void Clear()
        {
            m_Dictionary.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return m_Dictionary.ContainsKey(key);
        }
    }
}