using System;
using Game.Scripts.InteractionObjects.Interfaces;
using R3;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// "Эта дверь заперта - через неё не пройти": вырезает проём двери из NavMesh (NavMeshObstacle с вырезанием),
    /// пока дверь заперта, - пути ботов строятся в обход. Вешается на дверь сам, когда бот подёргал запертую
    /// дверь и сдался (<see cref="Block"/>); как только дверь отпирают - проём снова проходим.
    /// Сами двери в NavMesh не запекаются (проём проходим), поэтому без этой метки бот не знает, что дверь заперта.
    /// </summary>
    public class NpcDoorNavBlocker : MonoBehaviour
    {
        private IInteractionDoor m_Door;
        private NavMeshObstacle m_Obstacle;
        private IDisposable m_Subscription;
        private bool m_IsRequested;

        /// <summary>Сейчас проём вырезан (дверь заперта и бот о ней "знает").</summary>
        public bool IsBlocking => m_Obstacle != null && m_Obstacle.enabled;

        /// <summary>
        /// Пометить запертую дверь непроходимой для всех ботов, пока она заперта.
        /// </summary>
        public static NpcDoorNavBlocker Block(IInteractionDoor door)
        {
            if (door is not Component component || !door.IsLocked.CurrentValue)
            {
                return null;
            }

            if (!component.TryGetComponent(out NpcDoorNavBlocker blocker))
            {
                blocker = component.gameObject.AddComponent<NpcDoorNavBlocker>();
                blocker.Setup(door);
            }

            blocker.m_IsRequested = true;
            blocker.Refresh(door.IsLocked.CurrentValue);
            return blocker;
        }

        private void Setup(IInteractionDoor door)
        {
            m_Door = door;
            var holder = new GameObject("NavBlocker (дверь заперта)");
            holder.transform.SetParent(transform, false);
            m_Obstacle = holder.AddComponent<NavMeshObstacle>();
            m_Obstacle.shape = NavMeshObstacleShape.Box;
            m_Obstacle.carving = true;
            m_Obstacle.carveOnlyStationary = false;

            // Размер - по коллайдеру двери (полотно в закрытом положении), толще стены, чтобы проём точно перекрылся.
            if (TryGetComponent(out BoxCollider box))
            {
                m_Obstacle.center = box.center;
                m_Obstacle.size = new Vector3(Mathf.Max(box.size.x, 0.5f), box.size.y, Mathf.Max(box.size.z, 0.5f));
            }
            else if (TryGetComponent(out Collider other))
            {
                var bounds = other.bounds;
                holder.transform.position = bounds.center;
                holder.transform.rotation = Quaternion.identity;
                m_Obstacle.center = Vector3.zero;
                m_Obstacle.size = Vector3.Max(bounds.size, new Vector3(0.5f, 1f, 0.5f));
            }

            m_Obstacle.enabled = false;
            m_Subscription = door.IsLocked.Subscribe(Refresh);
        }

        private void Refresh(bool isLocked)
        {
            if (m_Obstacle == null)
            {
                return;
            }

            // Отперли - проём снова проходим; заново заблокирует бот, если опять наткнётся на запертую.
            if (!isLocked)
            {
                m_IsRequested = false;
            }

            m_Obstacle.enabled = isLocked && m_IsRequested;
        }

        private void OnDestroy()
        {
            m_Subscription?.Dispose();
        }
    }
}
