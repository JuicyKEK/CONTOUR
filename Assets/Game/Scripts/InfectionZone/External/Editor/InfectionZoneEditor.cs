using Game.Scripts.InfectionZone.Runtime.Controllers;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.InfectionZone.Editor
{
    /// <summary>
    /// Тестовый инструмент для отладки механики заражения зон.
    /// В плей-моде показывает в инспекторе слайдер и кнопки-пресеты, позволяющие
    /// вручную менять степень заражения зоны и сразу видеть, как появляются/исчезают
    /// интерактивные объекты очистки и объекты-аномалии, привязанные к этой зоне.
    /// </summary>
    [CustomEditor(typeof(Runtime.Controllers.InfectionZone))]
    public class InfectionZoneEditor : UnityEditor.Editor
    {
        private float m_DebugInfectionValue;

        public override void OnInspectorGUI()
        {
            // Стандартный инспектор со всеми сериализованными полями (ZoneId, стартовое заражение и т.д.)
            DrawDefaultInspector();

            var zone = (Runtime.Controllers.InfectionZone)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Тестирование заражения (доступно только в Play Mode)", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Запустите Play Mode, чтобы протестировать изменение степени заражения зоны в реальном времени.", MessageType.Info);
                return;
            }

            float currentValue = zone.InfectionLevel.CurrentValue;
            EditorGUILayout.LabelField("Текущая степень заражения", $"{currentValue:0.0}%");

            EditorGUI.BeginChangeCheck();
            m_DebugInfectionValue = EditorGUILayout.Slider("Установить заражение", currentValue, 0f, 100f);
            if (EditorGUI.EndChangeCheck())
            {
                zone.SetInfection(m_DebugInfectionValue);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Быстрые пресеты", EditorStyles.miniBoldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("0%"))
                {
                    zone.SetInfection(0f);
                }
                if (GUILayout.Button("30%"))
                {
                    zone.SetInfection(30f);
                }
                if (GUILayout.Button("50%"))
                {
                    zone.SetInfection(50f);
                }
                if (GUILayout.Button("70%"))
                {
                    zone.SetInfection(70f);
                }
                if (GUILayout.Button("100%"))
                {
                    zone.SetInfection(100f);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+10%"))
                {
                    zone.IncreaseInfection(10f);
                }
                if (GUILayout.Button("-10%"))
                {
                    zone.DecreaseInfection(10f);
                }
            }

            // Инспектор должен перерисовываться каждый кадр в Play Mode, чтобы отражать
            // изменения заражения, происходящие из кода (сюжетные события, очистка игроком).
            Repaint();
        }
    }
}
