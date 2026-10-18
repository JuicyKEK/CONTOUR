using GBS.Data;
using GBS.Save;
using GBS.Utility;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GBS.Windows
{
    /// <summary>
    /// Окно редактора графов сюжета: UnityDev/GBS/Open Graph.
    /// Тулбар: имя файла, ссылка на ассет графа, Save / Load / New / Clear Saves.
    /// </summary>
    public class GBSEditorWindow : EditorWindow
    {
        /// <summary>
        /// Версия окна. Видна в тулбаре - по ней сразу понятно, что Unity
        /// пересобрала скрипты и работает актуальная версия GBS.
        /// </summary>
        public const string Version = "v3.2";

        private const string DefaultFileName = "NewGBSGraph";
        private const string ToolbarStylePath = "Assets/GraphicalBranchingSystem/Scripts/Editor Default Resources/GBSView/GBSToolbarStyles.uss";
        private const string VariablesStylePath = "Assets/GraphicalBranchingSystem/Scripts/Editor Default Resources/GBSView/GBSVariables.uss";

        private GBSGraphView m_GraphView;
        private TextField m_FileNameTextField;
        private ObjectField m_GraphField;

        [MenuItem("UnityDev/GBS/Open Graph")]
        public static void Open()
        {
            GetWindow<GBSEditorWindow>("GBS Graph");
        }

        /// <summary>Открыть окно уже с загруженным графом (двойной клик по ассету).</summary>
        public static void Open(GBSGraphSO graph)
        {
            var window = GetWindow<GBSEditorWindow>("GBS Graph");

            window.LoadGraph(graph);
        }

        public void LoadGraph(GBSGraphSO graph)
        {
            if (graph == null)
            {
                return;
            }

            m_GraphField.value = graph;
            m_FileNameTextField.value = graph.name;

            GBSIOUtility.Load(m_GraphView, graph);
        }

        private void OnEnable()
        {
            AddGraphView();
            AddToolbar();
            AddStyles();
        }

        private void OnDisable()
        {
            m_GraphView?.DisposeNodes();
        }

        private void AddGraphView()
        {
            m_GraphView = new GBSGraphView(this);

            m_GraphView.StretchToParentSize();

            rootVisualElement.Add(m_GraphView);
        }

        private void AddToolbar()
        {
            var toolbar = new Toolbar();

            m_FileNameTextField = GBSElementUtility.CreateTextField(DefaultFileName, "File Name:", callback =>
            {
                m_FileNameTextField.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();
            });

            m_GraphField = GBSElementUtility.CreateObjectField(typeof(GBSGraphSO), null, null, "Graph:");

            var saveButton = GBSElementUtility.CreateButton("Save", SaveGraph);
            var loadButton = GBSElementUtility.CreateButton("Load", () => LoadGraph(m_GraphField.value as GBSGraphSO));
            var newButton = GBSElementUtility.CreateButton("New", NewGraph);
            var clearSavesButton = GBSElementUtility.CreateButton("Clear Saves", ClearSaves);

            toolbar.Add(m_FileNameTextField);
            toolbar.Add(m_GraphField);
            toolbar.Add(saveButton);
            toolbar.Add(loadButton);
            toolbar.Add(newButton);
            toolbar.Add(clearSavesButton);
            toolbar.Add(new Label($"  GBS {Version}"));

            var toolbarStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(ToolbarStylePath);

            if (toolbarStyle != null)
            {
                toolbar.styleSheets.Add(toolbarStyle);
            }

            rootVisualElement.Add(toolbar);
        }

        private void AddStyles()
        {
            var variablesStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(VariablesStylePath);

            if (variablesStyle != null)
            {
                rootVisualElement.styleSheets.Add(variablesStyle);
            }
        }

        private void SaveGraph()
        {
            var target = m_GraphField.value as GBSGraphSO;

            // Если имя файла отличается от текущего ассета - сохраняем как новый граф.
            if (target != null && target.name != m_FileNameTextField.value)
            {
                target = null;
            }

            var saved = GBSIOUtility.Save(m_GraphView, m_FileNameTextField.value, target);

            if (saved != null)
            {
                m_GraphField.value = saved;
            }
        }

        private void NewGraph()
        {
            m_GraphView.ClearGraph();
            m_GraphField.value = null;
            m_FileNameTextField.value = DefaultFileName;
        }

        private void ClearSaves()
        {
            if (!EditorUtility.DisplayDialog(
                    "GBS",
                    $"Удалить файл прогресса сюжета?\n{GBSSaveSystem.FilePath}",
                    "Удалить",
                    "Отмена"))
            {
                return;
            }

            GBSSaveSystem.Clear();
        }
    }
}


