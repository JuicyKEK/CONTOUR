using JuicyDI.Attributes;
using JuicyDI.Demo.Scripts.Interfaces;
using UnityEngine;

namespace JuicyDI.Demo.Scripts
{
    [JDIMonoController]
    public class Test4JDIMonoController : MonoBehaviour, ITestStarter
    {
        [Inject] private ITest1JDIMonoInterface test1JDIMonoInterface;


        public void Run()
        {
            test1JDIMonoInterface.Test3();
        }
    }
}