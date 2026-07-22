using JuicyDI.Attributes;
using JuicyDI.Context;
using JuicyDI.Demo.Scripts.Interfaces;
using UnityEngine;

namespace JuicyDI.Demo.Scripts
{
    [JDIMonoController(Context = typeof(GlobalBean))]
    [SequenceParticipant(1)]
    public class Test1JDIMonoController : MonoBehaviour, ITest1JDIMonoInterface, ITest2JDIMonoInterface, ISequence
    {
        [Inject] private Test3JDIMonoController m_Test1JDIMonoController;

        public void Run()
        {
            DontDestroyOnLoad(this);
            Debug.Log($"m_Test1JDIMonoController - {m_Test1JDIMonoController == null}");
            m_Test1JDIMonoController.Test1();
        }
        
        public void Test3()
        {
            Debug.Log($"Test1JDIMonoController - I exist");
        }
                
        public void Test2()
        {
            Debug.Log($"Test1JDIMonoController - I exist");
        }

        public void MethodInit()
        {
            
        } 
        
        public void MethodStart()
        {
            Debug.Log($"______Test1JDIMonoController____");
            DontDestroyOnLoad(this);
            Debug.Log($"m_Test1JDIMonoController - {m_Test1JDIMonoController == null}");
            m_Test1JDIMonoController.Test1();
        }

        public void LateInjectDebug()
        {
            Debug.Log("LateInject Run");
        }
    }
}