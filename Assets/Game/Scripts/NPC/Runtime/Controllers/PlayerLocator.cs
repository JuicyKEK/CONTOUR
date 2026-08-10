using Game.Scripts.NPC.Runtime.Interfaces;
using JuicyDI;
using JuicyDI.Attributes;
using JuicyDI.Context;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Простой глобальный бин: один раз находит игрока по тегу и раздаёт его
    /// Transform всем ботам через DI. Повесьте на любой постоянный объект сцены
    /// (например рядом с InputController) или укажите m_PlayerTransform вручную.
    /// </summary>
    [JDIMonoController(Context = typeof(GlobalBean))]
    [SequenceParticipant(10)]
    public class PlayerLocator : MonoBehaviour, IPlayerLocator, ISequence
    {
        [Tooltip("Если не задано - будет найден автоматически по тегу при старте.")]
        [SerializeField] private Transform m_PlayerTransform;
        [SerializeField] private string m_PlayerTag = "Player";

        public Transform PlayerTransform => m_PlayerTransform;

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            if (m_PlayerTransform == null)
            {
                var found = GameObject.FindGameObjectWithTag(m_PlayerTag);

                if (found != null)
                {
                    m_PlayerTransform = found.transform;
                }
                else
                {
                    Debug.LogWarning($"{nameof(PlayerLocator)}: player with tag '{m_PlayerTag}' not found.", this);
                }
            }
        }
    }
}

