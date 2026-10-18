using System;
using UnityEngine;
using System.Collections.Generic;

namespace JuicyDI
{
    public interface IBinController : IDisposable
    {
        void InitBins(List<MonoBehaviour> monoBehaviours, bool isRegisterNonMonoBehavior = false);
        T ConstructorLateInjection<T>(params object[] runtimeArgs) where T : class;

        /// <summary>Инжект в объект, созданный вне контейнера (например, через Instantiate).</summary>
        void LateInjection(object target);

        T GetBean<T>() where T : class;
        List<T> GetBeans<T>() where T : class;
    }
}