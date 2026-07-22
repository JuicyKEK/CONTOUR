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

        public void Init(Sprite icon, int count)
        {
            gameObject.SetActive(true);
            SetIcon(icon);
            SetTextCount(count);
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
        
        private void SetIcon(Sprite icon)
        {
            m_ItemIcon.sprite = icon;
        }
    }
}