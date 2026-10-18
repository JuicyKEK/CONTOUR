using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Audio.Footsteps
{
    /// <summary>
    /// Поверхность под коллайдером с запоминанием: поиск (GetComponentInParent, материалы) идёт один раз на коллайдер,
    /// дальше - одно обращение к словарю по id коллайдера. Кеш сбрасывается, если поверхность сменили на лету
    /// (<see cref="FootstepSurface.SetSurface"/>) или в нём накопилось слишком много записей (выгруженные локации).
    /// </summary>
    public sealed class FootstepSurfaceResolver
    {
        private const int MaxCacheSize = 4096;

        private readonly FootstepSurfaceLibrarySO m_Library;
        private readonly Dictionary<int, FootstepSurfaceSO> m_Cache = new();
        private int m_CacheVersion;

        public FootstepSurfaceResolver(FootstepSurfaceLibrarySO library)
        {
            m_Library = library;
            m_CacheVersion = FootstepSurface.Version;
        }

        public FootstepSurfaceSO Default => m_Library != null ? m_Library.DefaultSurface : null;

        public FootstepSurfaceSO Resolve(Collider collider)
        {
            if (collider == null)
            {
                return Default;
            }

            if (m_CacheVersion != FootstepSurface.Version || m_Cache.Count >= MaxCacheSize)
            {
                m_Cache.Clear();
                m_CacheVersion = FootstepSurface.Version;
            }

            int id = collider.GetInstanceID();

            if (m_Cache.TryGetValue(id, out var surface))
            {
                return surface;
            }

            surface = Find(collider) ?? Default;
            m_Cache[id] = surface;
            return surface;
        }

        private FootstepSurfaceSO Find(Collider collider)
        {
            var marker = collider.GetComponentInParent<FootstepSurface>();

            if (marker != null && marker.Surface != null)
            {
                return marker.Surface;
            }

            if (m_Library == null)
            {
                return null;
            }

            if (m_Library.TryGet(collider.sharedMaterial, out var byPhysics))
            {
                return byPhysics;
            }

            if (collider.TryGetComponent(out Renderer renderer) && m_Library.TryGet(renderer.sharedMaterial, out var byMaterial))
            {
                return byMaterial;
            }

            return null;
        }
    }
}
