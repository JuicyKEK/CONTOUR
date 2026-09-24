using System;
using System.Collections;
using System.Collections.Generic;
using JuicyDI.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JuicyDI
{
    /// <summary>
    /// Контейнер бинов.
    ///
    /// Правила скоупов:
    /// 1. GlobalBean регистрируется ОДИН раз за сессию (повторная загрузка сцены его не дублирует),
    ///    новые глобальные бины с новых сцен добавляются к уже существующим.
    /// 2. GlobalBean может зависеть ТОЛЬКО от GlobalBean (captive dependency).
    /// 3. SceneBean живёт в скоупе своей сцены (ключ - Scene.handle), поэтому аддитивная
    ///    загрузка/выгрузка локаций чистит ровно свои бины и не задевает соседние сцены.
    /// 4. SceneBean резолвится: своя сцена -> глобальные -> другие загруженные сцены.
    /// </summary>
    public sealed class JDIObjectFactory : IObjectFactory
    {
        private const int GlobalScopeHandle = 0;

        private readonly struct BeanEntry
        {
            public readonly object Instance;
            public readonly int SceneHandle;

            public BeanEntry(object instance, int sceneHandle)
            {
                Instance = instance;
                SceneHandle = sceneHandle;
            }
        }

        // Глобальный скоуп.
        private readonly Dictionary<Type, object> m_GlobalBeansContainer = new Dictionary<Type, object>();
        private readonly Dictionary<Type, List<object>> m_GlobalContractToBeans = new Dictionary<Type, List<object>>();

        // Скоупы сцен.
        private readonly Dictionary<Type, List<BeanEntry>> m_SceneContractToBeans = new Dictionary<Type, List<BeanEntry>>();
        private readonly Dictionary<int, List<object>> m_SceneScopes = new Dictionary<int, List<object>>();

        private readonly HashSet<object> m_RegisteredSceneInstances =
            new HashSet<object>(ReferenceEqualityComparer.Instance);

        // Переиспользуемые буферы, чтобы не мусорить в GC на каждой загрузке сцены.
        private readonly List<Type> m_DeadGlobalsBuffer = new List<Type>(8);
        private readonly List<Type> m_EmptyContractsBuffer = new List<Type>(16);

        private bool m_HasNewGlobalBeans;

        #region Registration

        public void RegisterMonoBehaviorsBeans(List<MonoBehaviour> monoBehaviours)
        {
            if (monoBehaviours == null)
            {
                return;
            }

            for (int i = 0; i < monoBehaviours.Count; i++)
            {
                var monoBehaviour = monoBehaviours[i];
                if (monoBehaviour == null)
                {
                    continue;
                }

                var type = monoBehaviour.GetType();
                var isGlobal = JDIReflectionCache.IsGlobalBean(type);
                if (isGlobal == null)
                {
                    continue;
                }

                if (isGlobal.Value)
                {
                    RegisterGlobalBean(monoBehaviour, type);
                }
                else
                {
                    RegisterSceneBean(monoBehaviour, type);
                }
            }
        }

        private void RegisterGlobalBean(MonoBehaviour monoBehaviour, Type type)
        {
            if (m_GlobalBeansContainer.TryGetValue(type, out var existing))
            {
                if (IsAlive(existing))
                {
                    if (!ReferenceEquals(existing, monoBehaviour))
                    {
                        // Дубликат глобального бина на новой сцене - оставляем первый.
                        Debug.LogWarning($"[JuicyDI] Global bean '{type.Name}' already registered. " +
                                         $"Duplicate on object '{monoBehaviour.name}' is ignored.");
                    }

                    return;
                }

                // Старый экземпляр уничтожен - подменяем новым.
                RemoveGlobalBean(type, existing);
            }

            KeepAlive(monoBehaviour, type);

            m_GlobalBeansContainer[type] = monoBehaviour;

            var contracts = JDIReflectionCache.GetContracts(type);
            for (int i = 0; i < contracts.Length; i++)
            {
                if (!m_GlobalContractToBeans.TryGetValue(contracts[i], out var beans))
                {
                    beans = new List<object>(2);
                    m_GlobalContractToBeans[contracts[i]] = beans;
                }

                beans.Add(monoBehaviour);
            }

            m_HasNewGlobalBeans = true;
        }

        private void RegisterSceneBean(MonoBehaviour monoBehaviour, Type type)
        {
            // Один и тот же инстанс может прийти повторно (повторный InitBins на той же сцене).
            if (!m_RegisteredSceneInstances.Add(monoBehaviour))
            {
                return;
            }

            int sceneHandle = monoBehaviour.gameObject.scene.handle;

            if (!m_SceneScopes.TryGetValue(sceneHandle, out var scope))
            {
                scope = new List<object>(16);
                m_SceneScopes[sceneHandle] = scope;
            }

            scope.Add(monoBehaviour);

            var entry = new BeanEntry(monoBehaviour, sceneHandle);
            var contracts = JDIReflectionCache.GetContracts(type);
            for (int i = 0; i < contracts.Length; i++)
            {
                if (!m_SceneContractToBeans.TryGetValue(contracts[i], out var beans))
                {
                    beans = new List<BeanEntry>(4);
                    m_SceneContractToBeans[contracts[i]] = beans;
                }

                beans.Add(entry);
            }
        }

        /// <summary>
        /// Глобальный бин обязан пережить смену сцены, иначе контейнер будет хранить мёртвую ссылку.
        /// </summary>
        private void KeepAlive(MonoBehaviour monoBehaviour, Type type)
        {
            var gameObject = monoBehaviour.gameObject;
            var scene = gameObject.scene;

            if (!scene.IsValid())
            {
                return;
            }

            if (gameObject.transform.parent != null)
            {
                Debug.LogWarning($"[JuicyDI] Global bean '{type.Name}' is not a root object " +
                                 $"('{gameObject.name}'). Move it to the scene root, otherwise it will be " +
                                 "destroyed together with its scene.");
                return;
            }

            Object.DontDestroyOnLoad(gameObject);
        }

        #endregion

        #region Injection

        public void InjectingBeans()
        {
            if (m_HasNewGlobalBeans)
            {
                m_HasNewGlobalBeans = false;

                foreach (var bean in m_GlobalBeansContainer.Values)
                {
                    if (IsAlive(bean))
                    {
                        Inject(bean, GlobalScopeHandle, true);
                    }
                }
            }

            // Бины сцен переинжектим всегда: состав загруженных сцен мог измениться,
            // а вместе с ним и содержимое List<T> зависимостей.
            foreach (var scope in m_SceneScopes)
            {
                var beans = scope.Value;
                for (int i = 0; i < beans.Count; i++)
                {
                    var bean = beans[i];
                    if (IsAlive(bean))
                    {
                        Inject(bean, scope.Key, false);
                    }
                }
            }
        }

        public void LateInjectingBeans(object bean)
        {
            if (bean == null)
            {
                return;
            }

            Inject(bean, SceneManager.GetActiveScene().handle, false);
        }

        private void Inject(object bean, int sceneHandle, bool globalOnly)
        {
            var fields = JDIReflectionCache.GetInjectedFields(bean.GetType());

            for (int i = 0; i < fields.Length; i++)
            {
                var injectedField = fields[i];

                if (injectedField.IsCollection)
                {
                    var list = JDIReflectionCache.CreateList(injectedField.CollectionType);
                    ResolveAll(injectedField.Contract, sceneHandle, globalOnly, list);
                    injectedField.Field.SetValue(bean, list);

                    if (list.Count == 0)
                    {
                        Debug.LogWarning($"[JuicyDI] No beans of type '{injectedField.Contract.Name}' " +
                                         $"for field '{injectedField.Field.Name}' in '{bean.GetType().Name}'." +
                                         ScopeHint(globalOnly, injectedField.Contract));
                    }

                    continue;
                }

                var dependency = ResolveSingle(injectedField.Contract, sceneHandle, globalOnly);
                injectedField.Field.SetValue(bean, dependency);

                if (dependency == null)
                {
                    Debug.LogError($"[JuicyDI] Can not resolve '{injectedField.Contract.Name}' " +
                                   $"for field '{injectedField.Field.Name}' in '{bean.GetType().Name}'." +
                                   ScopeHint(globalOnly, injectedField.Contract));
                }
            }
        }

        private string ScopeHint(bool globalOnly, Type contract)
        {
            if (!globalOnly)
            {
                return string.Empty;
            }

            return m_SceneContractToBeans.ContainsKey(contract)
                ? $" '{contract.Name}' is a SceneBean: GlobalBean can not depend on scene beans."
                : string.Empty;
        }

        #endregion

        #region Resolving

        public T GetBean<T>() where T : class
        {
            return (T)ResolveSingle(typeof(T), SceneManager.GetActiveScene().handle, false);
        }

        public object GetBean(Type contract)
        {
            return ResolveSingle(contract, SceneManager.GetActiveScene().handle, false);
        }

        public List<T> GetBeans<T>() where T : class
        {
            var result = new List<T>();
            ResolveAll(typeof(T), SceneManager.GetActiveScene().handle, false, result);
            return result;
        }

        private object ResolveSingle(Type contract, int sceneHandle, bool globalOnly)
        {
            if (!globalOnly)
            {
                // 1. Своя сцена - самый безопасный по времени жизни источник.
                var own = ResolveFromScene(contract, sceneHandle, true);
                if (own != null)
                {
                    return own;
                }
            }

            // 2. Глобальный скоуп.
            if (m_GlobalContractToBeans.TryGetValue(contract, out var globalBeans))
            {
                for (int i = 0; i < globalBeans.Count; i++)
                {
                    if (IsAlive(globalBeans[i]))
                    {
                        return globalBeans[i];
                    }
                }
            }

            if (globalOnly)
            {
                return null;
            }

            // 3. Другие загруженные сцены (аддитивные локации).
            return ResolveFromScene(contract, sceneHandle, false);
        }

        private object ResolveFromScene(Type contract, int sceneHandle, bool sameSceneOnly)
        {
            if (!m_SceneContractToBeans.TryGetValue(contract, out var entries))
            {
                return null;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (sameSceneOnly && entry.SceneHandle != sceneHandle)
                {
                    continue;
                }

                if (IsAlive(entry.Instance))
                {
                    return entry.Instance;
                }
            }

            return null;
        }

        private void ResolveAll(Type contract, int sceneHandle, bool globalOnly, IList target)
        {
            if (!globalOnly && m_SceneContractToBeans.TryGetValue(contract, out var entries))
            {
                // Сначала своя сцена, потом соседние аддитивные.
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].SceneHandle == sceneHandle && IsAlive(entries[i].Instance))
                    {
                        target.Add(entries[i].Instance);
                    }
                }

                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].SceneHandle != sceneHandle && IsAlive(entries[i].Instance))
                    {
                        target.Add(entries[i].Instance);
                    }
                }
            }

            if (m_GlobalContractToBeans.TryGetValue(contract, out var globalBeans))
            {
                for (int i = 0; i < globalBeans.Count; i++)
                {
                    if (IsAlive(globalBeans[i]))
                    {
                        target.Add(globalBeans[i]);
                    }
                }
            }
        }

        #endregion

        #region Lifetime

        public void RemoveSceneContext(int sceneHandle)
        {
            if (m_SceneScopes.TryGetValue(sceneHandle, out var beans))
            {
                for (int i = 0; i < beans.Count; i++)
                {
                    var bean = beans[i];
                    m_RegisteredSceneInstances.Remove(bean);

                    var contracts = JDIReflectionCache.GetContracts(bean.GetType());
                    for (int j = 0; j < contracts.Length; j++)
                    {
                        if (!m_SceneContractToBeans.TryGetValue(contracts[j], out var entries))
                        {
                            continue;
                        }

                        for (int k = entries.Count - 1; k >= 0; k--)
                        {
                            if (ReferenceEquals(entries[k].Instance, bean))
                            {
                                entries.RemoveAt(k);
                            }
                        }

                        if (entries.Count == 0)
                        {
                            m_EmptyContractsBuffer.Add(contracts[j]);
                        }
                    }
                }

                beans.Clear();
                m_SceneScopes.Remove(sceneHandle);
            }

            // Чистим пустые списки, чтобы словарь не рос бесконечно.
            for (int i = 0; i < m_EmptyContractsBuffer.Count; i++)
            {
                if (m_SceneContractToBeans.TryGetValue(m_EmptyContractsBuffer[i], out var entries)
                    && entries.Count == 0)
                {
                    m_SceneContractToBeans.Remove(m_EmptyContractsBuffer[i]);
                }
            }

            m_EmptyContractsBuffer.Clear();

            PurgeDestroyedGlobals();
        }

        public void Clear()
        {
            m_GlobalBeansContainer.Clear();
            m_GlobalContractToBeans.Clear();
            m_SceneContractToBeans.Clear();
            m_SceneScopes.Clear();
            m_RegisteredSceneInstances.Clear();
            m_DeadGlobalsBuffer.Clear();
            m_EmptyContractsBuffer.Clear();
            m_HasNewGlobalBeans = false;
        }

        /// <summary>
        /// Глобальный бин мог быть уничтожен вручную - контейнер не должен держать мёртвую ссылку.
        /// </summary>
        private void PurgeDestroyedGlobals()
        {
            foreach (var pair in m_GlobalBeansContainer)
            {
                if (!IsAlive(pair.Value))
                {
                    m_DeadGlobalsBuffer.Add(pair.Key);
                }
            }

            for (int i = 0; i < m_DeadGlobalsBuffer.Count; i++)
            {
                var type = m_DeadGlobalsBuffer[i];
                RemoveGlobalBean(type, m_GlobalBeansContainer[type]);
            }

            m_DeadGlobalsBuffer.Clear();
        }

        private void RemoveGlobalBean(Type type, object bean)
        {
            m_GlobalBeansContainer.Remove(type);

            var contracts = JDIReflectionCache.GetContracts(type);
            for (int i = 0; i < contracts.Length; i++)
            {
                if (!m_GlobalContractToBeans.TryGetValue(contracts[i], out var beans))
                {
                    continue;
                }

                for (int j = beans.Count - 1; j >= 0; j--)
                {
                    if (ReferenceEquals(beans[j], bean))
                    {
                        beans.RemoveAt(j);
                    }
                }

                if (beans.Count == 0)
                {
                    m_GlobalContractToBeans.Remove(contracts[i]);
                }
            }
        }

        private static bool IsAlive(object bean)
        {
            if (bean == null)
            {
                return false;
            }

            // Fake-null: объект уничтожен движком, но managed-обёртка ещё жива.
            if (bean is Object unityObject)
            {
                return unityObject != null;
            }

            return true;
        }

        #endregion
    }
}