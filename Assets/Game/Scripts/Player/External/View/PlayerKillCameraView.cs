using System.Collections.Generic;
using Game.Scripts.Player.External.Controllers;
using JuicyDI;
using JuicyDI.Attributes;
using R3;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.Scripts.Player.External.View
{
    /// <summary>
    /// Камера анимации убийства от первого лица: пока у поимки есть точка взгляда (IPlayerCapture.ViewAnchor - кость
    /// глаз игрока в скелете монстра), эта камера Cinemachine включена и стоит в этой точке (положение и поворот),
    /// переход от обычной камеры игрока - за m_BlendInDuration. Точки нет - камера выключена, видна обычная.
    /// Пока идёт убийство, прицел и другие объекты из m_HideDuringKill спрятаны.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(122)]
    public class PlayerKillCameraView : MonoBehaviour, ISequence
    {
        [Inject] private IPlayerCapture m_PlayerCapture;

        [SerializeField] private CinemachineCamera m_Camera;
        [Tooltip("За сколько секунд вид переходит от обычной камеры игрока к его глазам в анимации убийства.")]
        [SerializeField, Min(0f)] private float m_BlendInDuration = 0.25f;
        [Tooltip("Что спрятать на экране, пока игрока убивают (прицел).")]
        [SerializeField] private GameObject[] m_HideDuringKill;

        private readonly CompositeDisposable m_Disposables = new();
        private readonly List<GameObject> m_HiddenObjects = new();
        private CinemachineCore.GetBlendOverrideDelegate m_PreviousBlendOverride;

        public void MethodInit()
        {
            m_Camera.enabled = false;
            m_PreviousBlendOverride = CinemachineCore.GetBlendOverride;
            CinemachineCore.GetBlendOverride = OverrideBlend;

            m_PlayerCapture.ViewAnchor
                .Subscribe(ShowAnchor)
                .AddTo(m_Disposables);
        }

        public void MethodStart()
        {
        }

        private void ShowAnchor(Transform anchor)
        {
            m_Camera.Follow = anchor;
            m_Camera.enabled = anchor != null;
            SetHudHidden(anchor != null);
        }

        private void SetHudHidden(bool isHidden)
        {
            if (isHidden)
            {
                if (m_HideDuringKill == null)
                {
                    return;
                }

                foreach (var hudObject in m_HideDuringKill)
                {
                    if (hudObject != null && hudObject.activeSelf)
                    {
                        hudObject.SetActive(false);
                        m_HiddenObjects.Add(hudObject);
                    }
                }

                return;
            }

            foreach (var hudObject in m_HiddenObjects)
            {
                if (hudObject != null)
                {
                    hudObject.SetActive(true);
                }
            }

            m_HiddenObjects.Clear();
        }

        private CinemachineBlendDefinition OverrideBlend(ICinemachineCamera from, ICinemachineCamera to,
            CinemachineBlendDefinition defaultBlend, Object owner)
        {
            if (ReferenceEquals(to, m_Camera))
            {
                return new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, m_BlendInDuration);
            }

            if (ReferenceEquals(from, m_Camera))
            {
                return new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            }

            return m_PreviousBlendOverride != null ? m_PreviousBlendOverride(from, to, defaultBlend, owner) : defaultBlend;
        }

        private void OnDestroy()
        {
            if (CinemachineCore.GetBlendOverride == OverrideBlend)
            {
                CinemachineCore.GetBlendOverride = m_PreviousBlendOverride;
            }

            m_Disposables.Dispose();
        }
    }
}
