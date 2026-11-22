using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game.Scripts.Inventory;
using Game.Scripts.Story;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InteractionHints.Controllers
{
    /// <summary>
    /// Подсказка при взаимодействии: игрок нажал E на объекте - внизу экрана появляется текст (та же нижняя подсказка,
    /// что у сюжета, IStoryHintView). Когда показывать, решают условия на том же объекте (IInteractionHintCondition):
    /// подсказка показывается, если все разрешают; условий нет - при каждом взаимодействии.
    ///
    /// Слушатель взаимодействия (IInteractionListener): PlayerInteractiveController зовёт одного слушателя на объект.
    /// </summary>
    [JDIMonoController]
    [DisallowMultipleComponent]
    public class InteractionHint : MonoBehaviour, IInteractionListener
    {
        [Tooltip("Текст подсказки.")]
        [SerializeField, TextArea] private string m_Text;
        [Tooltip("Сколько секунд видна подсказка.")]
        [SerializeField, Min(0.5f)] private float m_Duration = 4f;

        [Inject] private IStoryHintView m_HintView;

        private readonly List<IInteractionHintCondition> m_Conditions = new();

        public void OnInteracted(IInteraction interaction)
        {
            if (m_HintView == null || string.IsNullOrEmpty(m_Text) || !CanShow())
            {
                return;
            }

            m_HintView.ShowAsync(m_Text, m_Duration, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private bool CanShow()
        {
            GetComponents(m_Conditions);

            foreach (var condition in m_Conditions)
            {
                if (!condition.CanShowHint())
                {
                    return false;
                }
            }

            return true;
        }
    }
}
