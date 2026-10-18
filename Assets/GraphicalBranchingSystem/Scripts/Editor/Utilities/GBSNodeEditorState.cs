using GBS.Data;
using UnityEngine;

namespace GBS.Utility
{
    /// <summary>
    /// Временный (не сохраняемый) контейнер данных одной ноды в окне редактора. Даёт SerializedObject,
    /// поэтому поля встроенных действий/условий ([SerializeReference]) рисуются стандартными
    /// PropertyField со всеми их атрибутами ([Tooltip], [TextArea], [StoryKey]...).
    /// Данные - глубокая копия: ассет графа меняется только по кнопке Save.
    /// </summary>
    public class GBSNodeEditorState : ScriptableObject
    {
        public const string NodePropertyPath = nameof(m_Node);

        [SerializeReference] private GBSNodeData m_Node;

        public GBSNodeData Node => m_Node;

        // ВАЖНО: не HideAndDontSave - он включает NotEditable, и тогда все PropertyField,
        // привязанные к контейнеру, становятся read-only (поля в нодах не редактируются).
        private const HideFlags StateHideFlags = HideFlags.DontSave;

        public static GBSNodeEditorState Create(GBSNodeData source)
        {
            var state = CreateInstance<GBSNodeEditorState>();
            state.hideFlags = StateHideFlags;
            state.m_Node = source;

            // Instantiate копирует сериализованные данные (включая [SerializeReference]) -
            // так получаем независимую копию данных ноды.
            var copy = Instantiate(state);
            copy.hideFlags = StateHideFlags;
            DestroyImmediate(state);
            return copy;
        }

        /// <summary>
        /// Независимая копия текущих данных ноды (для сохранения в ассет графа).
        /// </summary>
        public GBSNodeData CloneNode()
        {
            var copy = Instantiate(this);
            var node = copy.m_Node;
            DestroyImmediate(copy);
            return node;
        }
    }
}
