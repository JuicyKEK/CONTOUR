using Game.Scripts.LocationScene.Runtime.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.LocationScene.Runtime.Controllers
{
    /// <summary>
    /// Запуск сцены ядра игры. Локацию, её переходные сцены и игрока на чекпоинте поднимает уже само
    /// ядро (LocationGameStarter) - по сохранению игрока или, если его нет, со стартовой локации каталога.
    /// </summary>
    public static class LocationGameLauncher
    {
        /// <summary>
        /// Запускает игру заново: все загруженные сцены выгружаются, загружается ядро. Так игра стартует
        /// из главного меню и так же возвращается к последнему сохранению (например, при смерти игрока).
        /// </summary>
        public static bool LoadGame(LocationCatalogSO catalog)
        {
            if (!HasGameCoreScene(catalog))
            {
                return false;
            }

            SceneManager.LoadSceneAsync(catalog.GameCoreScenePath, LoadSceneMode.Single);
            return true;
        }

        /// <summary>
        /// Догружает ядро к уже открытой сцене, если его ещё нет: нужно, когда сцену локации запустили
        /// саму по себе (Play в редакторе), а не через главное меню.
        /// </summary>
        public static void EnsureGameCore(LocationCatalogSO catalog)
        {
            if (!HasGameCoreScene(catalog))
            {
                return;
            }

            // Сцена уже в списке SceneManager, даже если ещё грузится.
            if (SceneManager.GetSceneByPath(catalog.GameCoreScenePath).IsValid())
            {
                return;
            }

            Debug.Log("[LocationScene] Сцена локации запущена без ядра игры - догружаю ядро.");
            SceneManager.LoadSceneAsync(catalog.GameCoreScenePath, LoadSceneMode.Additive);
        }

        private static bool HasGameCoreScene(LocationCatalogSO catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("[LocationScene] Не назначен каталог локаций - ядро игры загрузить нельзя.");
                return false;
            }

            if (SceneUtility.GetBuildIndexByScenePath(catalog.GameCoreScenePath) < 0)
            {
                Debug.LogError($"[LocationScene] Сцена ядра игры '{catalog.GameCoreScenePath}' не назначена в каталоге " +
                               $"'{catalog.name}' или не добавлена в Build Settings.", catalog);
                return false;
            }

            return true;
        }
    }
}
