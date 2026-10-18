using System.Collections.Generic;
using JuicyDI;
using UnityEngine;

namespace GBS
{
    /// <summary>
    /// Тонкая обёртка над контейнером JuicyDI для "мягкого" резолва зависимостей.
    ///
    /// Зачем: поля с [Inject] обязательны - если бина нет на сцене, JuicyDI пишет LogError.
    /// Сюжетным графам далеко не всегда нужны все директоры (камера, катсцены, фейдер),
    /// поэтому GBS резолвит их вручную и молча допускает отсутствие.
    /// </summary>
    public sealed class GBSDependencyResolver : Game.Scripts.Story.IStoryServiceResolver
    {
        private readonly IBinController m_Container;

        public GBSDependencyResolver()
        {
            m_Container = BinController.GetContext();

            if (m_Container == null)
            {
                Debug.LogWarning("[GBS] JuicyDI container is not initialized yet. " +
                                 "All story dependencies will be resolved as null.");
            }
        }

        public GBSDependencyResolver(IBinController container)
        {
            m_Container = container;
        }

        public bool HasContainer => m_Container != null;

        /// <summary>
        /// Возвращает бин или null, если его нет на сцене.
        /// </summary>
        /// <param name="isRequired">true - при отсутствии бина будет ошибка в консоли.</param>
        public T Resolve<T>(bool isRequired = false) where T : class
        {
            var bean = m_Container?.GetBean<T>();

            if (bean == null && isRequired)
            {
                Debug.LogError($"[GBS] Required dependency '{typeof(T).Name}' is not found in the scene.");
            }

            return bean;
        }

        T Game.Scripts.Story.IStoryServiceResolver.Resolve<T>()
        {
            return Resolve<T>();
        }

        /// <summary>
        /// Возвращает все бины контракта. Пустой список - валидный результат.
        /// </summary>
        public List<T> ResolveAll<T>() where T : class
        {
            return m_Container?.GetBeans<T>() ?? new List<T>();
        }
    }
}

