using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JuicyDI.Attributes;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Нижняя текстовая подсказка. Регистрируется как SceneBean и используется только через интерфейс
    /// </summary>
    [JDIMonoController]
    public class StoryHintView : MonoBehaviour, IStoryHintView
    {
        [SerializeField] private GameObject m_Root;
        [SerializeField] private TMP_Text m_Text;

        // Номер последнего показа: таймер старой подсказки не должен скрыть уже новую.
        private int m_ShowVersion;

        public async UniTask ShowAsync(string text, float duration, CancellationToken token)
        {
            if (m_Root == null || m_Text == null)
            {
                Debug.LogWarning($"[Story] StoryHintView '{name}': не назначены Root или Text - подсказка не видна.", this);
            }

            int version = ++m_ShowVersion;

            if (m_Text != null)
            {
                m_Text.text = text;
            }

            if (m_Root != null)
            {
                m_Root.SetActive(true);
            }

            try
            {
                if (float.IsPositiveInfinity(duration))
                {
                    // Без таймера: до отмены токена (например, пока сюжет не уйдёт с ноды).
                    await UniTask.WaitUntilCanceled(token);
                }
                else
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
                }
            }
            finally
            {
                if (version == m_ShowVersion)
                {
                    Hide();
                }
            }
        }

        public void Hide()
        {
            if (m_Root != null)
            {
                m_Root.SetActive(false);
            }
        }
    }
}
