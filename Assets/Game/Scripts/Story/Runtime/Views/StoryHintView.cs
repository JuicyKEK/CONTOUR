using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JuicyDI.Attributes;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Нижняя текстовая подсказка. Регистрируется как SceneBean и
    /// внедряется в StoryManager через [Inject] IStoryHintView.
    /// </summary>
    [JDIMonoController]
    public class StoryHintView : MonoBehaviour, IStoryHintView
    {
        [SerializeField] private GameObject m_Root;
        [SerializeField] private TMP_Text m_Text;

        public async UniTask ShowAsync(string text, float duration, CancellationToken token)
        {
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
                await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: token);
            }
            finally
            {
                Hide();
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

