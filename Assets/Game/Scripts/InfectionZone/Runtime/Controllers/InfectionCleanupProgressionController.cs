using System.Collections.Generic;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InfectionZone.Runtime.Controllers
{
    /// <summary>
    /// Контроллер прогрессии появления очищаемых аномалий (объектов типа InfectionCleanupObject)
    /// на зоне заражения. Управляет двумя списками:
    /// - обязательные аномалии (m_MandatoryAnomalies) - появляются первыми, строго по порядку;
    /// - случайные аномалии (m_RandomPoolAnomalies) - появляются после того, как все обязательные
    ///   уже показаны, порядок внутри пула перемешивается один раз при инициализации.
    ///
    /// Общее количество аномалий N = кол-во обязательных + кол-во случайных.
    /// Порог заражения на одну аномалию = 100 / N.
    /// Например, если N = 10, то при заражении 0-10% появляется 1-я аномалия, 10-20% - 2-я и т.д.
    ///
    /// Важно: аномалия, однажды появившись, не скрывается автоматически при уменьшении
    /// заражения - убрать её со сцены может только сама аномалия при взаимодействии игрока
    /// (InfectionCleanupObject.Interact()). Так появление и исчезновение аномалий разделены:
    /// появление зависит от степени заражения, а исчезновение - только от действий игрока.
    /// </summary>
    public class InfectionCleanupProgressionController : MonoBehaviour
    {
        [Header("Ссылка на зону")]
        [Tooltip("Зона, за степенью заражения которой следит контроллер прогрессии.")]
        [SerializeField] private MonoBehaviour m_ZoneSource; // должен реализовывать IInfectionZone

        [Header("Обязательные аномалии (появляются первыми, по порядку)")]
        [SerializeField] private GameObject[] m_MandatoryAnomalies;

        [Header("Случайные аномалии (появляются после обязательных, порядок перемешан)")]
        [SerializeField] private GameObject[] m_RandomPoolAnomalies;
        
        [SerializeField] private bool m_DisableAfterUse = false;

        private readonly CompositeDisposable m_Disposables = new();

        private IInfectionZone m_Zone;
        private List<GameObject> m_OrderedSlots;
        private bool[] m_Revealed;
        private float m_ThresholdSize;
        private int m_TotalCount;
        private int m_MandatoryCount;

        private void Awake()
        {
            m_Zone = m_ZoneSource as IInfectionZone;

            if (m_Zone == null)
            {
                Debug.LogError($"[InfectionCleanupProgressionController] {name}: m_ZoneSource должен реализовывать IInfectionZone.", this);
                return;
            }

            BuildOrderedSlots();

            // На старте все аномалии должны быть скрыты - появляться они будут по мере заражения зоны.
            foreach (var slot in m_OrderedSlots)
            {
                if (slot != null)
                {
                    slot.SetActive(false);
                }
            }
        }

        private void OnEnable()
        {
            if (m_Zone == null)
            {
                return;
            }

            m_Zone.InfectionLevel
                .Subscribe(UpdateSlots)
                .AddTo(m_Disposables);
        }

        private void OnDisable()
        {
            m_Disposables.Clear();
        }

        /// <summary>
        /// Формирует общий упорядоченный список слотов: сначала обязательные аномалии
        /// в заданном порядке, затем случайные аномалии в перемешанном (один раз) порядке.
        /// </summary>
        private void BuildOrderedSlots()
        {
            m_OrderedSlots = new List<GameObject>();

            m_MandatoryCount = m_MandatoryAnomalies?.Length ?? 0;

            if (m_MandatoryAnomalies != null)
            {
                m_OrderedSlots.AddRange(m_MandatoryAnomalies);
            }

            var shuffledRandomPool = ShuffleCopy(m_RandomPoolAnomalies);
            m_OrderedSlots.AddRange(shuffledRandomPool);

            m_TotalCount = m_OrderedSlots.Count;
            m_Revealed = new bool[m_TotalCount];
            m_ThresholdSize = m_TotalCount > 0 ? 100f / m_TotalCount : 0f;
        }

        /// <summary>
        /// Возвращает копию массива в случайном порядке (алгоритм Фишера-Йетса).
        /// Перемешивание выполняется один раз при инициализации, поэтому порядок появления
        /// случайных аномалий фиксируется на всю игровую сессию.
        /// </summary>
        private static List<GameObject> ShuffleCopy(GameObject[] source)
        {
            var result = new List<GameObject>(source ?? System.Array.Empty<GameObject>());

            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }

        /// <summary>
        /// Пересчитывает, сколько слотов должно быть открыто при текущей степени заражения.
        ///
        /// Обычный режим (m_DisableAfterUse == false): просто активирует все слоты в пределах
        /// окна activeCount - ранее очищенные объекты (как обязательные, так и случайные)
        /// могут появляться снова, повторно пересекая свой порог заражения.
        ///
        /// Режим m_DisableAfterUse == true: однажды показанный ОБЯЗАТЕЛЬНЫЙ слот блокируется
        /// навсегда и больше не учитывается и не активируется повторно (считается "использованным").
        /// Если из-за этого свободных обязательных слотов не хватает, чтобы набрать нужное
        /// количество activeCount, остаток добирается из случайного пула. Случайные слоты
        /// блокировке не подлежат никогда - ведут себя как в обычном режиме.
        /// </summary>
        private void UpdateSlots(float infectionLevel)
        {
            if (m_TotalCount == 0)
            {
                return;
            }

            // Небольшой эпсилон компенсирует погрешность float при попадании ровно на границу порога.
            int activeCount = Mathf.Clamp(
                Mathf.CeilToInt(infectionLevel / m_ThresholdSize - 0.0001f),
                0,
                m_TotalCount);

            if (!m_DisableAfterUse)
            {
                for (int i = 0; i < activeCount; i++)
                {
                    if (m_OrderedSlots[i] != null)
                    {
                        m_OrderedSlots[i].SetActive(true);
                    }
                }

                return;
            }

            int mandatoryCount = Mathf.Min(m_MandatoryCount, m_TotalCount);

            // Сколько обязательных слотов ещё не заблокировано (не было показано ранее).
            int availableMandatoryCount = 0;
            for (int i = 0; i < mandatoryCount; i++)
            {
                if (!m_Revealed[i])
                {
                    availableMandatoryCount++;
                }
            }

            int neededFromMandatory = Mathf.Min(activeCount, availableMandatoryCount);
            int neededFromRandom = Mathf.Max(0, activeCount - availableMandatoryCount);

            // Активируем первые ещё не заблокированные обязательные слоты по порядку
            // и сразу же помечаем их как использованные - больше они не активируются.
            int activatedMandatory = 0;
            for (int i = 0; i < mandatoryCount && activatedMandatory < neededFromMandatory; i++)
            {
                if (m_Revealed[i])
                {
                    continue;
                }

                m_Revealed[i] = true;
                activatedMandatory++;

                if (m_OrderedSlots[i] != null)
                {
                    m_OrderedSlots[i].SetActive(true);
                }
            }

            // Недостающее количество добираем из случайного пула - он никогда не блокируется,
            // поэтому просто активируем нужное число слотов по порядку, не трогая m_Revealed.
            int randomCount = m_TotalCount - mandatoryCount;
            int takeFromRandom = Mathf.Min(neededFromRandom, randomCount);

            for (int i = 0; i < takeFromRandom; i++)
            {
                int index = mandatoryCount + i;

                if (m_OrderedSlots[index] != null)
                {
                    m_OrderedSlots[index].SetActive(true);
                }
            }
        }
    }
}
