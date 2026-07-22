using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JuicyDI.Scripts.RunTime.Factory
{
    public class LateInjectionFactory : ILateInjectionFactory
    {
        private Dictionary<string, Func<object[], object>> m_Factories = new();

        public object ConstructorLateInjection(Type type, object[] runtimeArgs)
        {
            string key = BuildSignature(type, runtimeArgs);
            
            if (!m_Factories.TryGetValue(key, out var cachedFactory))
            {
                m_Factories[key] = BuildFactory(type, runtimeArgs);
            }
            
            return m_Factories[key](runtimeArgs);
        }
        
        private string BuildSignature(Type type, object[] runtimeArgs)
        {
            var parts = new List<string> { type.AssemblyQualifiedName };
            foreach (var a in runtimeArgs)
            {
                parts.Add(a == null ? "null" : a.GetType().AssemblyQualifiedName);
            }
            return string.Join("|", parts);
        }
        
        private Func<object[], object> BuildFactory(Type type, object[] runtimeArgs)
        {
            runtimeArgs = runtimeArgs ?? Array.Empty<object>();

            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                throw new Exception($"No public constructors for {type}");
            } 
                    
            var currentLenConstructors = constructors.OrderByDescending(c => c.GetParameters().Length)
                .Where(c => c.GetParameters().Length == runtimeArgs.Length)
                .ToArray();
            
            ConstructorInfo selectedConstructor = null;
            
            for (int i = 0; i < currentLenConstructors.Length; i++)
            {
                var parameters = currentLenConstructors[i].GetParameters();
                bool flag = true;
                int exactMatches = 0;

                for (int j = 0; j < parameters.Length; j++)
                {
                    var arg = runtimeArgs[j];
                    var pType = parameters[j].ParameterType;

                    if (runtimeArgs[j] == null)
                    {
                        if (pType.IsValueType && Nullable.GetUnderlyingType(pType) == null)
                        {
                            flag = false;
                            break;
                        }
                    }
                    else
                    {
                        var argType = arg.GetType();
                        if (!pType.IsAssignableFrom(argType))
                        {
                            flag = false;
                            break;
                        }
                    }
                }

                if (flag)
                {
                    selectedConstructor = currentLenConstructors[i];
                    break;
                }
            }
            
            return (args) =>
            {
                if (selectedConstructor == null)
                {
                    var provided = string.Join(", ", runtimeArgs.Select(a => a?.GetType().Name ?? "null"));
                    throw new Exception($"No suitable constructor for {type}. Provided types: {provided}");
                }

                return selectedConstructor.Invoke(args);
            };
        }
    }
}