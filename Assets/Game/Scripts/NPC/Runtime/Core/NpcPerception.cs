using Game.Scripts.NPC.Runtime.Data;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Зрение бота: дальность, угол обзора и (если включено) прямая видимость - стены и закрытые двери
    /// загораживают игрока. Луч идёт из глаз бота в голову и в грудь игрока (видна хоть одна точка - игрок виден),
    /// чтобы игрока, выглядывающего из-за низкого укрытия, тоже замечали.
    /// </summary>
    public class NpcPerception
    {
        private static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        private readonly Transform m_Self;
        private readonly NpcTypeDefinitionSO m_Definition;

        private Transform m_CachedPlayer;
        private CharacterController m_PlayerBody;

        /// <summary>Откуда бот смотрел в последний раз и в какую точку игрока (для отладки в сцене).</summary>
        public Vector3 LastEyePosition { get; private set; }
        public Vector3 LastTargetPosition { get; private set; }

        public NpcPerception(Transform self, NpcTypeDefinitionSO definition)
        {
            m_Self = self;
            m_Definition = definition;
        }

        public Vector3 EyePosition => m_Self.position + Vector3.up * m_Definition.EyeHeight;

        /// <summary>
        /// Видит ли бот игрока прямо сейчас. distance - горизонтально-объёмное расстояние до игрока.
        /// </summary>
        public bool CanSee(Transform player, out float distance)
        {
            distance = float.MaxValue;

            if (player == null)
            {
                return false;
            }

            var toPlayer = player.position - m_Self.position;
            distance = toPlayer.magnitude;

            if (distance > m_Definition.ViewDistance)
            {
                return false;
            }

            var flatToPlayer = toPlayer;
            flatToPlayer.y = 0f;

            if (flatToPlayer.sqrMagnitude > 0.0001f && Vector3.Angle(m_Self.forward, flatToPlayer) > m_Definition.ViewHalfAngle)
            {
                return false;
            }

            if (!m_Definition.RequireLineOfSight)
            {
                LastEyePosition = EyePosition;
                LastTargetPosition = player.position;
                return true;
            }

            CachePlayerBody(player);
            var eye = EyePosition;
            LastEyePosition = eye;

            GetPlayerPoints(player, out var head, out var chest);
            int mask = m_Definition.LineOfSightObstacles.value != 0
                ? m_Definition.LineOfSightObstacles.value
                : Physics.DefaultRaycastLayers;

            if (IsVisible(eye, head, player, mask))
            {
                LastTargetPosition = head;
                return true;
            }

            LastTargetPosition = chest;
            return IsVisible(eye, chest, player, mask);
        }

        private bool IsVisible(Vector3 eye, Vector3 target, Transform player, int mask)
        {
            var toTarget = target - eye;
            float length = toTarget.magnitude;

            if (length < 0.01f)
            {
                return true;
            }

            int count = Physics.RaycastNonAlloc(eye, toTarget / length, s_Hits, length, mask, QueryTriggerInteraction.Ignore);
            float nearestBlock = float.MaxValue;
            float playerHit = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                var hitTransform = s_Hits[i].transform;

                // Свои коллайдеры бота (тело, оружие в руках) обзор не загораживают.
                if (hitTransform.IsChildOf(m_Self))
                {
                    continue;
                }

                if (hitTransform == player || hitTransform.IsChildOf(player))
                {
                    playerHit = Mathf.Min(playerHit, s_Hits[i].distance);
                    continue;
                }

                nearestBlock = Mathf.Min(nearestBlock, s_Hits[i].distance);
            }

            // Ничего не загородило, или луч сначала упёрся в самого игрока.
            return nearestBlock == float.MaxValue || playerHit < nearestBlock;
        }

        private void CachePlayerBody(Transform player)
        {
            if (m_CachedPlayer == player)
            {
                return;
            }

            m_CachedPlayer = player;
            m_PlayerBody = player.GetComponent<CharacterController>();
        }

        private void GetPlayerPoints(Transform player, out Vector3 head, out Vector3 chest)
        {
            if (m_PlayerBody != null && m_PlayerBody.enabled)
            {
                var bounds = m_PlayerBody.bounds;
                head = new Vector3(bounds.center.x, bounds.max.y - 0.15f, bounds.center.z);
                chest = bounds.center;
                return;
            }

            head = player.position + Vector3.up * 1.6f;
            chest = player.position + Vector3.up * 1f;
        }
    }
}
