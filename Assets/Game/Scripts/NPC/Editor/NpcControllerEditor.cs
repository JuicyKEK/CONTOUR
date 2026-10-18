using System.IO;
using System.Linq;
using Game.Scripts.NPC.Runtime.Controllers;
using Game.Scripts.NPC.Runtime.Core;
using Game.Scripts.NPC.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.NPC.Editor
{
    /// <summary>
    /// Инспектор бота: поля самого бота, а ниже - настройки его типа (NpcTypeDefinitionSO) прямо здесь, без перехода
    /// в ассет. Тип общий для всех ботов с ним - об этом предупреждение и кнопка "сделать отдельный тип для этого бота".
    /// В Play Mode сверху - текущее состояние бота и кнопки команд для отладки.
    /// </summary>
    [CustomEditor(typeof(NpcController))]
    [CanEditMultipleObjects]
    public class NpcControllerEditor : UnityEditor.Editor
    {
        private UnityEditor.Editor m_DefinitionEditor;
        private UnityEditor.Editor m_ProfileEditor;

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            var npc = (NpcController)target;

            if (Application.isPlaying && targets.Length == 1)
            {
                DrawRuntime(npc);
            }

            DrawDefaultInspector();

            if (targets.Length > 1)
            {
                return;
            }

            serializedObject.Update();
            var definitionProperty = serializedObject.FindProperty("m_Definition");
            var definition = definitionProperty.objectReferenceValue as NpcTypeDefinitionSO;
            EditorGUILayout.Space(10f);

            if (definition == null)
            {
                EditorGUILayout.HelpBox("Не назначен тип бота (NpcTypeDefinitionSO) - в нём все настройки поведения: обзор, " +
                                        "агрессия, скорости, атака, поиск, двери.", MessageType.Warning);

                if (GUILayout.Button("Создать новый тип бота"))
                {
                    var created = CreateDefinition(npc);

                    if (created != null)
                    {
                        definitionProperty.objectReferenceValue = created;
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                return;
            }

            if (!Foldout($"Настройки типа бота: {definition.name}", "definition"))
            {
                return;
            }

            int sharedCount = FindObjectsByType<NpcController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(other => other.Definition == definition);
            EditorGUILayout.HelpBox($"Это ассет типа '{definition.name}' - общий для всех ботов этого типа " +
                                    $"(в открытых сценах таких ботов: {sharedCount}). Изменения здесь меняют их всех.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Сделать отдельный тип для этого бота"))
                {
                    var copy = NpcTypeDefinitionTools.CloneForBot(npc);

                    if (copy != null)
                    {
                        GUIUtility.ExitGUI();
                    }
                }

                if (GUILayout.Button("Открыть ассет", GUILayout.Width(110f)))
                {
                    EditorGUIUtility.PingObject(definition);
                    Selection.activeObject = definition;
                }
            }

            CreateCachedEditor(definition, typeof(NpcTypeDefinitionSOEditor), ref m_DefinitionEditor);

            using (new EditorGUI.IndentLevelScope())
            {
                m_DefinitionEditor.OnInspectorGUI();
            }

            var profile = definition.AnimationSoundProfile;

            if (profile != null && Foldout($"Анимации и звуки типа: {profile.name}", "profile", false))
            {
                CreateCachedEditor(profile, null, ref m_ProfileEditor);

                using (new EditorGUI.IndentLevelScope())
                {
                    m_ProfileEditor.OnInspectorGUI();
                }
            }
        }

        private static void DrawRuntime(NpcController npc)
        {
            EditorGUILayout.LabelField("Сейчас (Play Mode)", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (!npc.IsInitialized)
                {
                    EditorGUILayout.LabelField("Бот ещё не запущен.");
                    return;
                }

                EditorGUILayout.LabelField("Состояние", npc.CurrentState + (npc.IsHunting ? " (сюжетная погоня)" : string.Empty));
                EditorGUILayout.LabelField("Агрессивен", npc.IsAggressive ? "да" : "нет");
                EditorGUILayout.LabelField("Видит игрока", npc.CanSeePlayer ? $"да, {npc.DistanceToPlayer:0.0} м" : "нет");

                if (npc.Definition != null && npc.Definition.AggressionMode == NpcAggressionMode.AngerMeter)
                {
                    var rect = EditorGUILayout.GetControlRect(false, 18f);
                    EditorGUI.ProgressBar(rect, npc.Anger, $"Злость {npc.Anger:P0}");
                }

                if (npc.Zone != null)
                {
                    EditorGUILayout.LabelField("Зона", $"{npc.Zone.DisplayName}: заражение {npc.Zone.InfectionLevel.CurrentValue:0}%");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Обход")) npc.ForcePatrol();
                    if (GUILayout.Button("Стоять")) npc.ForceIdle();
                    if (GUILayout.Button("Погоня")) npc.ForceChase();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Сюжетная погоня")) npc.ForceHunt();
                    if (GUILayout.Button("Поиск")) npc.ForceSearch();
                    if (GUILayout.Button("Потерять интерес")) npc.LoseInterest();
                }
            }

            EditorGUILayout.Space(6f);
        }

        private static NpcTypeDefinitionSO CreateDefinition(NpcController npc)
        {
            string path = EditorUtility.SaveFilePanelInProject("Новый тип бота", $"NpcType_{npc.name}", "asset",
                "Где сохранить тип бота");

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            var definition = CreateInstance<NpcTypeDefinitionSO>();
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            return definition;
        }

        private static bool Foldout(string title, string key, bool defaultOpen = true)
        {
            string stateKey = "NpcControllerEditor." + key;
            bool isOpen = SessionState.GetBool(stateKey, defaultOpen);
            bool newOpen = EditorGUILayout.BeginFoldoutHeaderGroup(isOpen, title);
            EditorGUILayout.EndFoldoutHeaderGroup();

            if (newOpen != isOpen)
            {
                SessionState.SetBool(stateKey, newOpen);
            }

            return newOpen;
        }

        private void OnDisable()
        {
            DestroyImmediate(m_DefinitionEditor);
            DestroyImmediate(m_ProfileEditor);
        }
    }

    /// <summary>
    /// Операции с типом бота из редактора.
    /// </summary>
    public static class NpcTypeDefinitionTools
    {
        /// <summary>
        /// Копия типа бота рядом с исходным ассетом (имя - тип + имя бота), назначается только этому боту.
        /// </summary>
        public static NpcTypeDefinitionSO CloneForBot(NpcController npc)
        {
            var source = npc.Definition;

            if (source == null)
            {
                return null;
            }

            string sourcePath = AssetDatabase.GetAssetPath(source);
            string folder = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/') ?? "Assets";
            string copyPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{source.name}_{npc.name}.asset");

            if (!AssetDatabase.CopyAsset(sourcePath, copyPath))
            {
                Debug.LogError($"[NPC] Не удалось скопировать тип '{sourcePath}'.");
                return null;
            }

            var copy = AssetDatabase.LoadAssetAtPath<NpcTypeDefinitionSO>(copyPath);
            var serializedNpc = new SerializedObject(npc);
            serializedNpc.FindProperty("m_Definition").objectReferenceValue = copy;
            serializedNpc.ApplyModifiedProperties();
            Debug.Log($"[NPC] Для бота '{npc.name}' создан отдельный тип: {copyPath}", copy);
            return copy;
        }
    }
}
