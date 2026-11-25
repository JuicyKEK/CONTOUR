using System.Collections.Generic;
using Game.Scripts.NPC.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.TestScenes.EnemyShowcase
{
    /// <summary>
    /// Сцена просмотра противников: вид глазами игрока в анимациях убийства - так, как его увидит игрок в игре.
    /// Строка сверху: «&lt; монстр &gt;» и «&lt; убийство &gt;» - выбор среди рядов с убийством (EnemyShowcaseKillStandIn),
    /// кнопки и строка состояния. Выбрал - камера сцены встаёт в глаза игрока этого ряда (кость PlayerEye монстра, где
    /// голова манекена) и идёт за ними, манекен ряда прячется (в игре у игрока нет тела), анимация начинается сначала,
    /// с момента затемнения из профиля монстра экран темнеет, как в игре. Поле зрения и ближняя плоскость - как у камеры
    /// убийства игрока (PlayerKillCameraView). Ряды выключенных групп монстров тоже в списке: пока такой ряд на экране,
    /// его группа включена (только в Play), потом снова выключается.
    /// Клавиши: Tab / Shift+Tab - следующее / предыдущее убийство, PageDown / PageUp - следующий / предыдущий монстр,
    /// R - смотреть (заново), F - затемнение вкл/выкл, Esc - общий вид, H - спрятать/показать строку.
    /// Только для показа.
    /// </summary>
    public class EnemyShowcaseKillView : MonoBehaviour
    {
        private const string Hint = "     Tab - убийство, PgUp/PgDn - монстр, R - заново, F - затемнение, Esc - общий вид, H - спрятать";

        [Tooltip("Камера сцены, которую ставим в глаза игрока.")]
        [SerializeField] private Camera m_Camera;

        [SerializeField] private float m_FieldOfView = 60f;
        [SerializeField] private float m_NearClipPlane = 0.02f;

        [Tooltip("За сколько секунд экран темнеет - как у бота в игре (NpcController, «Сколько секунд экран темнеет перед загрузкой»).")]
        [SerializeField, Min(0.01f)] private float m_FadeDuration = 1.2f;

        [Header("Строка")]
        [Tooltip("Строка целиком (H прячет её, затемнение остаётся).")]
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private UnityEngine.UI.Button m_MonsterPrevButton;
        [SerializeField] private UnityEngine.UI.Button m_MonsterNextButton;
        [SerializeField] private TMP_Text m_MonsterLabel;
        [SerializeField] private UnityEngine.UI.Button m_KillPrevButton;
        [SerializeField] private UnityEngine.UI.Button m_KillNextButton;
        [SerializeField] private TMP_Text m_KillLabel;
        [SerializeField] private UnityEngine.UI.Button m_PlayButton;
        [SerializeField] private TMP_Text m_PlayButtonLabel;
        [SerializeField] private UnityEngine.UI.Button m_FadeButton;
        [SerializeField] private TMP_Text m_FadeButtonLabel;
        [SerializeField] private UnityEngine.UI.Button m_BackButton;
        [SerializeField] private TMP_Text m_Status;
        [Tooltip("Чёрный экран во весь кадр - затемнение, как в игре.")]
        [SerializeField] private UnityEngine.UI.Image m_Fade;

        private readonly List<EnemyShowcaseKillStandIn> m_Rows = new();
        private readonly List<string> m_Monsters = new();
        private readonly List<GameObject> m_Activated = new();
        private int m_Cursor;              // выбранный ряд (показан в строке)
        private bool m_IsViewing;          // камера в глазах игрока выбранного ряда
        private bool m_IsFadeOn = true;
        private bool m_HasKill;
        private NpcKillClip m_Kill;
        private Vector3 m_HomePosition;
        private Quaternion m_HomeRotation;
        private float m_HomeFieldOfView;
        private float m_HomeNearClipPlane;

        private void Start()
        {
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
            }

            foreach (var row in FindObjectsByType<EnemyShowcaseKillStandIn>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (row.gameObject.scene == gameObject.scene)
                {
                    m_Rows.Add(row);
                }
            }

            m_Rows.Sort((a, b) => string.CompareOrdinal(Monster(a) + "/" + State(a), Monster(b) + "/" + State(b)));

            foreach (var row in m_Rows)
            {
                if (!m_Monsters.Contains(Monster(row)))
                {
                    m_Monsters.Add(Monster(row));
                }
            }

            m_MonsterPrevButton.onClick.AddListener(() => StepMonster(-1));
            m_MonsterNextButton.onClick.AddListener(() => StepMonster(1));
            m_KillPrevButton.onClick.AddListener(() => StepKill(-1));
            m_KillNextButton.onClick.AddListener(() => StepKill(1));
            m_PlayButton.onClick.AddListener(Play);
            m_FadeButton.onClick.AddListener(ToggleFade);
            m_BackButton.onClick.AddListener(Back);
            SetFadeAlpha(0f);
            Refresh();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;

            if (keyboard == null || m_Rows.Count == 0)
            {
                return;
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                StepKill(keyboard.shiftKey.isPressed ? -1 : 1);
            }
            else if (keyboard.pageDownKey.wasPressedThisFrame)
            {
                StepMonster(1);
            }
            else if (keyboard.pageUpKey.wasPressedThisFrame)
            {
                StepMonster(-1);
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                Play();
            }
            else if (keyboard.fKey.wasPressedThisFrame)
            {
                ToggleFade();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Back();
            }
            else if (keyboard.hKey.wasPressedThisFrame && m_Panel != null)
            {
                m_Panel.SetActive(!m_Panel.activeSelf);
            }
        }

        private void LateUpdate()
        {
            if (!m_IsViewing)
            {
                return;
            }

            var row = m_Rows[m_Cursor];

            if (row.Eye != null)
            {
                m_Camera.transform.SetPositionAndRotation(row.Eye.position, row.Eye.rotation);
            }

            float time = 0f;
            float length = 0f;
            var clip = row.Clip;

            if (row.Animator != null && clip != null)
            {
                var info = row.Animator.GetCurrentAnimatorStateInfo(0);

                if (info.IsName(clip.State))
                {
                    length = info.length;
                    time = Mathf.Min(info.normalizedTime, 1f) * length;
                }
            }

            // Как в игре: затемнение по кривой убийства (игрок задыхается) до FadeAt, с FadeAt - экран темнеет до конца.
            float darkness = m_Kill.Darkness != null && m_Kill.Darkness.length > 0
                ? Mathf.Clamp01(m_Kill.Darkness.Evaluate(Mathf.Min(time, m_Kill.FadeAt)))
                : 0f;
            float fade = Mathf.Clamp01((time - m_Kill.FadeAt) / m_FadeDuration);
            SetFadeAlpha(m_IsFadeOn && m_HasKill ? Mathf.Lerp(darkness, 1f, fade) : 0f);
            m_Status.text = $"{time:F1} / {length:F1} c" + (m_HasKill ? $",  затемнение с {m_Kill.FadeAt:F1} c" : "") + Hint;
        }

        /// <summary>Встать в глаза игрока ряда index и начать его анимацию сначала.</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= m_Rows.Count)
            {
                return;
            }

            if (!m_IsViewing)
            {
                m_HomePosition = m_Camera.transform.position;
                m_HomeRotation = m_Camera.transform.rotation;
                m_HomeFieldOfView = m_Camera.fieldOfView;
                m_HomeNearClipPlane = m_Camera.nearClipPlane;
            }
            else
            {
                m_Rows[m_Cursor].SetVisible(true);
            }

            RestoreActivated();
            m_Cursor = index;
            m_IsViewing = true;
            var row = m_Rows[index];
            Activate(row.transform);
            row.SetVisible(false);
            m_HasKill = row.TryGetKill(out m_Kill);
            m_Camera.fieldOfView = m_FieldOfView;
            m_Camera.nearClipPlane = m_NearClipPlane;
            row.Clip?.Replay();
            Refresh();
        }

        /// <summary>Следующее (direction 1) или предыдущее (-1) убийство того же монстра.</summary>
        private void StepKill(int direction)
        {
            if (m_Rows.Count == 0)
            {
                return;
            }

            if (!m_IsViewing)
            {
                Select(m_Cursor);
                return;
            }

            string monster = Monster(m_Rows[m_Cursor]);
            int i = m_Cursor;

            do
            {
                i = (i + direction + m_Rows.Count) % m_Rows.Count;
            }
            while (Monster(m_Rows[i]) != monster);

            Select(i);
        }

        /// <summary>Первое убийство следующего (direction 1) или предыдущего (-1) монстра.</summary>
        private void StepMonster(int direction)
        {
            if (m_Monsters.Count == 0)
            {
                return;
            }

            int current = m_Monsters.IndexOf(Monster(m_Rows[m_Cursor]));
            string target = m_Monsters[(current + direction + m_Monsters.Count) % m_Monsters.Count];
            Select(m_Rows.FindIndex(r => Monster(r) == target));
        }

        private void Play()
        {
            if (m_IsViewing)
            {
                m_Rows[m_Cursor].Clip?.Replay();
            }
            else if (m_Rows.Count > 0)
            {
                Select(m_Cursor);
            }
        }

        private void ToggleFade()
        {
            m_IsFadeOn = !m_IsFadeOn;
            Refresh();
        }

        private void Back()
        {
            if (!m_IsViewing)
            {
                return;
            }

            m_Rows[m_Cursor].SetVisible(true);
            RestoreActivated();
            m_IsViewing = false;
            m_Camera.transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
            m_Camera.fieldOfView = m_HomeFieldOfView;
            m_Camera.nearClipPlane = m_HomeNearClipPlane;
            SetFadeAlpha(0f);
            Refresh();
        }

        private void Refresh()
        {
            if (m_Rows.Count == 0)
            {
                m_MonsterLabel.text = "-";
                m_KillLabel.text = "-";
                m_Status.text = "В сцене нет рядов с убийствами.";
                return;
            }

            var row = m_Rows[m_Cursor];
            string monster = Monster(row);
            int count = 0;
            int number = 0;

            foreach (var r in m_Rows)
            {
                if (Monster(r) == monster)
                {
                    count++;

                    if (r == row)
                    {
                        number = count;
                    }
                }
            }

            m_MonsterLabel.text = $"{monster}  {m_Monsters.IndexOf(monster) + 1}/{m_Monsters.Count}";
            m_KillLabel.text = $"{State(row)}  {number}/{count}";
            m_PlayButtonLabel.text = m_IsViewing ? "Заново (R)" : "Смотреть (R)";
            m_FadeButtonLabel.text = m_IsFadeOn ? "Затемнение: вкл (F)" : "Затемнение: выкл (F)";

            if (!m_IsViewing)
            {
                m_Status.text = "Выберите убийство - камера встанет в глаза игрока." + Hint;
            }
        }

        /// <summary>Включить выключенные объекты над рядом (его группу), запомнив их.</summary>
        private void Activate(Transform row)
        {
            for (var t = row; t != null; t = t.parent)
            {
                if (!t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(true);
                    m_Activated.Add(t.gameObject);
                }
            }
        }

        private void RestoreActivated()
        {
            foreach (var go in m_Activated)
            {
                if (go != null)
                {
                    go.SetActive(false);
                }
            }

            m_Activated.Clear();
        }

        private static string Monster(EnemyShowcaseKillStandIn row)
        {
            return row.Animator != null ? row.Animator.name : row.name;
        }

        private static string State(EnemyShowcaseKillStandIn row)
        {
            return row.Clip != null ? row.Clip.State : row.name;
        }

        private void SetFadeAlpha(float alpha)
        {
            var color = m_Fade.color;
            color.a = alpha;
            m_Fade.color = color;
        }
    }
}
