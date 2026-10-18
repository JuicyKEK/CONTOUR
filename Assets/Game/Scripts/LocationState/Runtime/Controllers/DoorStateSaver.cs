using System;
using Game.Scripts.InteractionObjects.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.LocationState.Runtime.Controllers
{
    /// <summary>
    /// Сохраняет, открыта ли дверь и заперта ли она защёлкой. Работает с InteractionDoor и NonInteractionDoor
    /// (<see cref="IRestorableDoor"/>). После загрузки локации дверь и защёлка сразу стоят в сохранённом
    /// положении - без анимации и без звука.
    ///
    /// Не вешайте на двери, которыми управляет что-то другое при каждой загрузке (например, дверь в конце
    /// перехода, которую открывает LocationLoadEvents), - иначе сохранённое состояние и то управление будут спорить.
    /// </summary>
    [JDIMonoController]
    public class DoorStateSaver : LocationStateSaver
    {
        [Serializable]
        private class State
        {
            public bool IsOpen;
            public bool IsLocked;
        }

        [Tooltip("Дверь (InteractionDoor или NonInteractionDoor). Пусто - дверь на этом же объекте.")]
        [SerializeField] private MonoBehaviour m_Door;

        private IRestorableDoor m_RestorableDoor;
        private bool m_IsOpenInScene;
        private bool m_IsLockedInScene;

        private IRestorableDoor Door => m_RestorableDoor ??= m_Door as IRestorableDoor ?? GetComponent<IRestorableDoor>();

        public override string CaptureState()
        {
            if (!HasDoor())
            {
                return null;
            }

            Door.GetState(out bool isOpen, out bool isLocked);

            return isOpen == m_IsOpenInScene && isLocked == m_IsLockedInScene
                ? null
                : JsonUtility.ToJson(new State { IsOpen = isOpen, IsLocked = isLocked });
        }

        public override void ApplySavedState(string state)
        {
            if (!HasDoor())
            {
                return;
            }

            var saved = JsonUtility.FromJson<State>(state);
            Door.RestoreState(saved.IsOpen, saved.IsLocked);
        }

        protected override void RememberSceneState()
        {
            if (HasDoor())
            {
                Door.GetState(out m_IsOpenInScene, out m_IsLockedInScene);
            }
        }

        private bool HasDoor()
        {
            if (Door != null)
            {
                return true;
            }

            Debug.LogError($"[LocationState] DoorStateSaver '{name}': не найдена дверь (InteractionDoor или NonInteractionDoor) - " +
                           "назначьте её в поле Door или повесьте компонент на объект двери.", this);
            return false;
        }
    }
}
