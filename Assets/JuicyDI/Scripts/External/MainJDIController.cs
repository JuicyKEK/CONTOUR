using System;
using System.Collections.Generic;
using UnityEngine;

namespace JuicyDI
{
    public class MainJDIController : MonoBehaviour
    {
        [SerializeField] private bool m_IsRegisterNonMonoBehavior = false;

        private IBinController m_BinController;

        public void Init()
        {
            var fastSearchNamespacesByClasses = new List<Type>()
            {
                typeof(MainJDIController),
            };

            // Контейнер в проекте один: при повторной загрузке сцены переиспользуем существующий,
            // иначе все уже зарегистрированные глобальные бины были бы потеряны.
            m_BinController = BinController.GetOrCreateContext(fastSearchNamespacesByClasses);

            m_BinController.InitBins(JDISceneScanner.CollectSceneBehaviours(gameObject.scene),
                m_IsRegisterNonMonoBehavior);
        }
    }
}
