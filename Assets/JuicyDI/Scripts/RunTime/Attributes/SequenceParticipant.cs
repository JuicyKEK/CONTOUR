using System;

namespace JuicyDI.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class SequenceParticipant : Attribute
    {
        private int m_NumberInSequence;

        public int Number
        {
            get => m_NumberInSequence;
        }
        
        public SequenceParticipant(int number)
        {
            m_NumberInSequence = number;
        }
    }
}