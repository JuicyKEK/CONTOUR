using System.Collections.Generic;
using UnityEngine;

namespace JuicyDI
{
    public interface IObjectFactory
    {
        void RegisterMonoBehaviorsBeans(List<MonoBehaviour> monoBehaviours);
        void InjectingBeans();
        void RemoveSceneContext();
        List<T> GetBeans<T>(string key);
        void LateInjectingBeans(object bean);
    }
}