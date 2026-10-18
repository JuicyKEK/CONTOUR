using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JuicyDI.Attributes;

namespace JuicyDI
{
    public class GameSequenceController
    {
        public List<ISequence> SortedSequence => m_SortedSequence;
        
        private List<ISequence> m_SortedSequence;
        
        public GameSequenceController(List<ISequence> sequenceElements)
        {
            m_SortedSequence = sequenceElements == null
                ? new List<ISequence>()
                : sequenceElements
                    .Where(element => element != null)
                    .OrderBy(GetOrder)
                    .ToList();

            for (int i = 0; i < m_SortedSequence.Count; i++)
            {
                m_SortedSequence[i].MethodInit();
            }            
            
            for (int i = 0; i < m_SortedSequence.Count; i++)
            {
                m_SortedSequence[i].MethodStart();
            }
        }

        private static int GetOrder(ISequence element)
        {
            var attribute = element.GetType().GetCustomAttribute<SequenceParticipant>();
            return attribute?.Number ?? 0;
        }
    }
}