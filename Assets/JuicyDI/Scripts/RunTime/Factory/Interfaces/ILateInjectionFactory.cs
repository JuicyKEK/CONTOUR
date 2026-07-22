using System;

namespace JuicyDI.Scripts.RunTime.Factory
{
    public interface ILateInjectionFactory
    {
        object ConstructorLateInjection(Type type, object[] runtimeArgs);
    }
}