using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JuicyDI
{
    // Awake должен отработать раньше всех: успеть запомнить сцену до того, как чей-то
    // DontDestroyOnLoad (например DontDestroer на корне) унесёт этот объект в DDOL-сцену.
    [DefaultExecutionOrder(-10000)]
    public class MainJDIController : MonoBehaviour
    {
        [SerializeField] private bool m_IsRegisterNonMonoBehavior = false;

        private IBinController m_BinController;

        // Сцена, в которой контроллер лежал изначально.
        private Scene m_HomeScene;

        private void Awake()
        {
            m_HomeScene = gameObject.scene;
        }

        public void Init()
        {
            var fastSearchNamespacesByClasses = new List<Type>()
            {
                typeof(MainJDIController),
            };

            // Контейнер в проекте один: при повторной загрузке сцены переиспользуем существующий,
            // иначе все уже зарегистрированные глобальные бины были бы потеряны.
            m_BinController = BinController.GetOrCreateContext(fastSearchNamespacesByClasses);

            m_BinController.InitBins(CollectBehaviours(), m_IsRegisterNonMonoBehavior);
        }

        /// <summary>
        /// Объекты своей сцены. Если контроллер унесли в DontDestroyOnLoad, gameObject.scene - уже
        /// DDOL-сцена: сканируем и её (там теперь он сам и его соседи), и сцену, где он лежал, -
        /// иначе вся локация (UI, игрок, зоны) осталась бы без регистрации.
        /// </summary>
        private List<MonoBehaviour> CollectBehaviours()
        {
            var currentScene = gameObject.scene;
            var behaviours = JDISceneScanner.CollectSceneBehaviours(currentScene);

            if (!m_HomeScene.IsValid() || m_HomeScene == currentScene)
            {
                return behaviours;
            }

            // Сканер переиспользует один список - копируем до второго вызова.
            var result = new List<MonoBehaviour>(behaviours);
            result.AddRange(JDISceneScanner.CollectSceneBehaviours(m_HomeScene));

            return result;
        }
    }
}
