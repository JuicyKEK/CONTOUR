using JuicyDI.Attributes;
using UnityEngine;

/// <summary>
/// 3D-модель кассетного плеера в руках игрока. Показывается, пока открыта панель плеера
/// (управляет CassettePlayerController, получает вид через [Inject]); при появлении проигрывает анимацию.
/// </summary>
[JDIMonoController]
public class CassettePlayerModelView : MonoBehaviour
{
    [SerializeField] private Animator anim;

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    private void OnEnable()
    {
        if (anim != null)
        {
            anim.Play("CassetPlayer", -1, 0f);
        }
    }
}
