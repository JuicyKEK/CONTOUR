using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Базовый класс условия перехода в ноду сюжета.
    /// Конкретные условия - маленькие переиспользуемые SO-ассеты
    /// (нажатие кнопки, таймер, внешнее событие, композиция AllOf/AnyOf и т.д.),
    /// которые дизайнер комбинирует в инспекторе, не создавая новый C#-класс
    /// под каждую сюжетную ноду.
    /// </summary>
    public abstract class StoryCondition : ScriptableObject
    {
        public abstract UniTask WaitAsync(StoryContext context, CancellationToken token);
    }
}

