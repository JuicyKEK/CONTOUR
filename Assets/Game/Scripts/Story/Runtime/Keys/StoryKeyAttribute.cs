using System;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Вид ключа сюжета (для фильтрации выпадающего списка ключей).
    /// </summary>
    public enum StoryKeyKind
    {
        /// <summary>Любой ключ (фильтр не применяется).</summary>
        Any = 0,

        /// <summary>Событие мира/сюжета - счётчик срабатываний ("игрок вошёл в комнату").</summary>
        Signal = 1,

        /// <summary>Состояние да/нет ("дверь открыта", "игрок взял задание").</summary>
        Flag = 2,

        /// <summary>Реакция сцены на сюжет (UnityEvent в StorySceneReactions).</summary>
        Reaction = 3
    }

    /// <summary>
    /// Помечает строковое поле как ключ сюжета: в инспекторе и в нодах графа рядом с полем появляется
    /// выпадающий список ключей из каталога (StoryKeyCatalogSO) и предупреждение, если ключа там нет.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class StoryKeyAttribute : PropertyAttribute
    {
        public StoryKeyKind Kind { get; }

        public StoryKeyAttribute(StoryKeyKind kind = StoryKeyKind.Any)
        {
            Kind = kind;
        }
    }
}
