using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Задел под анимации бота: каждый кадр передаёт в Animator скорость, состояние, агрессию и злость,
    /// а действия (удар, превращение...) - триггерами. Параметр выставляется, только если он есть
    /// в контроллере аниматора, поэтому бот работает и с пустым/старым контроллером, и вовсе без Animator
    /// (примитивы). Имена параметров - константы ниже; их же ждёт шаблонный контроллер NpcTemplate.
    ///
    /// Если триггера с именем действия в контроллере нет, но есть состояние с таким именем (бытовые анимации монстров:
    /// мытьё посуды, еда - к ним в контроллере нет переходов), аниматор плавно переходит в это состояние. Как только
    /// бот пойдёт (Speed больше 0.1), аниматор плавно возвращается в состояние, из которого действие началось
    /// (обычно Idle с переходами по Speed).
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

        private const float StateFadeSeconds = 0.25f;
        private const float MovingSpeed = 0.1f;

        private readonly Dictionary<int, AnimatorControllerParameterType> m_Parameters = new();
        private Animator m_Animator;
        private RuntimeAnimatorController m_BoundController;
        private int m_ReturnStateHash; // куда вернуться, когда бот пойдёт после действия-состояния (0 - некуда)

        public Animator Animator => m_Animator;

        public void Bind(Animator animator)
        {
            m_Animator = animator;
            m_BoundController = null;
            m_Parameters.Clear();
            m_ReturnStateHash = 0;
        }

        public void Update(float speed, NpcStateId state, bool isAggressive, float anger)
        {
            if (!EnsureParameters())
            {
                return;
            }

            if (m_ReturnStateHash != 0 && speed > MovingSpeed)
            {
                m_Animator.CrossFadeInFixedTime(m_ReturnStateHash, StateFadeSeconds, 0);
                m_ReturnStateHash = 0;
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
                return TryPlayState(hash);
            }

            m_ReturnStateHash = 0;
            m_Animator.SetTrigger(hash);
            return true;
        }

        /// <summary>
        /// Плавно перейти в состояние аниматора (слой 0) с этим именем, если оно есть. Возвращает false, если такого нет.
        /// </summary>
        private bool TryPlayState(int stateHash)
        {
            if (!m_Animator.HasState(0, stateHash))
            {
                return false;
            }

            if (m_ReturnStateHash == 0)
            {
                int current = m_Animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
                m_ReturnStateHash = current != stateHash ? current : 0;
            }
            else if (m_ReturnStateHash == stateHash)
            {
                m_ReturnStateHash = 0;
            }

            m_Animator.CrossFadeInFixedTime(stateHash, StateFadeSeconds, 0);
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
