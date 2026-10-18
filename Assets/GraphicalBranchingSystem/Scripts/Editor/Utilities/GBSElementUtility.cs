using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
namespace GBS.Utility
{
    /// <summary>
    /// Мелкие хелперы для сборки UI нод графа.
    ///
    /// Важно: все поля собираются "сжимаемыми" (flex-shrink: 1, min-width: 0),
    /// иначе содержимое строки вылезает за границы ноды и обрезается - именно
    /// поэтому раньше не было видно кнопок удаления и выходных портов переходов.
    /// </summary>
    public static class GBSElementUtility
    {
        public static Button CreateButton(string text, Action onClick = null)
        {
            var button = new Button(onClick)
            {
                text = text
            };
            button.AddToClassList("ds-node__button");
            return button;
        }
        /// <summary>Компактная кнопка удаления элемента списка или строки перехода.</summary>
        public static Button CreateDeleteButton(Action onClick, string tooltip = "Удалить")
        {
            var button = CreateButton("X", onClick);
            button.tooltip = tooltip;
            button.AddToClassList("gbs-node__delete-button");
            button.style.flexShrink = 0;
            button.style.flexGrow = 0;
            button.style.width = 22;
            button.style.minWidth = 22;
            button.style.marginLeft = 2;
            button.style.marginRight = 2;
            return button;
        }
        public static TextField CreateTextField(string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            var textField = new TextField()
            {
                value = value,
                label = label
            };
            if (onValueChanged != null)
            {
                textField.RegisterValueChangedCallback(onValueChanged);
            }
            return textField;
        }
        public static TextField CreateTextArea(string value = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            var textArea = CreateTextField(value, label, onValueChanged);
            textArea.multiline = true;
            return textArea;
        }
        public static ObjectField CreateObjectField(Type objectType, UnityEngine.Object value, EventCallback<ChangeEvent<UnityEngine.Object>> onValueChanged = null, string label = null)
        {
            var objectField = new ObjectField(label)
            {
                objectType = objectType,
                allowSceneObjects = false,
                value = value
            };
            if (onValueChanged != null)
            {
                objectField.RegisterValueChangedCallback(onValueChanged);
            }
            // Поле обязано уметь сжиматься, иначе "кружок" выбора ассета
            // уезжает за правую границу ноды и его не видно.
            objectField.style.flexGrow = 1;
            objectField.style.flexShrink = 1;
            objectField.style.minWidth = 0;
            objectField.style.marginLeft = 0;
            objectField.style.marginRight = 0;
            return objectField;
        }
        public static Foldout CreateFoldout(string title, bool collapsed = false)
        {
            var foldout = new Foldout()
            {
                text = title,
                value = !collapsed
            };
            foldout.style.flexGrow = 1;
            foldout.style.flexShrink = 1;
            foldout.style.minWidth = 0;
            return foldout;
        }
        public static VisualElement CreateRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.flexGrow = 0;
            row.style.flexShrink = 1;
            row.style.minWidth = 0;
            row.style.marginBottom = 2;
            return row;
        }
        /// <summary>Вертикальный контейнер во всю ширину ноды.</summary>
        public static VisualElement CreateColumn()
        {
            var column = new VisualElement();
            column.style.flexDirection = FlexDirection.Column;
            column.style.flexGrow = 1;
            column.style.flexShrink = 1;
            column.style.minWidth = 0;
            return column;
        }
        /// <summary>Мелкая подпись слева от поля.</summary>
        public static Label CreateInlineLabel(string text, float width = 62f)
        {
            var label = new Label(text);
            label.style.width = width;
            label.style.minWidth = width;
            label.style.flexShrink = 0;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.fontSize = 10;
            label.style.opacity = 0.7f;
            return label;
        }
        /// <summary>Элемент (например порт) не сжимается и не растягивается.</summary>
        public static T AsFixed<T>(this T element) where T : VisualElement
        {
            element.style.flexShrink = 0;
            element.style.flexGrow = 0;
            return element;
        }
    }
}
