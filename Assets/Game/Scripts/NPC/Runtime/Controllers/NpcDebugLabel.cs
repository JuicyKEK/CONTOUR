using System.Text;
using Game.Scripts.NPC.Runtime.Core;
using TMPro;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Отладочная подпись над ботом прямо в игре: состояние, агрессия, шкала злости, видит ли игрока.
    /// Для тестовых сцен и настройки поведения; в обычных сценах её просто не добавляют (или выключают).
    /// </summary>
    public class NpcDebugLabel : MonoBehaviour
    {
        [SerializeField] private NpcController m_Npc;
        [Tooltip("Текст подписи. Пусто - создаётся сам.")]
        [SerializeField] private TMP_Text m_Text;
        [SerializeField] private float m_Height = 2.7f;
        [SerializeField, Min(0.05f)] private float m_RefreshInterval = 0.15f;

        private readonly StringBuilder m_Builder = new();
        private float m_RefreshTimer;
        private Camera m_Camera;

        private void Awake()
        {
            if (m_Npc == null)
            {
                m_Npc = GetComponentInParent<NpcController>();
            }

            if (m_Text == null)
            {
                var label = new GameObject("DebugLabel");
                label.transform.SetParent(transform, false);
                var text = label.AddComponent<TextMeshPro>();
                text.fontSize = 2.4f;
                text.alignment = TextAlignmentOptions.Center;
                text.rectTransform.sizeDelta = new Vector2(4f, 1.5f);
                m_Text = text;
            }
        }

        private void LateUpdate()
        {
            if (m_Npc == null || m_Text == null)
            {
                return;
            }

            if (m_Camera == null || !m_Camera.isActiveAndEnabled)
            {
                m_Camera = Camera.main;
            }

            var labelTransform = m_Text.transform;
            labelTransform.position = m_Npc.transform.position + Vector3.up * m_Height;

            if (m_Camera != null)
            {
                // Подпись всегда лицом к камере.
                labelTransform.rotation = Quaternion.LookRotation(labelTransform.position - m_Camera.transform.position);
            }

            m_RefreshTimer -= Time.deltaTime;

            if (m_RefreshTimer > 0f)
            {
                return;
            }

            m_RefreshTimer = m_RefreshInterval;
            m_Builder.Clear();
            m_Builder.Append(StateName(m_Npc.CurrentState));

            if (m_Npc.IsHunting)
            {
                m_Builder.Append(" (сюжетная)");
            }

            m_Builder.Append('\n');
            m_Builder.Append(m_Npc.IsAggressive ? "<color=#ff5050>агрессивен</color>" : "спокоен");

            if (m_Npc.Definition != null && m_Npc.Definition.AggressionMode == Data.NpcAggressionMode.AngerMeter)
            {
                m_Builder.Append(" · злость ").Append(Mathf.RoundToInt(m_Npc.Anger * 100f)).Append('%');
            }

            if (m_Npc.CanSeePlayer)
            {
                m_Builder.Append("\n<color=#ffd040>видит игрока</color>");
            }

            m_Text.text = m_Builder.ToString();
        }

        private static string StateName(NpcStateId state)
        {
            switch (state)
            {
                case NpcStateId.Idle: return "стоит";
                case NpcStateId.Observe: return "наблюдает";
                case NpcStateId.Patrol: return "обход / роль";
                case NpcStateId.Chase: return "погоня";
                case NpcStateId.Panic: return "паника";
                case NpcStateId.Wander: return "бродит";
                case NpcStateId.Search: return "ищет";
                case NpcStateId.Attack: return "атакует";
                case NpcStateId.Perform: return "действие";
                case NpcStateId.MoveTo: return "идёт к точке";
                default: return state.ToString();
            }
        }
    }
}
