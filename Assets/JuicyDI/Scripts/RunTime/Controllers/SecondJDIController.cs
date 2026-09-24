using System.Collections.Generic;
using UnityEngine;

namespace JuicyDI
{
    public class SecondJDIController : MonoBehaviour
    {
        private IBinController m_BinController;

        public void Init()
        {
            // Сцена может быть загружена как аддитивно, так и первой - контейнер создаём при необходимости.
            m_BinController = BinController.GetOrCreateContext();

            m_BinController.InitBins(JDISceneScanner.CollectSceneBehaviours(gameObject.scene));
        }
    }
}