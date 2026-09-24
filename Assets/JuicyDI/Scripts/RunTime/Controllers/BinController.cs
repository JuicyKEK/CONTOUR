using System.Collections.Generic;
using UnityEngine;
using System;
using JuicyDI.Scripts.RunTime.Factory;
using UnityEngine.SceneManagement;

namespace JuicyDI
{
    public class BinController : IBinController
    {
        private static IBinController m_CurrentContext;

        private readonly List<Type> m_FastSearchNamespacesByClasses;
        private readonly IObjectFactory m_ObjectFactory = new JDIObjectFactory();
        private readonly ILateInjectionFactory m_LateInjectionConstruction = new LateInjectionFactory();

        private bool m_IsSubscribed;

        public BinController(List<Type> fastSearchNamespacesByClasses = null)
        {
            m_FastSearchNamespacesByClasses = fastSearchNamespacesByClasses;
            RegisterCurrentContext();
            SubscribeSceneEvents();
        }

        /// <summary>
        /// Домен может не перезагружаться (Enter Play Mode Options) - статику надо чистить руками.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticContext()
        {
            (m_CurrentContext as BinController)?.Dispose();
            m_CurrentContext = null;
        }

        public static IBinController GetContext()
        {
            return m_CurrentContext;
        }

        /// <summary>
        /// Возвращает существующий контейнер или создаёт новый. Контейнер в проекте всегда один.
        /// </summary>
        public static IBinController GetOrCreateContext(List<Type> fastSearchNamespacesByClasses = null)
        {
            return m_CurrentContext ?? new BinController(fastSearchNamespacesByClasses);
        }

        public void InitBins(List<MonoBehaviour> monoBehaviours, bool isRegisterNonMonoBehavior = false)
        {
            RegisterCurrentContext();
            SubscribeSceneEvents();

            if (isRegisterNonMonoBehavior)
            {
                RegisterNonMonoBeans();
            }

            // Никакого тотального сброса скоупа сцен: чужие аддитивные локации не трогаем.
            // Уже зарегистрированные глобальные бины повторно не регистрируются.
            RegisterMonoBehaviorsBeans(monoBehaviours);
            InjectingBeans();
        }

        public T ConstructorLateInjection<T>(params object[] runtimeArgs) where T : class
        {
            var newObject = (T)m_LateInjectionConstruction.ConstructorLateInjection(typeof(T), runtimeArgs);
            m_ObjectFactory.LateInjectingBeans(newObject);
            return newObject;
        }

        public void LateInjection(object target)
        {
            m_ObjectFactory.LateInjectingBeans(target);
        }

        public T GetBean<T>() where T : class
        {
            return m_ObjectFactory.GetBean<T>();
        }

        public List<T> GetBeans<T>() where T : class
        {
            return m_ObjectFactory.GetBeans<T>();
        }

        public void Dispose()
        {
            if (m_IsSubscribed)
            {
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                m_IsSubscribed = false;
            }

            m_ObjectFactory.Clear();
        }

        private void SubscribeSceneEvents()
        {
            if (m_IsSubscribed)
            {
                return;
            }

            SceneManager.sceneUnloaded += OnSceneUnloaded;
            m_IsSubscribed = true;
        }

        /// <summary>
        /// Выгрузилась сцена (в т.ч. аддитивная локация): убираем ровно её бины
        /// и переинжектим оставшиеся, чтобы нигде не остались ссылки на уничтоженные объекты.
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            m_ObjectFactory.RemoveSceneContext(scene.handle);
            InjectingBeans();
        }

        private void InjectingBeans()
        {
            m_ObjectFactory.InjectingBeans();
        }

        private void RegisterCurrentContext()
        {
            if (m_CurrentContext == null)
            {
                m_CurrentContext = this;
            }
        }

        private void RegisterMonoBehaviorsBeans(List<MonoBehaviour> monoBehaviours)
        {
            m_ObjectFactory.RegisterMonoBehaviorsBeans(monoBehaviours);
        }

        private void RegisterNonMonoBeans()
        {
            //ComingSoon
        }
    }
}