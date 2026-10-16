using System;
using System.Collections.Generic;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.LocationState.Runtime.Controllers
{
    /// <summary>
    /// Сохраняет, включены ли объекты. Для выключателя света: вешается на выключатель, в Targets - лампы,
    /// которые он включает и выключает (ObjectToggler). После загрузки локации лампы сразу стоят включёнными
    /// или выключенными - кнопка выключателя не нажимается и не щёлкает.
    /// Подходит и для любых других объектов, которые во время игры включаются и выключаются.
    /// </summary>
    [JDIMonoController]
    public class ObjectActiveStateSaver : LocationStateSaver
    {
        [Serializable]
        private class State
        {
            public List<bool> IsActive = new();
        }

        [Tooltip("Объекты, включённость которых сохраняется (например, лампы выключателя). Пусто - этот объект.")]
        [SerializeField] private GameObject[] m_Targets;

        private readonly List<bool> m_IsActiveInScene = new();

        public override string CaptureState()
        {
            var state = new State();
            bool isAsInScene = true;
            var targets = GetTargets();

            for (int i = 0; i < targets.Length; i++)
            {
                bool isActive = targets[i] != null && targets[i].activeSelf;
                state.IsActive.Add(isActive);
                isAsInScene &= i < m_IsActiveInScene.Count && m_IsActiveInScene[i] == isActive;
            }

            return isAsInScene ? null : JsonUtility.ToJson(state);
        }

        protected override void RememberSceneState()
        {
            m_IsActiveInScene.Clear();

            foreach (var target in GetTargets())
            {
                m_IsActiveInScene.Add(target != null && target.activeSelf);
            }
        }

        public override void ApplySavedState(string state)
        {
            var saved = JsonUtility.FromJson<State>(state);
            var targets = GetTargets();

            if (saved.IsActive.Count != targets.Length)
            {
                Debug.LogWarning($"[LocationState] ObjectActiveStateSaver '{name}': в сохранении {saved.IsActive.Count} объектов, " +
                                 $"а в Targets {targets.Length} - список менялся, восстанавливаю по порядку.", this);
            }

            for (int i = 0; i < targets.Length && i < saved.IsActive.Count; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].SetActive(saved.IsActive[i]);
                }
            }
        }

        private GameObject[] GetTargets()
        {
            return m_Targets != null && m_Targets.Length > 0 ? m_Targets : new[] { gameObject };
        }
    }
}
