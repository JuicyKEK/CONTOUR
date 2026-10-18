using Game.Scripts.LocationState.Runtime.Interfaces;
using JuicyDI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.LocationState.Runtime.Controllers
{
    /// <summary>
    /// Базовый компонент объекта локации, состояние которого сохраняется. Вешается на сам объект (дверь,
    /// подбираемый предмет, выключатель). Наследник описывает, что сохранять: <see cref="CaptureState"/>
    /// возвращает состояние строкой, <see cref="ApplySavedState"/> ставит его обратно - сразу, без анимаций.
    ///
    /// Объект регистрируется в хранилище (<see cref="ILocationStateService"/>, сцена ядра) в MethodInit, то есть
    /// когда JuicyDI запускает бины его сцены: при загрузке локации во время перехода, после смерти и при
    /// загрузке из меню. Сохранённое состояние объект получает сразу при регистрации - до того, как игрок
    /// увидит сцену. В сценах без ядра игры (меню, старые тестовые сцены) хранилища нет, и компонент ничего не делает.
    ///
    /// Хранится только состояние, отличное от того, в котором объект лежит в сцене: <see cref="CaptureState"/>
    /// возвращает null, если объект в исходном состоянии (дверь закрыта, как в сцене, предмет не подобран).
    /// Исходное состояние наследник запоминает в <see cref="RememberSceneState"/> - до того, как объект получит
    /// сохранённое.
    ///
    /// Ключ объекта (<see cref="StateId"/>) проставляется сам при сохранении сцены (LocationStateIdBaker) -
    /// в том числе у копий объекта и у экземпляров префаба. Объект, сцену которого после добавления
    /// компонента ещё не сохраняли, ключа не имеет и не сохраняется.
    ///
    /// Наследник обязан быть помечен [JDIMonoController] - атрибут не наследуется.
    /// </summary>
    public abstract class LocationStateSaver : MonoBehaviour, ILocationStateObject, ISequence
    {
        [Tooltip("Ключ объекта в сохранении. Проставляется сам при сохранении сцены - руками не менять.")]
        [SerializeField] private string m_StateId;

        private ILocationStateService m_StateService;
        private bool m_IsRegistered;

        public string StateId => m_StateId;
        public string StateKey => $"{gameObject.scene.path}:{m_StateId}";
        public Scene Scene => gameObject.scene;

        public void MethodInit()
        {
            // Не [Inject]: в сценах без ядра игры хранилища нет, и это не ошибка.
            m_StateService = BinController.GetContext()?.GetBean<ILocationStateService>();

            if (m_StateService == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(m_StateId))
            {
                Debug.LogWarning($"[LocationState] У объекта '{name}' ({GetType().Name}) нет ключа сохранения - " +
                                 $"сохраните сцену '{gameObject.scene.name}', ключ проставится сам.", this);
                return;
            }

            RememberSceneState();
            m_IsRegistered = true;
            m_StateService.Register(this);
        }

        public void MethodStart()
        {
        }

        /// <summary>
        /// Состояние объекта строкой (обычно JSON) или null, если объект в том же состоянии, что и в сцене.
        /// </summary>
        public abstract string CaptureState();

        public abstract void ApplySavedState(string state);

        /// <summary>
        /// Запомнить, в каком состоянии объект лежит в сцене. Вызывается до получения сохранённого состояния.
        /// </summary>
        protected virtual void RememberSceneState()
        {
        }

        /// <summary>
        /// Запомнить состояние прямо сейчас - если объект вот-вот исчезнет со сцены.
        /// </summary>
        protected void StoreStateNow()
        {
            if (m_IsRegistered)
            {
                m_StateService.StoreState(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_IsRegistered)
            {
                m_IsRegistered = false;
                m_StateService.Unregister(this);
            }
        }
    }
}
