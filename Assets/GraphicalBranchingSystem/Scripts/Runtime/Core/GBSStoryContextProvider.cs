using Game.Scripts.Story;
using UnityEngine;

namespace GBS
{
    /// <summary>
    /// Точка расширения: как именно собирается StoryContext для GBSStarter.
    ///
    /// Проект может добавить свой компонент-наследник рядом с GBSStarter
    /// (или подменить его в рантайме через GBSStarter.SetContextProvider),
    /// чтобы собрать контекст из других зависимостей / моков / заглушек.
    /// </summary>
    public abstract class GBSStoryContextProvider : MonoBehaviour
    {
        public abstract StoryContext CreateContext(GBSDependencyResolver resolver);
    }
}

