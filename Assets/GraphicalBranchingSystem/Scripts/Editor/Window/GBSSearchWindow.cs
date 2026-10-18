using System.Collections.Generic;
using GBS.Data;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GBS.Windows
{
    /// <summary>
    /// Меню создания нод по пробелу / правому клику на пустом месте.
    /// </summary>
    public class GBSSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private GBSGraphView m_GraphView;

        public void Init(GBSGraphView graphView)
        {
            m_GraphView = graphView;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            return new List<SearchTreeEntry>()
            {
                new SearchTreeGroupEntry(new GUIContent("Create Element")),

                new SearchTreeGroupEntry(new GUIContent("Story Flow"), 1),
                new SearchTreeEntry(new GUIContent("Start")) { level = 2, userData = GBSNodeType.Start },
                new SearchTreeEntry(new GUIContent("Story Node")) { level = 2, userData = GBSNodeType.Story },
                new SearchTreeEntry(new GUIContent("End")) { level = 2, userData = GBSNodeType.End },

                new SearchTreeGroupEntry(new GUIContent("Logic"), 1),
                new SearchTreeEntry(new GUIContent("Condition")) { level = 2, userData = GBSNodeType.Condition },
                new SearchTreeEntry(new GUIContent("Boolean Operation")) { level = 2, userData = GBSNodeType.Logic },
                new SearchTreeEntry(new GUIContent("Graph Completed")) { level = 2, userData = GBSNodeType.GraphCompleted }
            };
        }

        public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
        {
            if (searchTreeEntry.userData is not GBSNodeType nodeType)
            {
                return false;
            }

            var localMousePosition = m_GraphView.GetLocalMousePosition(context.screenMousePosition, true);

            m_GraphView.CreateNode(nodeType, localMousePosition);

            return true;
        }
    }
}
