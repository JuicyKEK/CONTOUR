using System.Collections.Generic;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Доступ действий/условий сюжета к сервисам сцены (бинам JuicyDI) без расширения StoryContext
    /// под каждый новый сервис: <c>context.Resolve&lt;IMyService&gt;()</c>.
    /// </summary>
    public interface IStoryServiceResolver
    {
        /// <summary>
        /// Сервис или null, если его нет на сцене.
        /// </summary>
        T Resolve<T>() where T : class;

        /// <summary>
        /// Все сервисы контракта (пустой список, если их нет).
        /// </summary>
        List<T> ResolveAll<T>() where T : class;
    }
}
