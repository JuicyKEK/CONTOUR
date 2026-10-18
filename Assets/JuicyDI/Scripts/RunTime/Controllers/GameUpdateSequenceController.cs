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
            m_SortedSequence = sequence == null
                ? new List<IUpdateSequence>()
                : sequence
                    .Where(element => element != null)
                    .OrderBy(element => element.GetType().GetCustomAttribute<SequenceParticipant>()?.Number ?? 0)
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