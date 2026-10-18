using System;
using Game.Scripts.InteractionObjects.Interfaces;
using Game.Scripts.NPC.Runtime.Data;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Облики бота - для мимика, который на глазах превращается из человека в монстра. Каждый облик - свой
    /// объект модели (включён только текущий), свой Animator и при желании свои звуки и манера открывать двери.
    /// Облик переключает NpcController.ChangeForm (шаг графа "NPC/Change Form"). Без этого компонента
    /// бот работает с Animator и звуками из NpcController/NpcTypeDefinitionSO.
    /// </summary>
    public class NpcAppearance : MonoBehaviour
    {
        [Serializable]
        public class Form
        {
            [Tooltip("Ключ облика (например 'Human', 'Monster') - по нему облик выбирают граф и сюжет.")]
            public string Key;

            [Tooltip("Объект модели этого облика: включается, когда облик активен, остальные выключаются.")]
            public GameObject Root;

            [Tooltip("Animator модели облика (может быть пустым - тогда анимаций у облика нет, например у примитива).")]
            public Animator Animator;

            [Tooltip("Анимации и звуки облика (шаги, голос, действия). Пусто - профиль из NpcTypeDefinitionSO.")]
            public NpcAnimationSoundProfileSO SoundProfile;

            [Tooltip("Как этот облик открывает двери (человек - обычно, монстр - резко).")]
            public DoorOpenerKind DoorOpener = DoorOpenerKind.Human;
        }

        [SerializeField] private Form[] m_Forms = Array.Empty<Form>();
        [Tooltip("Облик на старте. Пусто - первый в списке.")]
        [SerializeField] private string m_StartForm;

        private Form m_Current;

        public event Action<Form> FormChanged;

        public Form Current
        {
            get
            {
                EnsureStartForm();
                return m_Current;
            }
        }

        public string CurrentKey => Current?.Key;

        private void Awake()
        {
            EnsureStartForm();
        }

        /// <summary>
        /// Переключить облик. Возвращает false, если облика с таким ключом нет.
        /// </summary>
        public bool SetForm(string key)
        {
            var form = Find(key);

            if (form == null)
            {
                Debug.LogWarning($"[NPC] {name}: облика '{key}' нет в NpcAppearance.", this);
                return false;
            }

            Apply(form);
            return true;
        }

        private void EnsureStartForm()
        {
            if (m_Current != null || m_Forms == null || m_Forms.Length == 0)
            {
                return;
            }

            Apply(Find(m_StartForm) ?? m_Forms[0]);
        }

        private void Apply(Form form)
        {
            m_Current = form;

            foreach (var other in m_Forms)
            {
                if (other?.Root != null)
                {
                    other.Root.SetActive(other == form);
                }
            }

            FormChanged?.Invoke(form);
        }

        private Form Find(string key)
        {
            if (string.IsNullOrEmpty(key) || m_Forms == null)
            {
                return null;
            }

            foreach (var form in m_Forms)
            {
                if (form != null && form.Key == key)
                {
                    return form;
                }
            }

            return null;
        }
    }
}
