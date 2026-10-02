using GBS.Data;
using GBS.Utility;
using UnityEditor;
using UnityEngine;

namespace GBS.Elements
{
    /// <summary>
    /// Нода, данные которой (встроенные действия/условия) редактируются через SerializedObject
    /// временного контейнера <see cref="GBSNodeEditorState"/>: поля рисуются стандартными PropertyField,
    /// а в ассет графа уходит копия данных по кнопке Save.
    /// </summary>
    public abstract class GBSDataNode<TData> : GBSNode where TData : GBSNodeData, new()
    {
        protected const string NodePath = GBSNodeEditorState.NodePropertyPath;

        private GBSNodeEditorState m_State;

        protected SerializedObject SerializedState { get; private set; }

        protected TData Data => (TData)m_State.Node;

        public override void LoadFrom(GBSNodeData data)
        {
            base.LoadFrom(data);

            if (data is TData typedData)
            {
                CreateState(typedData);
            }
        }

        public override GBSNodeData Save()
        {
            EnsureState();
            SerializedState.ApplyModifiedProperties();

            var data = (TData)m_State.CloneNode();
            PopulateBaseData(data);
            return data;
        }

        public override void Dispose()
        {
            SerializedState?.Dispose();
            SerializedState = null;

            if (m_State != null)
            {
                Object.DestroyImmediate(m_State);
                m_State = null;
            }
        }

        /// <summary>
        /// Новая нода (не из ассета) получает пустые данные.
        /// </summary>
        protected void EnsureState()
        {
            if (m_State == null)
            {
                CreateState(new TData());
            }
        }

        /// <summary>
        /// Вызывается для свежей копии данных до создания SerializedObject (перенос старого контента и т.п.).
        /// </summary>
        protected virtual void OnStateCreated(TData data)
        {
        }

        /// <summary>
        /// После прямых изменений C#-данных (Data) - обновить SerializedObject и привязанные поля.
        /// </summary>
        protected void SyncSerializedState()
        {
            SerializedState.Update();
        }

        /// <summary>
        /// Перед прямыми изменениями C#-данных - применить правки из привязанных полей.
        /// </summary>
        protected void ApplySerializedState()
        {
            SerializedState.ApplyModifiedProperties();
        }

        private void CreateState(TData source)
        {
            Dispose();

            m_State = GBSNodeEditorState.Create(source);
            OnStateCreated(Data);
            SerializedState = new SerializedObject(m_State);
        }
    }
}
