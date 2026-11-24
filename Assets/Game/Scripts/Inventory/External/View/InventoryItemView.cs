using Cysharp.Threading.Tasks;
using Game.Scripts.Inventory.External.Controllers.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game.Scripts.Inventory.Runtime.View
{
    public class InventoryItemView : MonoBehaviour
    {
        [SerializeField] private Image m_ItemIcon;
        [SerializeField] private Image m_SelectedIcon;
        [SerializeField] private TMP_Text m_TextCount;

        private string m_IconAddress;

        public void Init(string iconAddress, int count, IInventoryIconLoader iconLoader)
        {
            gameObject.SetActive(true);
            SetTextCount(count);
            SetIconAsync(iconAddress, iconLoader).Forget();
        }

        public void SetTextCount(int count)
        {
            if (count == 1)
            {
                m_TextCount.text = string.Empty;
                return;
            }

            m_TextCount.text = count.ToString();
        }

        public void SetSelected(bool selected)
        {
            m_SelectedIcon.gameObject.SetActive(selected);
        }

        private async UniTaskVoid SetIconAsync(string iconAddress, IInventoryIconLoader iconLoader)
        {
            if (iconAddress == m_IconAddress && m_ItemIcon.sprite != null)
            {
                return;
            }

            m_IconAddress = iconAddress;
            m_ItemIcon.enabled = false;

            if (iconLoader == null)
            {
                return;
            }

            var (isCanceled, icon) = await iconLoader
                .LoadIconAsync(iconAddress, this.GetCancellationTokenOnDestroy())
                .SuppressCancellationThrow();

            // Пока иконка грузилась, ячейку могли отдать другому предмету.
            if (isCanceled || iconAddress != m_IconAddress)
            {
                return;
            }

            m_ItemIcon.sprite = icon;
            m_ItemIcon.enabled = icon != null;
        }
    }
}
