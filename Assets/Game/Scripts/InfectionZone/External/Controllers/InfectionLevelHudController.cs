using System;
using DG.Tweening;
using Game.Scripts.InfectionZone.External.View;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using JuicyDI;
using JuicyDI.Attributes;
using R3;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.Controllers
{
    /// <summary>
    /// Управляет показом/скрытием HUD уровня заражения (см. <see cref="IInfectionLevelHud"/>).
    /// Бин сцены JuicyDI: вид (<see cref="InfectionLevelHudView"/>) и трекер текущей зоны игрока
    /// приходят через [Inject] - на сцене они должны быть в одном экземпляре.
    ///
    /// Два сценария показа:
    ///  - <see cref="ShowCurrentLevel"/> - игрок перешёл в другую зону (сам контроллер следит за
    ///    <see cref="IPlayerInfectionZoneTracker.CurrentZone"/>): слайдер сразу выставляется
    ///    на текущий уровень заражения, анимации значения нет;
    ///  - <see cref="ShowLevelChange"/> - изменение заражения зоны (например очистка): слайдер
    ///    сначала показывает старое значение, затем плавно "утекает" к новому.
    /// В обоих случаях HUD появляется через fade, ждёт m_AutoHideDelay секунд и прячется.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(100)]
    public class InfectionLevelHudController : MonoBehaviour, IInfectionLevelHud, ISequence
    {
        [Header("Тайминги")]
        [Tooltip("Длительность плавного появления/скрытия всего блока HUD.")]
        [SerializeField] private float m_FadeDuration = 0.25f;

        [Tooltip("Длительность анимации перехода слайдера от старого значения к новому.")]
        [SerializeField] private float m_TransitionDuration = 1.2f;

        [Tooltip("Сколько секунд HUD остаётся на экране (после появления/окончания анимации значения), прежде чем скрыться.")]
        [SerializeField] private float m_AutoHideDelay = 5f;

        [Inject] private InfectionLevelHudView m_View;
        [Inject] private IPlayerInfectionZoneTracker m_ZoneTracker;

        private Sequence m_Sequence;
        private IDisposable m_CurrentZoneSubscription;

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            // Прячем HUD сразу и без анимации - до первого реального вызова Show*
            // он не должен быть виден на экране.
            m_View.SetVisibleInstant(false);

            // Без трекера на сцене JuicyDI уже написал ошибку резолва - не роняем остальной старт сцены.
            if (m_ZoneTracker == null)
            {
                return;
            }

            // Игрок перешёл в другую зону - показываем её текущий уровень заражения.
            m_CurrentZoneSubscription = m_ZoneTracker.CurrentZone
                .Where(zone => zone != null)
                .Subscribe(zone => ShowCurrentLevel(zone.DisplayName, zone.InfectionLevel.CurrentValue));
        }

        /// <summary>
        /// Вход игрока в зону - показываем текущий уровень заражения без анимации значения.
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
            m_CurrentZoneSubscription?.Dispose();
        }
    }
}



