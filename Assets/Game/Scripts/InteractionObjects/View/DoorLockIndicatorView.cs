using System;
using R3;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.View
{
    /// <summary>
    /// Лампочка у двери: красная - дверь заперта, зелёная - открыта для прохода. Цвет меняется через
    /// MaterialPropertyBlock (материал не копируется). Состояние из сохранения - сразу нужного цвета.
    /// </summary>
    public class DoorLockIndicatorView : MonoBehaviour
    {
        private static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_EmissionColor = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private InteractionDoor m_Door;
        [SerializeField] private Renderer m_Renderer;
        [SerializeField] private Color m_LockedColor = new(0.9f, 0.1f, 0.1f);
        [SerializeField] private Color m_UnlockedColor = new(0.1f, 0.85f, 0.2f);

        private MaterialPropertyBlock m_Block;
        private IDisposable m_Subscription;

        private void Start()
        {
            if (m_Renderer == null)
            {
                m_Renderer = GetComponent<Renderer>();
            }

            if (m_Door == null || m_Renderer == null)
            {
                Debug.LogWarning($"[{nameof(DoorLockIndicatorView)}] {name}: не назначена дверь или Renderer лампочки.", this);
                return;
            }

            m_Block = new MaterialPropertyBlock();
            m_Subscription = m_Door.IsLocked.Subscribe(SetLocked);
        }

        private void OnDestroy()
        {
            m_Subscription?.Dispose();
        }

        private void SetLocked(bool isLocked)
        {
            var color = isLocked ? m_LockedColor : m_UnlockedColor;
            m_Renderer.GetPropertyBlock(m_Block);
            m_Block.SetColor(s_BaseColor, color);
            m_Block.SetColor(s_EmissionColor, color * 1.5f);
            m_Renderer.SetPropertyBlock(m_Block);
        }
    }
}
