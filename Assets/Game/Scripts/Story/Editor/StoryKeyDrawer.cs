using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Story.EditorTools
{
    /// <summary>
    /// Поле ключа сюжета ([StoryKey]): текст + "▾" (выбор ключа: сначала каталог StoryKeyCatalogSO, затем
    /// ключ из него) + "+" (добавить введённый ключ в выбранный каталог - видна, только если ключа нигде нет).
    /// Работает и в инспекторе, и в нодах графа GBS.
    /// </summary>
    [CustomPropertyDrawer(typeof(StoryKeyAttribute))]
    public class StoryKeyDrawer : PropertyDrawer
    {
        private const float ButtonWidth = 20f;

        private StoryKeyKind Kind => ((StoryKeyAttribute)attribute).Kind;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                return new Label($"{property.displayName}: [StoryKey] работает только со string.");
            }

            var serializedObject = property.serializedObject;
            var propertyPath = property.propertyPath;
            var kind = Kind;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexShrink = 1;
            row.style.minWidth = 0;

            var textField = new TextField(property.displayName);
            textField.BindProperty(property);
            textField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            textField.style.flexGrow = 1;
            textField.style.flexShrink = 1;
            textField.style.minWidth = 0;

            var pickButton = new Button { text = "▾", tooltip = "Выбрать ключ: каталог → ключ" };
            pickButton.style.width = ButtonWidth;
            pickButton.clicked += () => ShowKeyMenu(pickButton.worldBound, kind, textField.value,
                key => SetValue(serializedObject, propertyPath, key));

            var addButton = new Button { text = "+", tooltip = "Ключа нет ни в одном каталоге - добавить" };
            addButton.style.width = ButtonWidth;
            addButton.clicked += () => ShowAddMenu(addButton.worldBound, textField.value, kind,
                () => Refresh(textField, addButton));

            textField.RegisterValueChangedCallback(_ => Refresh(textField, addButton));

            row.Add(textField);
            row.Add(pickButton);
            row.Add(addButton);

            Refresh(textField, addButton, property.stringValue);
            return row;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "[StoryKey] работает только со string.");
                return;
            }

            var fieldRect = new Rect(position.x, position.y, position.width - ButtonWidth * 2f, position.height);
            var pickRect = new Rect(fieldRect.xMax, position.y, ButtonWidth, position.height);
            var addRect = new Rect(pickRect.xMax, position.y, ButtonWidth, position.height);

            label.tooltip = GetTooltip(property.stringValue);
            EditorGUI.PropertyField(fieldRect, property, label);

            var serializedObject = property.serializedObject;
            var propertyPath = property.propertyPath;
            var value = property.stringValue;

            if (GUI.Button(pickRect, "▾"))
            {
                ShowKeyMenu(pickRect, Kind, value, key => SetValue(serializedObject, propertyPath, key));
            }

            using (new EditorGUI.DisabledScope(IsKnown(value)))
            {
                if (GUI.Button(addRect, new GUIContent("+", "Ключа нет ни в одном каталоге - добавить")))
                {
                    ShowAddMenu(addRect, value, Kind, null);
                }
            }
        }

        private static void Refresh(TextField textField, VisualElement addButton, string value = null)
        {
            value ??= textField.value;

            addButton.style.display = IsKnown(value) ? DisplayStyle.None : DisplayStyle.Flex;
            textField.tooltip = GetTooltip(value);
        }

        private static bool IsKnown(string value)
        {
            return string.IsNullOrEmpty(value) || StoryKeyCatalogUtility.Find(value) != null;
        }

        /// <summary>
        /// Подсказка поля: из какого каталога ключ и его описание; предупреждения об опечатке и дублях.
        /// </summary>
        private static string GetTooltip(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            var catalogs = StoryKeyCatalogUtility.FindCatalogs(value);

            if (catalogs.Count == 0)
            {
                return "Ключа нет ни в одном каталоге сюжета (StoryKeyCatalogSO) - опечатка? Нажмите '+', чтобы добавить.";
            }

            var catalogNames = string.Join(", ", catalogs.Select(StoryKeyCatalogUtility.GetDisplayName));
            var description = catalogs[0].Find(value)?.Description;
            var tooltip = $"Каталог: {catalogNames}";

            if (catalogs.Count > 1)
            {
                tooltip += "\nКлюч объявлен в нескольких каталогах - в сюжете это ОДИН и тот же ключ.";
            }

            return string.IsNullOrEmpty(description) ? tooltip : tooltip + "\n" + description;
        }

        private static void SetValue(SerializedObject serializedObject, string propertyPath, string key)
        {
            if (serializedObject == null || serializedObject.targetObject == null)
            {
                return;
            }

            serializedObject.Update();
            var property = serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                return;
            }

            property.stringValue = key;
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Меню выбора: первый уровень - каталоги, внутри - их ключи (группы через "/" остаются подменю).
        /// </summary>
        private static void ShowKeyMenu(Rect position, StoryKeyKind kind, string currentKey, System.Action<string> onSelected)
        {
            var menu = new GenericMenu();
            var kindName = kind == StoryKeyKind.Any ? string.Empty : $" вида {kind}";

            foreach (var catalog in StoryKeyCatalogUtility.Catalogs)
            {
                var catalogName = StoryKeyCatalogUtility.GetDisplayName(catalog);
                bool hasKeys = false;

                foreach (var entry in StoryKeyCatalogUtility.GetEntries(catalog, kind))
                {
                    var key = entry.Key;
                    menu.AddItem(new GUIContent($"{catalogName}/{key}", entry.Description), key == currentKey, () => onSelected(key));
                    hasKeys = true;
                }

                if (!hasKeys)
                {
                    menu.AddDisabledItem(new GUIContent($"{catalogName}/Нет ключей{kindName}"));
                }
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("Нет каталогов ключей - введите ключ и нажмите '+'"));
            }

            menu.DropDown(position);
        }

        /// <summary>
        /// "+": один каталог - ключ добавляется сразу, несколько - выбор каталога (главы).
        /// </summary>
        private static void ShowAddMenu(Rect position, string key, StoryKeyKind kind, System.Action onAdded)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var catalogs = StoryKeyCatalogUtility.Catalogs;

            if (catalogs.Count <= 1)
            {
                var catalog = catalogs.Count == 1 ? catalogs[0] : StoryKeyCatalogUtility.CreateDefaultCatalog();
                StoryKeyCatalogUtility.AddKey(catalog, key, kind);
                onAdded?.Invoke();
                return;
            }

            var menu = new GenericMenu();

            foreach (var catalog in catalogs)
            {
                var target = catalog;
                menu.AddItem(new GUIContent($"Добавить в каталог/{StoryKeyCatalogUtility.GetDisplayName(catalog)}"), false, () =>
                {
                    StoryKeyCatalogUtility.AddKey(target, key, kind);
                    onAdded?.Invoke();
                });
            }

            menu.DropDown(position);
        }
    }
}
