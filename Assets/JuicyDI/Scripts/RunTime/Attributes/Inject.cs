using System;

namespace JuicyDI.Attributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, Inherited = false)]
    public class Inject : Attribute
    {

    }
}