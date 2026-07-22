using JuicyDI.Attributes;
using UnityEngine;

namespace JuicyDI.Demo.Scripts
{
    public class Test1JDILateInjectController
    {
        [Inject] private Test1JDIMonoController _test1JDIMonoController;

        public Test1JDILateInjectController(string text)
        {
            Debug.Log(text);
        }
        
        public Test1JDILateInjectController()
        {
            Debug.Log("Test1JDILateInjectController WITHOUT arguments");
        }
        
        public Test1JDILateInjectController(string text, int param2)
        {
            Debug.Log($"{param2} - {text}");
        }
        
        public void Run()
        {
            _test1JDIMonoController.LateInjectDebug();
        }
    }
}