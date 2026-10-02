using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;

namespace GBS.Steps
{
    /// <summary>
    /// Действие ноды графа. Хранится прямо внутри ноды ([SerializeReference]) - параметры
    /// (текст подсказки, ключ сигнала, длительность) живут в графе, отдельный SO-ассет
    /// на каждое действие не нужен. Действия ноды выполняются строго по очереди с ожиданием.
    ///
    /// Новое действие = новый [Serializable] класс-наследник: он сам появится в меню "Add Action"
    /// ноды (путь в меню задаётся атрибутом <see cref="GBSMenuAttribute"/>). Сервисы сцены -
    /// через context.Resolve&lt;T&gt;(), без правок StoryContext.
    /// </summary>
    [Serializable]
    public abstract class GBSAction
    {
        public abstract UniTask ExecuteAsync(StoryContext context, CancellationToken token);

        /// <summary>
        /// Так действие вызывает раннер графа. Переопределите, если действию нужно знать, когда сюжет
        /// уходит с ноды (<see cref="GBSActionContext.NodeExitToken"/>) - например подсказка "пока не
        /// сработает переход". По умолчанию - обычный <see cref="ExecuteAsync(StoryContext, CancellationToken)"/>.
        /// </summary>
        public virtual UniTask ExecuteAsync(GBSActionContext context, CancellationToken token)
        {
            return ExecuteAsync(context.Story, token);
        }
    }

    /// <summary>
    /// Контекст выполнения действия ноды.
    /// </summary>
    public readonly struct GBSActionContext
    {
        public readonly StoryContext Story;

        /// <summary>
        /// Отменяется, когда сюжет уходит с ноды: сработал переход (для End-ноды - граф завершён)
        /// или граф остановлен.
        /// </summary>
        public readonly CancellationToken NodeExitToken;

        public GBSActionContext(StoryContext story, CancellationToken nodeExitToken)
        {
            Story = story;
            NodeExitToken = nodeExitToken;
        }
    }

    /// <summary>
    /// Путь типа в меню выбора действий/условий ноды (например "Story State/Raise Signal").
    /// Без атрибута тип попадает в корень меню под своим именем.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class GBSMenuAttribute : Attribute
    {
        public string Path { get; }

        public GBSMenuAttribute(string path)
        {
            Path = path;
        }
    }
}
