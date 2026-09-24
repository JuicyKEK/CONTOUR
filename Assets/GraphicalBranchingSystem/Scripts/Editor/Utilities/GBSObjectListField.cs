using System.Collections.Generic;
using UnityEngine.UIElements;
namespace GBS.Utility
{
    /// <summary>
    /// Переиспользуемый блок "список ассетов" с кнопками добавления и удаления.
    /// Используется для списков StoryAction / GBSEvent в нодах.
    ///
    /// Каждая строка: [индекс] [поле ассета] [X]. Кнопка X имеет фиксированную
    /// ширину и не может уехать за границу ноды.
    /// </summary>
    public class GBSObjectListField<T> where T : UnityEngine.Object
    {
        private readonly List<T> m_Items;
        private readonly VisualElement m_ItemsContainer;
        private readonly Foldout m_Root;
        public GBSObjectListField(string title, List<T> items, string addButtonText, bool collapsed = false)
        {
            m_Items = items;
            m_Root = GBSElementUtility.CreateFoldout(title, collapsed);
            m_ItemsContainer = GBSElementUtility.CreateColumn();
            m_Root.Add(m_ItemsContainer);
            var buttonsRow = GBSElementUtility.CreateRow();
            var addButton = GBSElementUtility.CreateButton(addButtonText, () => Add());
            addButton.style.flexGrow = 1;
            buttonsRow.Add(addButton);
            buttonsRow.Add(GBSElementUtility.CreateDeleteButton(RemoveLast, "Удалить последний элемент"));
            m_Root.Add(buttonsRow);
            Rebuild();
        }
        public VisualElement Root => m_Root;
        public void Add(T item = null)
        {
            m_Items.Add(item);
            Rebuild();
        }
        public void RemoveLast()
        {
            if (m_Items.Count == 0)
            {
                return;
            }
            m_Items.RemoveAt(m_Items.Count - 1);
            m_ItemsContainer.schedule.Execute(Rebuild);
        }
        public void Rebuild()
        {
            m_ItemsContainer.Clear();
            for (int i = 0; i < m_Items.Count; i++)
            {
                var index = i;
                var row = GBSElementUtility.CreateRow();
                row.Add(GBSElementUtility.CreateInlineLabel(index.ToString(), 18f));
                row.Add(GBSElementUtility.CreateObjectField(
                    typeof(T),
                    m_Items[index],
                    callback =>
                    {
                        if (index < m_Items.Count)
                        {
                            m_Items[index] = callback.newValue as T;
                        }
                    }));
                row.Add(GBSElementUtility.CreateDeleteButton(() =>
                {
                    if (index >= m_Items.Count)
                    {
                        return;
                    }
                    m_Items.RemoveAt(index);
                    // Перестраиваем на следующем кадре, чтобы не удалять элементы
                    // прямо во время обработки клика по кнопке.
                    m_ItemsContainer.schedule.Execute(Rebuild);
                }));
                m_ItemsContainer.Add(row);
            }
        }
    }
}
