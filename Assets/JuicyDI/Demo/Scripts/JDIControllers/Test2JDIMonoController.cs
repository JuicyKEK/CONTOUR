using JuicyDI.Attributes;
using JuicyDI.Demo.Scripts.Interfaces;
using UnityEngine;

namespace JuicyDI.Demo.Scripts
{
    [JDIMonoController]
    [SequenceParticipant(0)]
    public class Test2JDIMonoController: MonoBehaviour, ITest2JDIMonoInterface, ITest3JDIMonoInterface, ISequence
    {
        [Inject] private ITest1JDIMonoInterface m_Test1JDIMonoController;
        
        public void Run()
        {

        }

        public void Test2()
        {
            Debug.Log($"Test2JDIMonoController - I exist");
        }

        public void MethodInit()
        {
            
        }        
        
        public void MethodStart()
        {
            Debug.Log($"______Test2JDIMonoController____");
            Debug.Log($"ITest2JDIMonoInterface - {m_Test1JDIMonoController == null}");
            m_Test1JDIMonoController.Test3();
        }
    }
}