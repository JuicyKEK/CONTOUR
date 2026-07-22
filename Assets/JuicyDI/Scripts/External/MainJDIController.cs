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
            
            m_BinController = new BinController(fastSearchNamespacesByClasses);
            
            List<MonoBehaviour> allSceneObjects = new List<MonoBehaviour>();
            foreach (var rootGameObject in gameObject.scene.GetRootGameObjects())
            {
                allSceneObjects.AddRange(
                    rootGameObject.GetComponentsInChildren<MonoBehaviour>(true));
            }

            m_BinController.InitBins(allSceneObjects, m_IsRegisterNonMonoBehavior);
        }
    }
}
