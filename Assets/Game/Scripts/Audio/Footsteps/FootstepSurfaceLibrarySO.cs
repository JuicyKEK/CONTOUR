using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Audio.Footsteps
{
    /// <summary>
    /// По чему идёт игрок: сопоставление "что под ногами" -> поверхность шагов.
    /// Порядок, в котором ищется поверхность коллайдера под ногами (см. <see cref="FootstepSurfaceResolver"/>):
    ///  1) компонент <see cref="FootstepSurface"/> на объекте коллайдера или его родителе - явная метка;
    ///  2) физический материал коллайдера (Collider.sharedMaterial) - из списка ниже;
    ///  3) материал меша (Renderer.sharedMaterial объекта коллайдера) - из списка ниже;
    ///  4) поверхность по умолчанию.
    /// Ответ запоминается для каждого коллайдера, поэтому поиск идёт один раз, а не на каждом шаге.
    /// </summary>
    [CreateAssetMenu(menuName = "Audio/Footstep Surface Library", fileName = "FootstepSurfaceLibrary")]
    public class FootstepSurfaceLibrarySO : ScriptableObject
    {
        [Serializable]
        public struct PhysicsMaterialSurface
        {
            public PhysicsMaterial Material;
            public FootstepSurfaceSO Surface;
        }

        [Serializable]
        public struct RenderMaterialSurface
        {
            public Material Material;
            public FootstepSurfaceSO Surface;
        }

        [Tooltip("Поверхность, если ничего не подошло.")]
        [SerializeField] private FootstepSurfaceSO m_DefaultSurface;
        [Tooltip("Физический материал коллайдера -> поверхность.")]
        [SerializeField] private PhysicsMaterialSurface[] m_PhysicsMaterials = Array.Empty<PhysicsMaterialSurface>();
        [Tooltip("Материал меша -> поверхность (удобно, когда пол уже покрашен нужными материалами).")]
        [SerializeField] private RenderMaterialSurface[] m_RenderMaterials = Array.Empty<RenderMaterialSurface>();

        private Dictionary<PhysicsMaterial, FootstepSurfaceSO> m_ByPhysicsMaterial;
        private Dictionary<Material, FootstepSurfaceSO> m_ByRenderMaterial;

        public FootstepSurfaceSO DefaultSurface => m_DefaultSurface;

        private void OnEnable()
        {
            m_ByPhysicsMaterial = null;
            m_ByRenderMaterial = null;
        }

        private void OnValidate()
        {
            m_ByPhysicsMaterial = null;
            m_ByRenderMaterial = null;
        }

        public bool TryGet(PhysicsMaterial material, out FootstepSurfaceSO surface)
        {
            surface = null;

            if (material == null)
            {
                return false;
            }

            if (m_ByPhysicsMaterial == null)
            {
                m_ByPhysicsMaterial = new Dictionary<PhysicsMaterial, FootstepSurfaceSO>();

                foreach (var entry in m_PhysicsMaterials)
                {
                    if (entry.Material != null && entry.Surface != null)
                    {
                        m_ByPhysicsMaterial[entry.Material] = entry.Surface;
                    }
                }
            }

            return m_ByPhysicsMaterial.TryGetValue(material, out surface);
        }

        public bool TryGet(Material material, out FootstepSurfaceSO surface)
        {
            surface = null;

            if (material == null)
            {
                return false;
            }

            if (m_ByRenderMaterial == null)
            {
                m_ByRenderMaterial = new Dictionary<Material, FootstepSurfaceSO>();

                foreach (var entry in m_RenderMaterials)
                {
                    if (entry.Material != null && entry.Surface != null)
                    {
                        m_ByRenderMaterial[entry.Material] = entry.Surface;
                    }
                }
            }

            return m_ByRenderMaterial.TryGetValue(material, out surface);
        }
    }
}
