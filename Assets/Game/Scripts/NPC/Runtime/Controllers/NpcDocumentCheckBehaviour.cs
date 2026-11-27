using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.ItemInspection;
using Game.Scripts.NPC.Runtime.Interfaces;
using Game.Scripts.Story;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Конкретная стратегия взаимодействия: проверка документов бота (см.
    /// пример "мирный сотрудник газовой службы" / "мимик" из ТЗ). Вешается на
    /// GameObject рядом с NpcInteractionPoint и указывается в его m_Behaviour.
    ///
    /// Ничего не знает про FSM бота напрямую - только просит NpcController
    /// проиграть анимацию/звук по ключу и поднимает нужный StoryEventChannelSO,
    /// чтобы сюжет мог сделать развилку (StoryEventCondition на канал).
    /// Дальнейшую реакцию (SetAggressiveOn+ForceChase у мимика и т.д.) обычно
    /// вешают через StoryEventChannelListener на этот же канал - не здесь.
    ///
    /// Показ документов (назначены m_Documents): бот достаёт документы и протягивает их (m_ShowAnimationKey),
    /// через m_ShowDuration игрок их осматривает (<see cref="IItemInspection.TryInspectAndReturn"/> - при каждой
    /// проверке, документы не попадают ни в инвентарь, ни в список осмотренного), после осмотра документы
    /// возвращаются в руку бота, бот убирает их (m_PutAwayAnimationKey), и только тогда поднимается сигнал проверки.
    /// Документы не назначены - как раньше: анимация DocumentsValid/DocumentsForged и сигнал сразу.
    /// </summary>
    [JDIMonoController]
    public class NpcDocumentCheckBehaviour : MonoBehaviour, INpcInteractionBehaviour
    {
        [Tooltip("Если true - документы бота 'поддельные'/испорчены (мимик).")]
        [SerializeField] private bool m_HasForgedDocuments;

        [Tooltip("Сигнал сюжета, когда документы в порядке.")]
        [SerializeField, StoryKey(StoryKeyKind.Signal)] private string m_DocumentsValidSignal;

        [Tooltip("Сигнал сюжета, когда документы поддельные/испорченные.")]
        [SerializeField, StoryKey(StoryKeyKind.Signal)] private string m_DocumentsForgedSignal;

        [Header("Показ документов")]
        [Tooltip("Документы в руке бота (объект Documents под костью DocsSocket): их осматривает игрок, после осмотра " +
                 "они возвращаются на это же место. Пусто - бот документы не показывает.")]
        [SerializeField] private GameObject m_Documents;

        [Tooltip("Подпись под документами при осмотре.")]
        [SerializeField] private string m_DocumentsName = "Документы";

        [Tooltip("Ключ анимации (NpcAnimationSoundProfile) или имя состояния аниматора: бот достаёт документы и протягивает их.")]
        [SerializeField] private string m_ShowAnimationKey = "Docs_Show";

        [Tooltip("Через сколько секунд после начала показа документы уже в протянутой руке - тогда начинается осмотр.")]
        [SerializeField, Min(0f)] private float m_ShowDuration = 2f;

        [Tooltip("Ключ анимации (NpcAnimationSoundProfile) или имя состояния аниматора: бот убирает документы туда, " +
                 "откуда доставал.")]
        [SerializeField] private string m_PutAwayAnimationKey = "Docs_PutAway";

        [Header("Устаревшие SO-каналы (используйте сигналы выше)")]
        [Tooltip("Раздаётся, когда документы в порядке.")]
        [SerializeField] private StoryEventChannelSO m_OnDocumentsValidChannel;

        [Tooltip("Раздаётся, когда документы поддельные/испорченные.")]
        [SerializeField] private StoryEventChannelSO m_OnDocumentsForgedChannel;

        [Inject] private IItemInspection m_Inspection;

        private bool m_IsShowingDocuments;

        public void OnPlayerInteracted(NpcController controller)
        {
            // Бот уже показывает документы - повторное нажатие ничего не делает.
            if (m_IsShowingDocuments)
            {
                return;
            }

            if (m_Documents == null || m_Inspection == null)
            {
                controller.PlayAnimationSound(m_HasForgedDocuments ? "DocumentsForged" : "DocumentsValid");
                RaiseChecked();
                return;
            }

            ShowDocumentsAsync(controller, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid ShowDocumentsAsync(NpcController controller, CancellationToken token)
        {
            m_IsShowingDocuments = true;
            controller.PlayAnimationSound(m_ShowAnimationKey);

            if (await UniTask.Delay(TimeSpan.FromSeconds(m_ShowDuration), cancellationToken: token)
                    .SuppressCancellationThrow())
            {
                return;
            }

            // Осмотр сейчас невозможен (открыта панель с курсором, игрок обездвижен) - бот просто убирает документы.
            if (!m_Inspection.TryInspectAndReturn(m_Documents, m_DocumentsName, () => OnDocumentsInspected(controller)))
            {
                PutAway(controller);
            }
        }

        private void OnDocumentsInspected(NpcController controller)
        {
            PutAway(controller);
            RaiseChecked();
        }

        private void PutAway(NpcController controller)
        {
            m_IsShowingDocuments = false;

            if (controller != null)
            {
                controller.PlayAnimationSound(m_PutAwayAnimationKey);
            }
        }

        private void RaiseChecked()
        {
            if (m_HasForgedDocuments)
            {
                StorySignals.Raise(m_DocumentsForgedSignal);
                m_OnDocumentsForgedChannel?.Raise();
            }
            else
            {
                StorySignals.Raise(m_DocumentsValidSignal);
                m_OnDocumentsValidChannel?.Raise();
            }
        }
    }
}
