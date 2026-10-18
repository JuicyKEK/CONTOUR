using System;
using System.Collections.Generic;
using JuicyDI.Attributes;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Источник реакций сцены на сюжет: действие графа "Invoke Scene Reaction" с ключом вызывает
    /// все реакции с этим ключом во всех источниках на загруженных сценах.
    /// </summary>
    public interface IStorySceneReactionSource
    {
        /// <summary>
        /// Вызывает реакции с ключом, возвращает их количество (0 - у источника такой реакции нет).
        /// </summary>
        int InvokeReaction(string key);
    }

    /// <summary>
    /// Реакции сцены на сюжет: список "ключ -> UnityEvent". Заменяет пару "SO UnityEventAction +
    /// StoryEventChannelListener" на каждый шаг сюжета: один такой компонент может держать все реакции
    /// главы/локации, ключи выбираются из каталога. В UnityEvent - что угодно на сцене: включить объекты,
    /// забрать управление, скомандовать NPC/монстрам (ForceChase, SetPatrolRoute...), проиграть анимацию.
    /// </summary>
    [JDIMonoController]
    public class StorySceneReactions : MonoBehaviour, IStorySceneReactionSource
    {
        [Serializable]
        public class Reaction
        {
            [StoryKey(StoryKeyKind.Reaction)] public string Key;
            public UnityEvent Response = new();
        }

        [SerializeField] private List<Reaction> m_Reactions = new();

        public int InvokeReaction(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return 0;
            }

            int invokedCount = 0;

            for (int i = 0; i < m_Reactions.Count; i++)
            {
                var reaction = m_Reactions[i];

                if (reaction == null || reaction.Key != key)
                {
                    continue;
                }

                reaction.Response?.Invoke();
                invokedCount++;
            }

            return invokedCount;
        }
    }
}
