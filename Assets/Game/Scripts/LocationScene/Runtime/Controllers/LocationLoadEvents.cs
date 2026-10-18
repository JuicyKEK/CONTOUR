using Game.Scripts.LocationScene.Runtime.Data;
using Game.Scripts.LocationScene.Runtime.Interfaces;
using JuicyDI;
using JuicyDI.Attributes;
using R3;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.LocationScene.Runtime.Controllers
{
    /// <summary>
    /// События загрузки локации для объектов сцены (обычно переходной): дверь в конце перехода, которая
    /// открывается, только когда локация за ней загрузилась, свет, звук, заслонка и т.п.
    ///
    ///  - On Loaded - локация загрузилась и готова к игре. Если к моменту запуска этого компонента она уже
    ///    загружена (локация, из которой игрок пришёл), событие приходит сразу на старте.
    ///  - On Unloaded - локация начала выгружаться (игрок ушёл из перехода в другую сторону).
    /// События всегда чередуются: после On Loaded придёт только On Unloaded и наоборот. На старте, пока
    /// локация не загружена, ничего не вызывается - объект должен стоять в сцене в состоянии "не загружено"
    /// (дверь закрыта).
    ///
    /// В переходной сцене на каждом триггере перехода (LocationTransitionGate) уже лежит такой компонент
    /// для локации этого триггера.
    /// </summary>
    [JDIMonoController]
    public class LocationLoadEvents : MonoBehaviour, ISequence
    {
        [Tooltip("Локация, за загрузкой которой следит компонент.")]
        [SerializeField] private LocationDefinitionSO m_Location;

        [Tooltip("Локация загрузилась и готова к игре (или уже была загружена на старте этого объекта).")]
        [SerializeField] private UnityEvent m_OnLoaded = new();
        [Tooltip("Локация начала выгружаться.")]
        [SerializeField] private UnityEvent m_OnUnloaded = new();

        [Inject] private ILocationSceneService m_LocationService;

        private readonly CompositeDisposable m_Disposables = new();
        private bool m_IsLoaded;

        /// <summary>
        /// Загружена ли локация по последнему событию.
        /// </summary>
        public bool IsLoaded => m_IsLoaded;

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            if (m_Location == null)
            {
                Debug.LogError($"[LocationScene] LocationLoadEvents '{name}': не назначена локация.", this);
                return;
            }

            if (m_LocationService == null)
            {
                Debug.LogError($"[LocationScene] LocationLoadEvents '{name}': загрузчик локаций не заинжекчен - " +
                               "событий не будет.", this);
                return;
            }

            m_LocationService.LocationLoaded
                .Subscribe(location => SetLoaded(location, true))
                .AddTo(m_Disposables);

            m_LocationService.LocationUnloaded
                .Subscribe(location => SetLoaded(location, false))
                .AddTo(m_Disposables);

            if (m_LocationService.IsLocationLoaded(m_Location))
            {
                SetLoaded(m_Location, true);
            }
        }

        private void OnDestroy()
        {
            m_Disposables.Dispose();
        }

        private void SetLoaded(LocationDefinitionSO location, bool isLoaded)
        {
            if (location != m_Location || m_IsLoaded == isLoaded)
            {
                return;
            }

            m_IsLoaded = isLoaded;

            if (isLoaded)
            {
                m_OnLoaded.Invoke();
            }
            else
            {
                m_OnUnloaded.Invoke();
            }
        }
    }
}
