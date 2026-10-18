using System.Collections.Generic;
using Game.Scripts.LocationScene.Runtime.Data;
using JuicyDI;
using JuicyDI.Utils;
using UnityEngine;

namespace Game.Scripts.LocationScene.Runtime.Controllers
{
    /// <summary>
    /// Стартер сцены локации или переходной сцены - кладётся в каждую такую сцену (префаб LocationSceneStarter).
    /// Делает для своей сцены то же, что стартер ядра делает для всей игры, но только со своими объектами:
    /// регистрирует бины сцены в JuicyDI, запускает их MethodInit/MethodStart и каждый кадр вызывает их
    /// CustomUpdate. Бины ядра и соседних сцен он не трогает - иначе они запускались бы повторно при каждой
    /// догрузке локации. Выгружается вместе со сценой, и обновление её бинов прекращается само.
    ///
    /// Когда сцену грузит игра, <see cref="Init"/> вызывает загрузчик локаций. Если же сцену запустили саму
    /// по себе (Play в редакторе), стартер догружает сцену ядра - и дальше игра стартует как обычно, с этой локации.
    /// </summary>
    public class LocationSceneStarter : MonoBehaviour
    {
        [Tooltip("Каталог локаций: из него берётся сцена ядра игры, когда эту сцену запускают саму по себе.")]
        [SerializeField] private LocationCatalogSO m_Catalog;

        private readonly List<MonoBehaviour> m_SceneBehaviours = new();

        private IGameUpdateSequenceController m_GameUpdateSequenceController;
        private bool m_IsRegistered;

        private void Start()
        {
            // Сцену загрузило ядро - оно её и запустит.
            if (m_IsRegistered)
            {
                return;
            }

            LocationGameLauncher.EnsureGameCore(m_Catalog);
        }

        private void Update()
        {
            m_GameUpdateSequenceController?.UpdateSequence();
        }

        /// <summary>
        /// Полный запуск сцены, догруженной во время игры.
        /// </summary>
        public void Init()
        {
            RegisterBeans();
            RunSequence();
            StartUpdates();
        }

        /// <summary>
        /// Регистрирует бины сцены в JuicyDI (и переинжектит бины остальных загруженных сцен).
        /// </summary>
        public void RegisterBeans()
        {
            if (m_IsRegistered)
            {
                return;
            }

            m_IsRegistered = true;

            // Сканер переиспользует один список - копируем.
            m_SceneBehaviours.Clear();
            m_SceneBehaviours.AddRange(JDISceneScanner.CollectSceneBehaviours(gameObject.scene));

            BinController.GetOrCreateContext().InitBins(m_SceneBehaviours);
        }

        /// <summary>
        /// MethodInit и MethodStart бинов этой сцены - в порядке SequenceParticipant.
        /// </summary>
        public void RunSequence()
        {
            _ = new GameSequenceController(CollectSceneBeans<ISequence>(true));
        }

        /// <summary>
        /// Начинает каждый кадр вызывать CustomUpdate бинов этой сцены.
        /// </summary>
        public void StartUpdates()
        {
            m_GameUpdateSequenceController = new GameUpdateSequenceController(CollectSceneBeans<IUpdateSequence>(false));
            m_SceneBehaviours.Clear();
        }

        private List<T> CollectSceneBeans<T>(bool isWarnAboutGlobalBeans) where T : class
        {
            var result = new List<T>();

            for (int i = 0; i < m_SceneBehaviours.Count; i++)
            {
                var behaviour = m_SceneBehaviours[i];

                if (behaviour == null || behaviour is not T bean)
                {
                    continue;
                }

                bool? isGlobalBean = JDIReflectionCache.IsGlobalBean(behaviour.GetType());

                if (isGlobalBean == false)
                {
                    result.Add(bean);
                }
                else if (isGlobalBean == true && isWarnAboutGlobalBeans)
                {
                    // Глобальный бин может оказаться дубликатом уже зарегистрированного (тогда JuicyDI его
                    // не инжектит), поэтому стартер сцены его не запускает.
                    Debug.LogWarning($"[LocationScene] Глобальный бин '{behaviour.GetType().Name}' лежит в сцене локации " +
                                     $"'{gameObject.scene.name}': стартер сцены его не запускает. Держите глобальные бины в сцене ядра.", behaviour);
                }
            }

            return result;
        }
    }
}
