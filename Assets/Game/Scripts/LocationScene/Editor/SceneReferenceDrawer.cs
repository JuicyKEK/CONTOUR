using Game.Scripts.LocationScene.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.LocationScene.EditorTools
{
    /// <summary>
    /// Поле ссылки на сцену (<see cref="SceneReference"/>) в инспекторе: одно поле с ассетом сцены
    /// и предупреждение под ним, если сцена не добавлена в Build Settings (такую сцену игра не загрузит).
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        private const string SceneAssetProperty = "m_SceneAsset";
        private const string ScenePathProperty = "m_ScenePath";
        private const float WarningHeight = 30f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;

            if (IsMissingInBuildSettings(property))
            {
                height += EditorGUIUtility.standardVerticalSpacing + WarningHeight;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var sceneAsset = property.FindPropertyRelative(SceneAssetProperty);
            var scenePath = property.FindPropertyRelative(ScenePathProperty);

            var fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(fieldRect, label, property);
            EditorGUI.BeginChangeCheck();

            var newSceneAsset = EditorGUI.ObjectField(fieldRect, label, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);

            if (EditorGUI.EndChangeCheck())
            {
                sceneAsset.objectReferenceValue = newSceneAsset;
                scenePath.stringValue = newSceneAsset != null ? AssetDatabase.GetAssetPath(newSceneAsset) : string.Empty;
            }

            EditorGUI.EndProperty();

            if (IsMissingInBuildSettings(property))
            {
                var warningRect = new Rect(
                    position.x,
                    fieldRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                    position.width,
                    WarningHeight);

                EditorGUI.HelpBox(warningRect, "Сцена не добавлена в Build Settings (или выключена там) - игра её не загрузит.", MessageType.Warning);
            }
        }

        private static bool IsMissingInBuildSettings(SerializedProperty property)
        {
            var sceneAsset = property.FindPropertyRelative(SceneAssetProperty).objectReferenceValue;

            if (sceneAsset == null)
            {
                return false;
            }

            string path = AssetDatabase.GetAssetPath(sceneAsset);

            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && scene.path == path)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
