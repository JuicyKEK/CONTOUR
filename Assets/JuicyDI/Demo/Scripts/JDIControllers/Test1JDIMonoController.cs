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
        // ВАЖНО: глобальный бин не может держать ссылку на бин сцены - после выгрузки сцены
        // это будет уничтоженный объект. Сценные зависимости берём "на месте", в момент вызова.
        private Test3JDIMonoController SceneTest3 => BinController.GetContext()?.GetBean<Test3JDIMonoController>();

        public void Run()
        {
            LogSceneDependency();
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
            LogSceneDependency();
        }

        public void LateInjectDebug()
        {
            Debug.Log("LateInject Run");
        }

        private void LogSceneDependency()
        {
            var test3 = SceneTest3;
            Debug.Log($"m_Test1JDIMonoController - {test3 == null}");
            test3?.Test1();
        }
    }
}