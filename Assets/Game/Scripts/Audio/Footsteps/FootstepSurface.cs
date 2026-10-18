using UnityEngine;

namespace Game.Scripts.Audio.Footsteps
{
    /// <summary>
    /// Явная метка поверхности для шагов: вешается на пол (или на общий родитель нескольких полов) - самый
    /// надёжный способ, не зависит от материалов. Сменить поверхность на лету (пол залило водой) - <see cref="SetSurface"/>.
    /// </summary>
    public class FootstepSurface : MonoBehaviour
    {
        [SerializeField] private FootstepSurfaceSO m_Surface;

        /// <summary>
        /// Растёт при каждой смене поверхности на лету - по нему кеш поверхностей понимает, что его пора сбросить.
        /// </summary>
        public static int Version { get; private set; }

        public FootstepSurfaceSO Surface => m_Surface;

        public void SetSurface(FootstepSurfaceSO surface)
        {
            m_Surface = surface;
            Version++;
        }
    }
}
