using System.Collections.Generic;
using UnityEngine;

namespace GBS.Events
{
    [CreateAssetMenu(fileName = "New Base Event", menuName = "GBS/Events/New Base Event", order = 51)]
    public class GBSEvent : ScriptableObject
    {
        private readonly List<IGameEventListener> m_eventListeners = new List<IGameEventListener>();

        [ContextMenu("Raise")]
        public void Raise()
        {
            for (int i = m_eventListeners.Count - 1; i >= 0; i--)
            {
                {
                    m_eventListeners[i].OnEventRaised();
                }
            }
        }

        //[MenuItem("UnityDev/GBS/Events/New Base Event")]
        //public static void CreateEvent()
        //{
        //    System.Type type = typeof(GBSEvent);
        //    System.Reflection.MethodInfo method = type.GetMethod("CreateMyAsset");
        //    var attributes = method.GetCustomAttributes(typeof(CreateAssetMenu), true);

        //    if (attributes.Length > 0)
        //    {
        //        CreateAssetMenu createAssetMenuAttribute = (CreateAssetMenu)attributes[0];
        //        EditorApplication.ExecuteMenuItem(createAssetMenuAttribute.menuName);
        //    }
        //}

        public void RegistersListeners(IGameEventListener listener)
        {
            if (!m_eventListeners.Contains(listener))
            {
                m_eventListeners.Add(listener);
            }
        }

        public void UnregisterListener(IGameEventListener listener)
        {
            if (m_eventListeners.Contains(listener))
            {
                m_eventListeners.Remove(listener);
            }
        }
    }

    public interface IGameEventListener
    {
        void OnEventRaised();
    }
}
