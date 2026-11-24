using DG.Tweening;
using Game.Scripts.Audio.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.View
{
    /// <summary>
    /// Ключ в замке двери (пара к InteractionDoorWithKey). Дверь отперли ключом (IsLocked стал false) - перед замочной
    /// скважиной появляется ключ, входит в неё, проворачивается (на повороте щёлкает замок - IsPlaySound, на него подписан
    /// SoundPlayer) и пропадает. Вся анимация, от появления ключа до исчезновения, - Duration.
    /// Дверь отперта уже на старте или пришла отпертой из сохранения - ключа нет.
    /// </summary>
    public class DoorKeyUnlockView : MonoBehaviour, ISoundPlay
    {
        public Subject<Unit> IsPlaySound => m_IsPlaySound;

        [SerializeField] private InteractionDoor m_Door;

        [Tooltip("Замочная скважина: ключ входит в неё вдоль её оси Z (Z смотрит в дверь) и поворачивается вокруг этой оси. " +
                 "Ключ появляется её дочерним объектом - у неё и её родителей масштаб должен быть 1.")]
        [SerializeField] private Transform m_Keyhole;

        [Tooltip("Ключ (только модель): кончик бородки - в начале координат префаба, ключ смотрит бородкой по +Z.")]
        [SerializeField] private GameObject m_KeyPrefab;

        [Tooltip("Вся анимация, от появления ключа до его исчезновения, с.")]
        [SerializeField, Min(0.1f)] private float m_Duration = 2f;

        [Tooltip("На каком расстоянии перед скважиной ключ появляется, м.")]
        [SerializeField, Min(0f)] private float m_StartDistance = 0.08f;

        [Tooltip("Насколько глубоко ключ входит в скважину, м.")]
        [SerializeField, Min(0f)] private float m_InsertDepth = 0.04f;

        [Tooltip("На сколько градусов проворачивается ключ.")]
        [SerializeField] private float m_TurnAngle = 90f;

        private readonly CompositeDisposable m_Disposables = new();
        private readonly Subject<Unit> m_IsPlaySound = new();
        private GameObject m_Key;
        private Sequence m_Sequence;

        private void Start()
        {
            if (m_Door == null || m_Keyhole == null || m_KeyPrefab == null)
            {
                Debug.LogWarning($"[{nameof(DoorKeyUnlockView)}] {name}: не назначены дверь, скважина или ключ.", this);
                return;
            }

            m_Door.IsLocked
                .Skip(1)
                .Where(isLocked => !isLocked)
                .Subscribe(_ => OnUnlocked())
                .AddTo(m_Disposables);
        }

        private void OnUnlocked()
        {
            // Состояние пришло из сохранения - дверь уже была отперта, ключ не показываем.
            if (m_Door.IsRestoringState || m_Sequence != null)
            {
                return;
            }

            PlayKey();
        }

        private void PlayKey()
        {
            m_Key = Instantiate(m_KeyPrefab, m_Keyhole);
            Transform key = m_Key.transform;
            Vector3 scale = m_KeyPrefab.transform.localScale;
            key.localPosition = new Vector3(0f, 0f, -m_StartDistance);
            key.localRotation = Quaternion.identity;
            key.localScale = Vector3.zero;

            // Доли Duration: появляется 0.2, входит 0.25, проворачивается 0.3, держится 0.1, пропадает 0.15.
            float appear = m_Duration * 0.2f, insert = m_Duration * 0.25f, turn = m_Duration * 0.3f;
            float hold = m_Duration * 0.1f, vanish = m_Duration * 0.15f;

            m_Sequence = DOTween.Sequence()
                .Append(key.DOScale(scale, appear).SetEase(Ease.OutBack))
                .Append(key.DOLocalMove(new Vector3(0f, 0f, m_InsertDepth), insert).SetEase(Ease.InOutQuad))
                .Append(key.DOLocalRotate(new Vector3(0f, 0f, m_TurnAngle), turn).SetEase(Ease.InOutCubic))
                .InsertCallback(appear + insert + turn * 0.8f, () => m_IsPlaySound.OnNext(Unit.Default))
                .AppendInterval(hold)
                .Append(key.DOScale(Vector3.zero, vanish).SetEase(Ease.InBack))
                .OnKill(RemoveKey);
        }

        private void RemoveKey()
        {
            m_Sequence = null;

            if (m_Key != null)
            {
                Destroy(m_Key);
                m_Key = null;
            }
        }

        private void OnDestroy()
        {
            m_Sequence?.Kill();
            m_Disposables.Dispose();
            m_IsPlaySound.Dispose();
        }
    }
}
