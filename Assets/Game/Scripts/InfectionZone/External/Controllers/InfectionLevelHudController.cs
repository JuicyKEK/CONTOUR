using DG.Tweening;
using Game.Scripts.InfectionZone.External.View;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
// ReSharper disable once RedundantUsingDirective
using JuicyDI;
using JuicyDI.Attributes;
using JuicyDI.Context;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Управляет показом/скрытием HUD уровня заражения (см. <see cref="IInfectionLevelHud"/>).
    /// Регистрируется как глобальный бин JuicyDI, чтобы триггеры зон на любой сцене могли
    /// получить его через [Inject], не имея прямой ссылки на объект HUD.
    ///
    /// Два сценария показа:
    ///  - <see cref="ShowCurrentLevel"/> - вход игрока в локацию: слайдер сразу выставляется
    ///    на текущий уровень заражения, анимации значения нет;
    ///  - <see cref="ShowLevelChange"/> - изменение заражения зоны (например очистка): слайдер
    ///    сначала показывает старое значение, затем плавно "утекает" к новому.
    /// В обоих случаях HUD появляется через fade, ждёт m_AutoHideDelay секунд и прячется.
    /// </summary>
    [JDIMonoController(Context = typeof(GlobalBean))]
    [SequenceParticipant(100)]
    public class InfectionLevelHudController : MonoBehaviour, IInfectionLevelHud, ISequence
    {
        [Header("Вид")]
        [SerializeField] private InfectionLevelHudView m_View;

        [Header("Тайминги")]
        [Tooltip("Длительность плавного появления/скрытия всего блока HUD.")]
        [SerializeField] private float m_FadeDuration = 0.25f;

        [Tooltip("Длительность анимации перехода слайдера от старого значения к новому.")]
        [SerializeField] private float m_TransitionDuration = 1.2f;

        [Tooltip("Сколько секунд HUD остаётся на экране (после появления/окончания анимации значения), прежде чем скрыться.")]
        [SerializeField] private float m_AutoHideDelay = 5f;

        private Sequence m_Sequence;

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            // Прячем HUD сразу и без анимации - до первого реального вызова Show*
            // он не должен быть виден на экране.
            m_View.SetVisibleInstant(false);
        }

        /// <summary>
        /// Вход игрока в локацию - показываем текущий уровень заражения без анимации значения.
        /// </summary>
        public void ShowCurrentLevel(string zoneDisplayName, float level)
        {
            m_Sequence?.Kill();

            m_View.SetTitle(zoneDisplayName);
            m_View.SetSliderValueImmediate(level);

            m_Sequence = DOTween.Sequence()
                .Append(m_View.FadeTween(true, m_FadeDuration))
                .AppendInterval(m_AutoHideDelay)
                .Append(m_View.FadeTween(false, m_FadeDuration));
        }

        /// <summary>
        /// Уровень заражения зоны изменился, пока игрок в ней находится - показываем переход
        /// от старого значения к новому.
        /// </summary>
        public void ShowLevelChange(string zoneDisplayName, float fromLevel, float toLevel)
        {
            m_Sequence?.Kill();

            m_View.SetTitle(zoneDisplayName);

            m_Sequence = DOTween.Sequence()
                .Append(m_View.FadeTween(true, m_FadeDuration))
                .Append(m_View.SliderValueTween(fromLevel, toLevel, m_TransitionDuration))
                .AppendInterval(m_AutoHideDelay)
                .Append(m_View.FadeTween(false, m_FadeDuration));
        }

        private void OnDestroy()
        {
            m_Sequence?.Kill();
        }
    }
}


