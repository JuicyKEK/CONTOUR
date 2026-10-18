using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuicyDI
{
    public interface IObjectFactory
    {
        /// <summary>
        /// Регистрирует бины. Повторная регистрация уже существующего глобального бина игнорируется.
        /// </summary>
        void RegisterMonoBehaviorsBeans(List<MonoBehaviour> monoBehaviours);

        /// <summary>
        /// Инжект тех бинов, которым он актуально нужен (новые глобальные + все бины загруженных сцен).
        /// </summary>
        void InjectingBeans();

        /// <summary>
        /// Точечное удаление скоупа выгруженной сцены.
        /// </summary>
        void RemoveSceneContext(int sceneHandle);

        /// <summary>
        /// Полный сброс контейнера.
        /// </summary>
        void Clear();

        void LateInjectingBeans(object bean);

        T GetBean<T>() where T : class;

        List<T> GetBeans<T>() where T : class;

        object GetBean(Type contract);
    }
}