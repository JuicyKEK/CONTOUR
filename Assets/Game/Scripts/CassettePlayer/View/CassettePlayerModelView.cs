using UnityEngine;

public class CassettePlayerModelView : MonoBehaviour
{
    [SerializeField] private Animator anim;
    
    private void OnEnable()
    {
        if (anim != null)
        {
            anim.Play("CassetPlayer", -1, 0f);
        }
    }
}
