using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JuicyDI.Attributes;

namespace JuicyDI
{
        public class GameUpdateSequenceController : IGameUpdateSequenceController
        {
            
        private List<IUpdateSequence> m_SortedSequence;

        public GameUpdateSequenceController(List<IUpdateSequence> sequence)
        {
            m_SortedSequence = sequence
                .OrderBy(type => type.GetType().GetCustomAttribute<SequenceParticipant>().Number)
                .ToList();
        }
        
        public void UpdateSequence()
        {
            for (int i = 0; i < m_SortedSequence.Count; i++)
            {
                m_SortedSequence[i].CustomUpdate();
            }
        }
    }
}