using System.Collections.Generic;
using JuicyDI.Attributes;
using JuicyDI.Demo.Scripts.Interfaces;
using UnityEngine;

namespace JuicyDI.Demo.Scripts
{
    [JDIMonoController]
    [SequenceParticipant(-1)]
    public class Test3JDIMonoController: MonoBehaviour, ITest3JDIMonoInterface, ISequence
    {
        [Inject] private List<ITest2JDIMonoInterface> m_Test1JDIMonoController;
        
        public void Run()
        {

        }

        public void Test1()
        {
            Debug.Log($"Test3JDIMonoController - I exist");
        }

        public void MethodInit()
        {
            
        } 
        
        public void MethodStart()
        {
            Debug.Log($"______Test3JDIMonoController____");
            foreach (var test in m_Test1JDIMonoController)
            {
                Debug.Log($"ITest3JDIMonoInterface - {test == null}");
                test.Test2();
            } 
        }

        public void RunLateInject()
        {
            Debug.Log($"Запускаем Test1");
            Test1JDILateInjectController test1 = BinController.GetContext()
                .ConstructorLateInjection<Test1JDILateInjectController>("LateInject Test1 WAS constructed", 42);
            test1.Run();
            
            Debug.Log($"Запускаем Test11");
            Test1JDILateInjectController test11 = BinController.GetContext()
                .ConstructorLateInjection<Test1JDILateInjectController>("LateInject Test11 WAS constructed", 11);
            test11.Run();

            
            Debug.Log($"Запускаем Test3");
            Test1JDILateInjectController test3 = BinController.GetContext()
                .ConstructorLateInjection<Test1JDILateInjectController>("LateInject Test3 WAS constructed");
            test3.Run();
            

            
            Debug.Log($"Запускаем Test4");
            Test1JDILateInjectController test4 = BinController.GetContext()
                .ConstructorLateInjection<Test1JDILateInjectController>("LateInject Test4 WAS constructed");
            test4.Run();
                        
            Debug.Log($"Запускаем Test2");
            Test1JDILateInjectController test2 = BinController.GetContext().ConstructorLateInjection<Test1JDILateInjectController>();
            test2.Run();
        }
    }
}