using Game.Scripts.LocationScene.Runtime.Controllers;
using Game.Scripts.LocationScene.Runtime.Data;
using Game.Scripts.Utilities.Save;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.LocationScene.External.Controllers
{
    /// <summary>
    /// Запуск игры из главного меню. Методы вешаются на кнопки меню (UnityEvent кнопки):
    ///  - <see cref="StartNewGame"/> - "Начать игру": удаляет сохранения и запускает игру со стартовой локации;
    ///  - <see cref="ContinueGame"/> - "Загрузить игру": запускает игру с сохранённой локации и чекпоинта.
    /// Сам компонент только загружает сцену ядра - локацию и игрока поднимает оно (LocationGameStarter).
    /// </summary>
    public class MainMenuGameLauncher : MonoBehaviour
    {
        [Tooltip("Каталог локаций: из него берётся сцена ядра игры.")]
        [SerializeField] private LocationCatalogSO m_Catalog;
        [Tooltip("Кнопка 'Загрузить игру' - недоступна, пока нет сохранений. Можно не назначать.")]
        [SerializeField] private Button m_ContinueButton;

        private bool m_IsLoading;

        private void Start()
        {
            if (m_ContinueButton != null)
            {
                m_ContinueButton.interactable = JsonSaveFile.HasAnySave();
            }
        }

        public void StartNewGame()
        {
            if (m_IsLoading)
            {
                return;
            }

            JsonSaveFile.DeleteAll(this);
            LoadGame();
        }

        public void ContinueGame()
        {
            if (m_IsLoading)
            {
                return;
            }

            LoadGame();
        }

        private void LoadGame()
        {
            m_IsLoading = LocationGameLauncher.LoadGame(m_Catalog);
        }
    }
}
