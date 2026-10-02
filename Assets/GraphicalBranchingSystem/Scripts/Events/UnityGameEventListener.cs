using GBS.Events;
using UnityEngine;
using UnityEngine.Events;

namespace GBS
{
    public class UnityGameEventListener : MonoBehaviour, IGameEventListener
    {
        [SerializeField]
        private GBSEvent eventTest;
        [SerializeField]
        private UnityEvent response;

        public void OnEnable()
        {
            if (eventTest != null)
            {
                eventTest.RegistersListeners(this);
            }
        }

        private void OnDisable()
        {
            if (eventTest != null)
            {
                eventTest.UnregisterListener(this);
            }
        }

        public void OnEventRaised()
        {
            response?.Invoke();
        }
    }
}
