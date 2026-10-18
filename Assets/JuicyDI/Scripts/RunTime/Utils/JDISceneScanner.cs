using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JuicyDI
{
    /// <summary>
    /// Сбор MonoBehaviour конкретной сцены. Списки переиспользуются - на загрузке локации
    /// не создаём лишний мусор для GC.
    /// </summary>
    public static class JDISceneScanner
    {
        private static readonly List<MonoBehaviour> s_Behaviours = new List<MonoBehaviour>(256);
        private static readonly List<MonoBehaviour> s_RootBuffer = new List<MonoBehaviour>(64);
        private static readonly List<GameObject> s_RootObjects = new List<GameObject>(32);

        public static List<MonoBehaviour> CollectSceneBehaviours(Scene scene)
        {
            s_Behaviours.Clear();

            if (!scene.IsValid() || !scene.isLoaded)
            {
                return s_Behaviours;
            }

            s_RootObjects.Clear();
            scene.GetRootGameObjects(s_RootObjects);

            for (int i = 0; i < s_RootObjects.Count; i++)
            {
                s_RootBuffer.Clear();
                s_RootObjects[i].GetComponentsInChildren(true, s_RootBuffer);
                s_Behaviours.AddRange(s_RootBuffer);
            }

            s_RootBuffer.Clear();
            s_RootObjects.Clear();

            return s_Behaviours;
        }
    }
}

