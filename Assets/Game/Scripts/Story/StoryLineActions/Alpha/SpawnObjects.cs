using UnityEngine;

namespace Game.Scripts.Story.StoryLineActions.Alpha
{
    public class SpawnObjects : MonoBehaviour
    {
        [SerializeField] private GameObject[] m_Objects;

        public void Spawn()
        {
            if (m_Objects == null || m_Objects.Length == 0)
            {
                return;
            }
            
            for (int i = 0; i < m_Objects.Length; i++)
            {
                m_Objects[i].SetActive(true);
            }
        }
    }
}