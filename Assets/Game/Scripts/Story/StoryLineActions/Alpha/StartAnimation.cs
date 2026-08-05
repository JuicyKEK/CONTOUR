using UnityEngine;

public class StartAnimation : MonoBehaviour
{
    [SerializeField] private Animator m_Animator;

    public void PlayaAnimation(string animationName)
    {
        m_Animator.Play(animationName);
    }
}
