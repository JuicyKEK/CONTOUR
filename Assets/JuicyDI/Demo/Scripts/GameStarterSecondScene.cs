using System.Collections.Generic;
using JuicyDI.Attributes;
using JuicyDI.Demo.Scripts.Interfaces;
using UnityEngine;

namespace JuicyDI.Demo.Scripts
{
    [JDIMonoController]
    public class GameStarterSecondScene : MonoBehaviour
    {
        [Inject] private List<ITestStarter> m_TestStarter;
        
        [SerializeField] private SecondJDIController m_SecondJDIController;

        private void Start()
        {
            Debug.Log("~~~~Before Injection~~~~");
            m_SecondJDIController.Init();

            Debug.Log("~~~~After Injection~~~~");
            foreach (var starter in m_TestStarter)
            {
                starter.Run();
            }
            
            Debug.Log("~~~~After Run~~~~");
        }
    }
}