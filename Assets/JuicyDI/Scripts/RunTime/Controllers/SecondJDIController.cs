using System.Collections.Generic;
using UnityEngine;

namespace JuicyDI
{
    public class SecondJDIController : MonoBehaviour
    {
        private IBinController m_BinController;
        
        public void Init()
        {
            m_BinController = BinController.GetContext();
            
            List<MonoBehaviour> allSceneObjects = new List<MonoBehaviour>();
            foreach (var rootGameObject in gameObject.scene.GetRootGameObjects())
            {
                allSceneObjects.AddRange(
                    rootGameObject.GetComponentsInChildren<MonoBehaviour>(true));
            }

            m_BinController.InitBins(allSceneObjects);
        }
    }
}