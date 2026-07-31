using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Базовый класс действия, выполняемого при входе в ноду сюжета.
    /// Действия ноды выполняются последовательно и awaited, поэтому можно
    /// строить точные сценарии вида "забрать управление -> проиграть ролик ->
    /// перетечь камерой -> вернуть управление -> показать подсказку".
    /// </summary>
    public abstract class StoryAction : ScriptableObject
    {
        public abstract UniTask ExecuteAsync(StoryContext context, CancellationToken token);
    }
}

