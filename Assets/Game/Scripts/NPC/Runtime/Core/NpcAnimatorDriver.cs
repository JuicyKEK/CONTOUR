using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Задел под анимации бота: каждый кадр передаёт в Animator скорость, состояние, агрессию и злость,
    /// а действия (удар, превращение...) - триггерами. Параметр выставляется, только если он есть
    /// в контроллере аниматора, поэтому бот работает и с пустым/старым контроллером, и вовсе без Animator
    /// (примитивы). Имена параметров - константы ниже; их же ждёт шаблонный контроллер NpcTemplate.
    /// </summary>
    public class NpcAnimatorDriver
    {
        public const string SpeedParameter = "Speed";
        public const string StateParameter = "State";
        public const string AggressiveParameter = "IsAggressive";
        public const string AngerParameter = "Anger";

        private static readonly int s_SpeedHash = Animator.StringToHash(SpeedParameter);
        private static readonly int s_StateHash = Animator.StringToHash(StateParameter);
        private static readonly int s_AggressiveHash = Animator.StringToHash(AggressiveParameter);
        private static readonly int s_AngerHash = Animator.StringToHash(AngerParameter);

        private readonly Dictionary<int, AnimatorControllerParameterType> m_Parameters = new();
        private Animator m_Animator;
        private RuntimeAnimatorController m_BoundController;

        public Animator Animator => m_Animator;

        public void Bind(Animator animator)
        {
            m_Animator = animator;
            m_BoundController = null;
            m_Parameters.Clear();
        }

        public void Update(float speed, NpcStateId state, bool isAggressive, float anger)
        {
            if (!EnsureParameters())
            {
                return;
            }

            SetFloat(s_SpeedHash, speed);
            SetInt(s_StateHash, (int)state);
            SetBool(s_AggressiveHash, isAggressive);
            SetFloat(s_AngerHash, anger);
        }

        /// <summary>
        /// Срабатывает триггер, если он есть в контроллере. Возвращает false, если такого триггера нет.
        /// </summary>
        public bool TrySetTrigger(string triggerName)
        {
            if (string.IsNullOrEmpty(triggerName) || !EnsureParameters())
            {
                return false;
            }

            int hash = Animator.StringToHash(triggerName);

            if (!m_Parameters.TryGetValue(hash, out var type) || type != AnimatorControllerParameterType.Trigger)
            {
                return false;
            }

            m_Animator.SetTrigger(hash);
            return true;
        }

        private bool EnsureParameters()
        {
            if (m_Animator == null || !m_Animator.isActiveAndEnabled || m_Animator.runtimeAnimatorController == null)
            {
                return false;
            }

            if (m_BoundController != m_Animator.runtimeAnimatorController)
            {
                m_BoundController = m_Animator.runtimeAnimatorController;
                m_Parameters.Clear();

                foreach (var parameter in m_Animator.parameters)
                {
                    m_Parameters[parameter.nameHash] = parameter.type;
                }
            }

            return true;
        }

        private void SetFloat(int hash, float value)
        {
            if (m_Parameters.TryGetValue(hash, out var type) && type == AnimatorControllerParameterType.Float)
            {
                m_Animator.SetFloat(hash, value);
            }
        }

        private void SetInt(int hash, int value)
        {
            if (m_Parameters.TryGetValue(hash, out var type) && type == AnimatorControllerParameterType.Int)
            {
                m_Animator.SetInteger(hash, value);
            }
        }

        private void SetBool(int hash, bool value)
        {
            if (m_Parameters.TryGetValue(hash, out var type) && type == AnimatorControllerParameterType.Bool)
            {
                m_Animator.SetBool(hash, value);
            }
        }
    }
}
