using System.Collections.Generic;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Game.GameStarter
{
    [JDIMonoController]
    public class TestRoomStarter : MonoBehaviour
    {
        [Inject] private List<ISequence> m_StartSequence;
        [Inject] private List<IUpdateSequence> m_UpdateSequence;

        [SerializeField] private MainJDIController m_MainJDIController;
        
        //private GameSequenceController m_GameSequenceController;
        private IGameUpdateSequenceController m_GameUpdateSequenceController;
        
        private void Start()
        {
            m_MainJDIController.Init();
            _ = new GameSequenceController(m_StartSequence);
            m_GameUpdateSequenceController = new GameUpdateSequenceController(m_UpdateSequence);
        }

        private void Update()
        {
            m_GameUpdateSequenceController.UpdateSequence();
        }
    }
}
