using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayOnceAudio : MonoBehaviour
{
    [SerializeField] private AudioSource m_AudioSource;
    
    private bool m_HasPlayed;
    
    public void PlayOnce()
    {
        if (m_HasPlayed)
            return;

        m_HasPlayed = true;
        m_AudioSource.Play();
    }
}