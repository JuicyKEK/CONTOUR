using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GBS.Steps;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GBS.Utility
{
    /// <summary>
    /// UI для встроенных действий/условий ([SerializeReference]) в нодах графа:
    ///  - список: у каждого элемента заголовок (тип - клик меняет тип, ↑ ↓ X) и поля;
    ///    внизу кнопка добавления с меню всех доступных типов;
    ///  - одиночное поле: заголовок с выбором типа (или None) и поля.
    /// Типы берутся автоматически (TypeCache) - новый класс-наследник GBSAction/GBSCondition
    /// сам появляется в меню; путь в меню задаёт атрибут GBSMenu.
    /// Вложенные [SerializeReference] (например условия внутри All Of) рисуются так же.
    /// </summary>
    public static class GBSManagedReferenceUI
    {
        private const string StepClass = "gbs-step";
        private const string StepHeaderClass = "gbs-step__header";

        public static VisualElement CreateList(SerializedObject serializedObject, string propertyPath, Type baseType, string addButtonText)
        {
            return new ManagedReferenceList(serializedObject, propertyPath, baseType, addButtonText);
        }

        public static VisualElement CreateField(SerializedObject serializedObject, string propertyPath, Type baseType, string label)
        {
            return new ManagedReferenceField(serializedObject, propertyPath, baseType, label);
        }

        private sealed class ManagedReferenceList : VisualElement
        {
            private readonly SerializedObject m_SerializedObject;
            private readonly string m_PropertyPath;
            private readonly Type m_BaseType;
            private readonly VisualElement m_Items;

            public ManagedReferenceList(SerializedObject serializedObject, string propertyPath, Type baseType, string addButtonText)
            {
                m_SerializedObject = serializedObject;
                m_PropertyPath = propertyPath;
                m_BaseType = baseType;

                style.flexShrink = 1;
                style.minWidth = 0;

                m_Items = GBSElementUtility.CreateColumn();
                Add(m_Items);

                var addButton = GBSElementUtility.CreateButton(addButtonText);
                addButton.clicked += () => ShowTypeMenu(addButton.worldBound, m_BaseType, false, AddItem);
                Add(addButton);

                Rebuild();
            }

            private SerializedProperty ListProperty => m_SerializedObject.FindProperty(m_PropertyPath);

            private void Rebuild()
            {
                m_Items.Clear();
                m_SerializedObject.Update();

                var list = ListProperty;

                if (list == null || !list.isArray)
                {
                    return;
                }

                for (int i = 0; i < list.arraySize; i++)
                {
                    m_Items.Add(CreateItem(list.GetArrayElementAtIndex(i), i, list.arraySize));
                }
            }

            private VisualElement CreateItem(SerializedProperty element, int index, int count)
            {
                var container = new VisualElement();
                container.AddToClassList(StepClass);

                var header = GBSElementUtility.CreateRow();
                header.AddToClassList(StepHeaderClass);

                var typeButton = GBSElementUtility.CreateButton($"{index + 1}. {GetDisplayName(element)}");
                typeButton.tooltip = "Сменить тип";
                typeButton.style.flexGrow = 1;
                typeButton.style.flexShrink = 1;
                typeButton.style.minWidth = 0;
                typeButton.style.unityTextAlign = TextAnchor.MiddleLeft;
                typeButton.clicked += () => ShowTypeMenu(typeButton.worldBound, m_BaseType, false, type => SetItemType(index, type));

                header.Add(typeButton);
                header.Add(CreateSmallButton("↑", "Выше", () => MoveItem(index, index - 1), index > 0));
                header.Add(CreateSmallButton("↓", "Ниже", () => MoveItem(index, index + 1), index < count - 1));
                header.Add(GBSElementUtility.CreateDeleteButton(() => schedule.Execute(() => RemoveItem(index))));

                container.Add(header);
                CreateChildren(m_SerializedObject, element, container);
                return container;
            }

            private void AddItem(Type type)
            {
                m_SerializedObject.Update();
                var list = ListProperty;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).managedReferenceValue = CreateInstance(type);
                ApplyAndRebuild();
            }

            private void SetItemType(int index, Type type)
            {
                m_SerializedObject.Update();
                ListProperty.GetArrayElementAtIndex(index).managedReferenceValue = CreateInstance(type);
                ApplyAndRebuild();
            }

            private void MoveItem(int from, int to)
            {
                m_SerializedObject.Update();
                ListProperty.MoveArrayElement(from, to);
                ApplyAndRebuild();
            }

            private void RemoveItem(int index)
            {
                m_SerializedObject.Update();
                var list = ListProperty;

                if (index < 0 || index >= list.arraySize)
                {
                    return;
                }

                int size = list.arraySize;
                list.DeleteArrayElementAtIndex(index);

                // Для части типов первый вызов только обнуляет элемент - тогда удаляем вторым.
                if (list.arraySize == size)
                {
                    list.DeleteArrayElementAtIndex(index);
                }

                ApplyAndRebuild();
            }

            private void ApplyAndRebuild()
            {
                m_SerializedObject.ApplyModifiedProperties();
                Rebuild();
            }
        }

        private sealed class ManagedReferenceField : VisualElement
        {
            private readonly SerializedObject m_SerializedObject;
            private readonly string m_PropertyPath;
            private readonly Type m_BaseType;
            private readonly string m_Label;

            public ManagedReferenceField(SerializedObject serializedObject, string propertyPath, Type baseType, string label)
            {
                m_SerializedObject = serializedObject;
                m_PropertyPath = propertyPath;
                m_BaseType = baseType;
                m_Label = label;

                AddToClassList(StepClass);
                style.flexShrink = 1;
                style.minWidth = 0;

                Rebuild();
            }

            private void Rebuild()
            {
                Clear();
                m_SerializedObject.Update();

                var property = m_SerializedObject.FindProperty(m_PropertyPath);

                if (property == null)
                {
                    return;
                }

                var header = GBSElementUtility.CreateRow();
                header.AddToClassList(StepHeaderClass);

                if (!string.IsNullOrEmpty(m_Label))
                {
                    header.Add(GBSElementUtility.CreateInlineLabel(m_Label));
                }

                var hasValue = property.managedReferenceValue != null;
                var typeButton = GBSElementUtility.CreateButton(hasValue ? GetDisplayName(property) : "None");
                typeButton.tooltip = "Выбрать тип";
                typeButton.style.flexGrow = 1;
                typeButton.style.flexShrink = 1;
                typeButton.style.minWidth = 0;
                typeButton.style.unityTextAlign = TextAnchor.MiddleLeft;
                typeButton.clicked += () => ShowTypeMenu(typeButton.worldBound, m_BaseType, true, SetType);

                header.Add(typeButton);
                Add(header);

                if (hasValue)
                {
                    CreateChildren(m_SerializedObject, property, this);
                }
            }

            private void SetType(Type type)
            {
                m_SerializedObject.Update();
                m_SerializedObject.FindProperty(m_PropertyPath).managedReferenceValue = type != null ? CreateInstance(type) : null;
                m_SerializedObject.ApplyModifiedProperties();
                Rebuild();
            }
        }

        /// <summary>
        /// Поля объекта [SerializeReference]: обычные - через PropertyField (с drawer'ами атрибутов),
        /// вложенные [SerializeReference] - через эти же список/поле с выбором типа.
        /// </summary>
        private static void CreateChildren(SerializedObject serializedObject, SerializedProperty property, VisualElement target)
        {
            var managedType = property.managedReferenceValue?.GetType();

            if (managedType == null)
            {
                return;
            }

            var iterator = property.Copy();
            var end = property.GetEndProperty();

            if (!iterator.NextVisible(true))
            {
                return;
            }

            do
            {
                if (SerializedProperty.EqualContents(iterator, end))
                {
                    break;
                }

                var field = FindField(managedType, iterator.name);

                if (field != null && field.IsDefined(typeof(SerializeReference), true))
                {
                    var label = ObjectNames.NicifyVariableName(iterator.name);

                    if (TryGetListElementType(field.FieldType, out var elementType))
                    {
                        var foldout = GBSElementUtility.CreateFoldout(label);
                        foldout.Add(CreateList(serializedObject, iterator.propertyPath, elementType, "Add"));
                        target.Add(foldout);
                    }
                    else
                    {
                        target.Add(CreateField(serializedObject, iterator.propertyPath, field.FieldType, label));
                    }

                    continue;
                }

                var propertyField = new PropertyField(iterator.Copy());
                propertyField.style.flexShrink = 1;
                propertyField.style.minWidth = 0;
                propertyField.Bind(serializedObject);
                target.Add(propertyField);
            }
            while (iterator.NextVisible(false));
        }

        private static void ShowTypeMenu(Rect position, Type baseType, bool includeNone, Action<Type> onSelected)
        {
            var menu = new GenericMenu();

            if (includeNone)
            {
                menu.AddItem(new GUIContent("None"), false, () => onSelected(null));
                menu.AddSeparator(string.Empty);
            }

            foreach (var type in GetConcreteTypes(baseType).OrderBy(GetMenuPath))
            {
                var selectedType = type;
                menu.AddItem(new GUIContent(GetMenuPath(type)), false, () => onSelected(selectedType));
            }

            menu.DropDown(position);
        }

        private static IEnumerable<Type> GetConcreteTypes(Type baseType)
        {
            return TypeCache.GetTypesDerivedFrom(baseType).Where(type =>
                !type.IsAbstract &&
                !type.IsGenericType &&
                type.IsDefined(typeof(SerializableAttribute), false) &&
                type.GetConstructor(Type.EmptyTypes) != null);
        }

        private static string GetMenuPath(Type type)
        {
            var menu = type.GetCustomAttribute<GBSMenuAttribute>();
            return menu != null && !string.IsNullOrEmpty(menu.Path) ? menu.Path : ObjectNames.NicifyVariableName(type.Name);
        }

        private static string GetDisplayName(SerializedProperty property)
        {
            var type = property.managedReferenceValue?.GetType();

            if (type == null)
            {
                return "(тип не выбран)";
            }

            var path = GetMenuPath(type);
            var slash = path.LastIndexOf('/');
            return slash >= 0 ? path.Substring(slash + 1) : path;
        }

        private static object CreateInstance(Type type)
        {
            return Activator.CreateInstance(type);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                var field = current.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private static bool TryGetListElementType(Type fieldType, out Type elementType)
        {
            if (fieldType.IsArray)
            {
                elementType = fieldType.GetElementType();
                return true;
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = fieldType.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static Button CreateSmallButton(string text, string tooltip, Action onClick, bool isEnabled)
        {
            var button = GBSElementUtility.CreateButton(text, onClick);
            button.tooltip = tooltip;
            button.style.width = 22;
            button.style.minWidth = 22;
            button.style.flexShrink = 0;
            button.SetEnabled(isEnabled);
            return button;
        }
    }
}
