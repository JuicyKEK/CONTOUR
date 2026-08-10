using System;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Story
{
    [Serializable]
    public class StoryCameraEntry
    {
        [SerializeField] private string m_Key;
        [SerializeField] private Camera m_Camera;

        public string Key => m_Key;
        public Camera Camera => m_Camera;
    }

    /// <summary>
    /// Держит реестр камер сцены (игрока, катсценовых) и переключает активную
    /// по ключу. Используется совместно с IScreenFader для плавного перехода.
    /// </summary>
    [JDIMonoController]
    public class CameraDirector : MonoBehaviour, ICameraDirector
    {
        [SerializeField] private StoryCameraEntry[] m_Cameras;

        public void SetActiveCamera(string key)
        {
            if (m_Cameras == null)
            {
                return;
            }

            foreach (var entry in m_Cameras)
            {
                if (entry?.Camera == null)
                {
                    continue;
                }

                entry.Camera.gameObject.SetActive(entry.Key == key);
            }
        }
    }
}

