using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Debug = UnityEngine.Debug;

namespace Game.Scripts.LocationScene.External.Controllers
{
    /// <summary>
    /// Прогрев шейдеров за экраном загрузки. Без него видеодрайвер на машине игрока при первом показе
    /// каждого нового материала (новая локация, новый объект в кадре) собирает под него шейдер и состояние
    /// конвейера (PSO) - на D3D12 это заметный фриз прямо в игре, на тестовой машине около секунды при
    /// первом появлении второй локации. Драйвер кэширует результат на диск, поэтому фриз бывает только при
    /// первом запуске после установки или обновления игры (или драйвера).
    ///
    /// Прогрев: LocationGameStarter перед тем, как убрать экран загрузки, ждёт <see cref="WarmUpAsync"/> - тот
    /// прогревает записанные наборы состояний (GraphicsStateCollection, файлы .graphicsstate) для текущего
    /// графического API. Один раз за запуск игры: возврат к сохранению прогрев не повторяет.
    ///
    /// Запись набора (после того как в игре появились новые локации или материалы):
    ///  1) собрать билд и запустить его с аргументом -traceShaders;
    ///  2) пройти все локации и посмотреть вокруг (всё, что игрок может увидеть);
    ///  3) выйти из игры - файл сохранится в Application.persistentDataPath/ShaderWarmup (путь пишется в лог);
    ///  4) положить файл в проект (Assets/Game/Shaders/Warmup) и добавить его в Collections этого компонента.
    /// Для каждого графического API (D3D12, Vulkan...) нужен свой файл. API GraphicsStateCollection в Unity 6
    /// помечен как экспериментальный.
    /// </summary>
    public class ShaderWarmupController : MonoBehaviour
    {
        private const string TraceArgument = "-traceShaders";

        [Tooltip("Записанные наборы состояний шейдеров (.graphicsstate). Прогреваются те, что записаны на " +
                 "текущем графическом API.")]
        [SerializeField] private GraphicsStateCollection[] m_Collections = Array.Empty<GraphicsStateCollection>();
        [Tooltip("Писать в консоль, что и за сколько прогрето.")]
        [SerializeField] private bool m_LogWarmup = true;

        // Ядро пересоздаётся при возврате к сохранению - прогрев и запись живут весь запуск игры.
        private static bool s_IsWarmedUp;
        private static GraphicsStateCollection s_Trace;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_IsWarmedUp = false;
            s_Trace = null;
        }

        private void Awake()
        {
            if (s_Trace == null && Array.IndexOf(Environment.GetCommandLineArgs(), TraceArgument) >= 0)
            {
                StartTrace();
            }
        }

        /// <summary>
        /// Прогревает наборы для текущего графического API. Повторный вызов за тот же запуск игры ничего не делает.
        /// </summary>
        public async UniTask WarmUpAsync(CancellationToken token)
        {
            if (s_IsWarmedUp)
            {
                return;
            }

            s_IsWarmedUp = true;

            // В редакторе шейдеры собирает сам редактор (и на своём графическом API) - прогрев нужен только в билде.
            if (Application.isEditor)
            {
                return;
            }

            var watch = Stopwatch.StartNew();
            int warmedCount = 0;

            foreach (var collection in m_Collections)
            {
                if (collection == null || collection.graphicsDeviceType != SystemInfo.graphicsDeviceType)
                {
                    continue;
                }

                var handle = collection.WarmUp();

                while (!handle.IsCompleted)
                {
                    await UniTask.Yield(token);
                }

                handle.Complete();
                warmedCount += collection.totalGraphicsStateCount;
            }

            if (m_LogWarmup)
            {
                Debug.Log(warmedCount > 0
                    ? $"[ShaderWarmup] прогрето состояний шейдеров: {warmedCount} за {watch.Elapsed.TotalMilliseconds:F0} мс ({SystemInfo.graphicsDeviceType})"
                    : $"[ShaderWarmup] нет набора для {SystemInfo.graphicsDeviceType} - шейдеры не прогреты", this);
            }
        }

        private static void StartTrace()
        {
            s_Trace = new GraphicsStateCollection();

            if (!s_Trace.BeginTrace())
            {
                Debug.LogWarning($"[ShaderWarmup] Запись состояний шейдеров на {SystemInfo.graphicsDeviceType} не поддерживается " +
                                 "(нужен SystemInfo.supportsParallelPSOCreation).");
                s_Trace = null;
                return;
            }

            Application.quitting += SaveTrace;
            Debug.Log($"[ShaderWarmup] Идёт запись состояний шейдеров ({SystemInfo.graphicsDeviceType}). Пройдите все локации и выйдите из игры.");
        }

        private static void SaveTrace()
        {
            if (s_Trace == null)
            {
                return;
            }

            s_Trace.EndTrace();

            string folder = Path.Combine(Application.persistentDataPath, "ShaderWarmup");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"Locations_{SystemInfo.graphicsDeviceType}.graphicsstate");

            Debug.Log(s_Trace.SaveToFile(path)
                ? $"[ShaderWarmup] Записано состояний шейдеров: {s_Trace.totalGraphicsStateCount}, вариантов: {s_Trace.variantCount}. Файл: {path}"
                : $"[ShaderWarmup] Не удалось сохранить запись в {path}");
        }
    }
}
