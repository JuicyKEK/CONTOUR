using System;
using Game.Scripts.InputController;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.ItemInspection.Controllers
{
    /// <summary>
    /// Связь модуля осмотра с взаимодействием игрока: когда игрок первый раз подбирает предмет этого типа, сначала
    /// показывается осмотр (<see cref="IItemInspection"/>), а сам подбор (Interact предмета - в инвентарь, в кассеты...)
    /// выполняется, когда предмет долетел. Следующие предметы того же типа подбираются сразу.
    ///
    /// Вешается на подбираемый предмет рядом с его IInteraction. PlayerInteractiveController спрашивает его перед
    /// взаимодействием (<see cref="IInteractionInterceptor"/>). Какие типы уже осмотрены, хранит ItemInspectionRegistry
    /// и сохраняет ItemInspectionSaveController - отдельно от инвентаря.
    ///
    /// Тип и подпись - поля ниже, либо их выбирает компонент-источник на том же объекте (<see cref="IItemInspectionInfoSource"/>,
    /// например AudioTapeInspectionInfo: обычная и злая кассета - разные типы).
    /// </summary>
    [JDIMonoController]
    [DisallowMultipleComponent]
    public class InspectOnFirstPickup : MonoBehaviour, IInteractionInterceptor
    {
        [Tooltip("Тип предмета: осматривается только первый подобранный предмет каждого типа. Например, ключ предмета " +
                 "инвентаря (Spray with vinegar). Пусто - осмотра нет.")]
        [SerializeField] private string m_TypeKey;
        [Tooltip("Подпись под предметом при осмотре. Пусто - тип предмета.")]
        [SerializeField] private string m_DisplayName;
        [Tooltip("Включено - осматривается только первый предмет типа. Выключено - этот предмет осматривается при каждом подборе.")]
        [SerializeField] private bool m_OnlyFirstOfType = true;

        [Inject] private IItemInspection m_Inspection;
        [Inject] private IItemInspectionRegistry m_Registry;

        private IItemInspectionInfoSource m_InfoSource;
        private IItemInspectionFlyTargetSource m_FlyTargetSource;

        public string TypeKey
        {
            get
            {
                GetInfo(out string typeKey, out _);
                return typeKey;
            }
        }

        public bool TryIntercept(Action proceed)
        {
            if (!isActiveAndEnabled || m_Inspection == null)
            {
                return false;
            }

            GetInfo(out string typeKey, out string displayName);

            if (string.IsNullOrEmpty(typeKey))
            {
                return false;
            }

            if (m_OnlyFirstOfType && m_Registry != null && m_Registry.WasInspected(typeKey))
            {
                return false;
            }

            m_FlyTargetSource ??= GetComponent<IItemInspectionFlyTargetSource>();
            Transform flyTarget = m_FlyTargetSource?.GetFlyTarget();

            if (!m_Inspection.TryInspect(gameObject, string.IsNullOrEmpty(displayName) ? typeKey : displayName,
                    flyTarget, proceed))
            {
                return false;
            }

            m_Registry?.MarkInspected(typeKey);
            return true;
        }

        private void GetInfo(out string typeKey, out string displayName)
        {
            m_InfoSource ??= GetComponent<IItemInspectionInfoSource>();

            if (m_InfoSource != null && m_InfoSource.TryGetInspectionInfo(out typeKey, out displayName))
            {
                return;
            }

            typeKey = m_TypeKey;
            displayName = m_DisplayName;
        }
    }
}
