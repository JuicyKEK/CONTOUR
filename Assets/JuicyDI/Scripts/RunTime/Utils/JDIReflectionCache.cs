using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JuicyDI.Attributes;
using JuicyDI.Context;

namespace JuicyDI.Utils
{
    /// <summary>
    /// Описание одного [Inject] поля. Считается один раз на тип за всё время жизни приложения.
    /// </summary>
    public readonly struct InjectedField
    {
        public readonly FieldInfo Field;
        /// <summary>Тип, который надо резолвить (для List&lt;T&gt; это T).</summary>
        public readonly Type Contract;
        public readonly bool IsCollection;
        /// <summary>Тип самого поля (List&lt;T&gt;), нужен для создания экземпляра коллекции.</summary>
        public readonly Type CollectionType;

        public InjectedField(FieldInfo field, Type contract, bool isCollection, Type collectionType)
        {
            Field = field;
            Contract = contract;
            IsCollection = isCollection;
            CollectionType = collectionType;
        }
    }

    /// <summary>
    /// Кэш рефлексии DI. Все словари статические: типы не меняются в рантайме,
    /// поэтому платим за рефлексию ровно один раз, а не на каждой загрузке сцены.
    /// </summary>
    public static class JDIReflectionCache
    {
        private const BindingFlags FieldsFlags = BindingFlags.NonPublic
                                                 | BindingFlags.Public
                                                 | BindingFlags.Instance
                                                 | BindingFlags.DeclaredOnly;

        private static readonly Dictionary<Type, InjectedField[]> s_InjectedFields = new Dictionary<Type, InjectedField[]>();
        private static readonly Dictionary<Type, Type[]> s_Contracts = new Dictionary<Type, Type[]>();
        private static readonly Dictionary<Type, bool?> s_IsGlobalBean = new Dictionary<Type, bool?>();

        private static readonly InjectedField[] s_EmptyFields = Array.Empty<InjectedField>();
        private static readonly List<InjectedField> s_FieldsBuffer = new List<InjectedField>(16);
        private static readonly List<Type> s_ContractsBuffer = new List<Type>(16);

        /// <summary>
        /// null  - тип не является бином (нет атрибута JDIMonoController);
        /// true  - глобальный бин;
        /// false - бин сцены.
        /// </summary>
        public static bool? IsGlobalBean(Type type)
        {
            if (s_IsGlobalBean.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var attribute = type.GetCustomAttribute<JDIMonoController>(false);
            bool? result = attribute == null ? (bool?)null : attribute.Context == typeof(GlobalBean);

            s_IsGlobalBean[type] = result;
            return result;
        }

        /// <summary>
        /// Контракты, по которым бин может быть найден: сам тип + все его интерфейсы.
        /// </summary>
        public static Type[] GetContracts(Type type)
        {
            if (s_Contracts.TryGetValue(type, out var cached))
            {
                return cached;
            }

            s_ContractsBuffer.Clear();
            s_ContractsBuffer.Add(type);

            var interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                s_ContractsBuffer.Add(interfaces[i]);
            }

            var result = s_ContractsBuffer.ToArray();
            s_ContractsBuffer.Clear();

            s_Contracts[type] = result;
            return result;
        }

        /// <summary>
        /// Все поля с [Inject], включая приватные поля базовых классов.
        /// </summary>
        public static InjectedField[] GetInjectedFields(Type type)
        {
            if (s_InjectedFields.TryGetValue(type, out var cached))
            {
                return cached;
            }

            s_FieldsBuffer.Clear();

            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                var fields = current.GetFields(FieldsFlags);
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (!field.IsDefined(typeof(Inject), false))
                    {
                        continue;
                    }

                    var fieldType = field.FieldType;
                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        s_FieldsBuffer.Add(new InjectedField(field, fieldType.GetGenericArguments()[0], true, fieldType));
                    }
                    else
                    {
                        s_FieldsBuffer.Add(new InjectedField(field, fieldType, false, null));
                    }
                }
            }

            var result = s_FieldsBuffer.Count == 0 ? s_EmptyFields : s_FieldsBuffer.ToArray();
            s_FieldsBuffer.Clear();

            s_InjectedFields[type] = result;
            return result;
        }

        /// <summary>
        /// Создание List&lt;T&gt; без generic-рефлексии на каждом инжекте.
        /// </summary>
        public static IList CreateList(Type listType)
        {
            return (IList)Activator.CreateInstance(listType);
        }
    }
}

