using System;
using JuicyDI.Context;

namespace JuicyDI.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class JDIMonoController : Attribute
    {
        private Type m_Context = typeof(SceneBean);

        public Type Context
        {
            get => m_Context;
            set => m_Context = value;
        }
    }
}