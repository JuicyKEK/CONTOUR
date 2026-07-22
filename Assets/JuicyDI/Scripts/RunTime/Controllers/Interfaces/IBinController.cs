using UnityEngine;
using System.Collections.Generic;

namespace JuicyDI
{
    public interface IBinController
    {
        void InitBins(List<MonoBehaviour> monoBehaviours, bool isRegisterNonMonoBehavior = false);
        T ConstructorLateInjection<T>(params object[] runtimeArgs) where T : class;
    }
}