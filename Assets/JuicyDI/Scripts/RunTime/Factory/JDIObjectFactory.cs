using System;
using System.Collections.Generic;
using System.Reflection;
using JuicyDI.Attributes;
using JuicyDI.Context;
using JuicyDI.Utils;
using UnityEngine;

namespace JuicyDI
{
    public class JDIObjectFactory : IObjectFactory
    {
        private Dictionary<string, object> m_GlobalBeansContainer = new Dictionary<string, object>();
        private MultiValueDictionary<string, object> m_SceneBeansContainer = new MultiValueDictionary<string, object>();
        private MultiValueDictionary<string, string> m_MapInterfaceToBeans = new MultiValueDictionary<string, string>();
        
        private List<string> m_MapInterfaceToDestroy = new List<string>();
        
        public void RegisterMonoBehaviorsBeans(List<MonoBehaviour> monoBehaviours)
        {
            if (monoBehaviours == null)
            {
                return;
            }
            
            foreach (var monoBehaviour in monoBehaviours)
            {
                if (monoBehaviour == null)
                {
                    continue;
                }
 
                var monoController = monoBehaviour.GetType().GetCustomAttribute<JDIMonoController>();
                if (monoController != null)
                {
                    string name = monoBehaviour.GetType().AssemblyQualifiedName;

                    // if (m_GlobalBeansContainer.ContainsKey(name))
                    // {
                    //     Debug.LogError($"bean with name: {name} already exists in GlobalBeansContainer");
                    //     continue;
                    // }
                    //
                    // if (m_SceneBeansContainer.ContainsKey(name))
                    // {
                    //     Debug.LogError($"bean with name: {name} already exists in SceneBeansContainer");
                    //     continue;
                    // }

                    if (monoController.Context == typeof(SceneBean))
                    {
                        m_SceneBeansContainer.Add(name, monoBehaviour);
                    }
                    else
                    {
                        m_GlobalBeansContainer.Add(name, monoBehaviour);
                    }
                    
                    RegisterInterface(monoBehaviour);
                }
            }
        }

        public void InjectingBeans()
        {
            Injecting(m_GlobalBeansContainer);
            Injecting(m_SceneBeansContainer);
        }

        public void LateInjectingBeans(object bean)
        {
            Injecting(bean);
        }

        public void RemoveSceneContext()
        {
            if (m_SceneBeansContainer != null)
            {
                m_SceneBeansContainer.Clear();
            }

            if (m_MapInterfaceToDestroy != null)
            {
                m_MapInterfaceToDestroy.Clear();
            }
        }

        private void Injecting(Dictionary<string, object> beansContainer)
        {
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

            foreach (var bean in beansContainer.Values)
            {
                foreach (var field in bean.GetType().GetFields(bindingFlags))
                {
                    foreach (var attr in Attribute.GetCustomAttributes(field))
                    {
                        if (attr.GetType() == typeof(Inject))
                        {
                            InjectToField(field, bean);
                        }
                    }
                }
            }
        }
        
        private void Injecting(MultiValueDictionary<string, object> beansContainer)
        {
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
            
            foreach (var keys in beansContainer.GetKeys())
            {
                foreach (var bean in beansContainer.Values(keys))
                {
                    foreach (var field in bean.GetType().GetFields(bindingFlags))
                    {
                        foreach (var attr in Attribute.GetCustomAttributes(field))
                        {
                            if (attr.GetType() == typeof(Inject))
                            {
                                InjectToField(field, bean);
                            }
                        }
                    }
                }
            }
        }
        
        private void Injecting(object bean)
        {
            var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;

            foreach (var field in bean.GetType().GetFields(bindingFlags))
            {
                foreach (var attr in Attribute.GetCustomAttributes(field))
                {
                    if (attr.GetType() == typeof(Inject))
                    {
                        InjectToField(field, bean);
                    }
                }
            }
        }

        private void InjectToField(FieldInfo field, object bean)
        {
            if (!field.FieldType.IsGenericType)
            {
                var key = field.FieldType.AssemblyQualifiedName;
#if UNITY_EDITOR
                try
                {
#endif
                    var b = GetBean(key);
                    field.SetValue(bean, b);
#if UNITY_EDITOR
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Inject to object with type: {bean.GetType()}");
                    throw;
                }
#endif
            }
            else
            {
                var arguments = field.FieldType.GetGenericArguments();
                if (field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    string key = arguments[0].AssemblyQualifiedName;
                    MethodInfo method = typeof(IObjectFactory).GetMethod(nameof(IObjectFactory.GetBeans));
                    MethodInfo genericMethod = method.MakeGenericMethod(arguments[0]);
                    object[] parameters = new object[] { key };
#if UNITY_EDITOR
                    try
                    {
#endif
                        field.SetValue(bean, genericMethod.Invoke(this, parameters));
#if UNITY_EDITOR
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Inject to object with type: {bean.GetType()} I think you have empty collections to inject in field {field.Name}");
                        throw;
                    }
#endif
                }
            }
        }

        //Мб это все в отдельные сервисы вынести?

        public List<T> GetBeans<T>(string key)
        {
            List<T> beans = new List<T>();

            if (m_MapInterfaceToBeans.ContainsKey(key))
            {
                var values = m_MapInterfaceToBeans.Values(key);
                for (int i = 0; i < values.Count; i++)
                {
                    var result = GetBeanObject(values[i]);
                    if (result != null)
                    {
                        beans.Add((T)result);
                    }
                }
                
                RemoveOldInterfaceBeans(key);
            }
            
            if (m_GlobalBeansContainer.ContainsKey(key))
            {
                beans.Add((T)m_GlobalBeansContainer[key]);
            }
            
            if (m_SceneBeansContainer.ContainsKey(key))
            {
                for (int i = 0; i < m_SceneBeansContainer[key].Count; i++)
                {
                    beans.Add((T)m_SceneBeansContainer[key][i]);
                }
            }
            
            return beans;
        }
        
        private object GetBean(string key)
        {
            string beanName;
            if (m_MapInterfaceToBeans.ContainsKey(key))
            {
                beanName = m_MapInterfaceToBeans.Value(key);
                var bean = GetBeanObject(beanName);
                RemoveOldInterfaceBeans(key);
                return bean;
            }
            
            if (m_GlobalBeansContainer.ContainsKey(key))
            {
                return m_GlobalBeansContainer[key];
            }
            
            if (m_SceneBeansContainer.ContainsKey(key))
            {
                return m_SceneBeansContainer.Value(key);
            }
            
            return null;
        }

        private object GetBeanObject(string beanName)
        {
            if (m_GlobalBeansContainer.ContainsKey(beanName))
            {
                return m_GlobalBeansContainer[beanName];
            }

            if (m_SceneBeansContainer.ContainsKey(beanName))
            {
                return m_SceneBeansContainer.Value(beanName);
            }
            
            m_MapInterfaceToDestroy.Add(beanName);
            return null;
        }

        private void RegisterInterface(MonoBehaviour monoBehaviour)
        {
            var interfaces = monoBehaviour.GetType().GetInterfaces();

            for (int i = 0; i < interfaces.Length; i++)
            {
                m_MapInterfaceToBeans.Add(interfaces[i].AssemblyQualifiedName,
                    monoBehaviour.GetType().AssemblyQualifiedName); 
            }
        }

        private void RemoveOldInterfaceBeans(string key)
        {
            if (m_MapInterfaceToDestroy.Count == 1)
            {
                m_MapInterfaceToBeans.RemoveValue(key, m_MapInterfaceToDestroy[0]); 
                return;
            }
            
            if (m_MapInterfaceToDestroy.Count > 1)
            {
                m_MapInterfaceToBeans.RemoveValues(key, m_MapInterfaceToDestroy); 
                return;
            }
        }
    }
}