using Game.Scripts.Audio.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.View
{
    /// <summary>
    /// Навесной замок на двери. Дверь отперли (IsLocked стал false) - замок ломается: отрывается от створки, падает
    /// на пол и звучит (SoundPlayer, подписанный на IsPlaySound). Дверь отперта уже на старте или пришла отпертой
    /// из сохранения - замок просто падает, без звука. Вешается на модель замка.
    /// </summary>
    public class BreakableLockView : MonoBehaviour, ISoundPlay
    {
        public Subject<Unit> IsPlaySound => m_IsPlaySound;

        [SerializeField] private InteractionDoor m_Door;
        [Tooltip("Скорость, с которой сломанный замок отлетает от двери, м/с.")]
        [SerializeField, Min(0f)] private float m_DropPush = 0.6f;
        [Tooltip("Как быстро сломанный замок крутится в падении, рад/с.")]
        [SerializeField, Min(0f)] private float m_DropSpin = 4f;
        [SerializeField, Min(0.01f)] private float m_Mass = 0.5f;

        private readonly CompositeDisposable m_Disposables = new();
        private readonly Subject<Unit> m_IsPlaySound = new();
        private bool m_IsBroken;

        private void Start()
        {
            if (m_Door == null)
            {
                Debug.LogWarning($"[{nameof(BreakableLockView)}] {name}: не назначена дверь.", this);
                return;
            }

            if (!m_Door.IsLocked.CurrentValue)
            {
                Break(false);
                return;
            }

            m_Door.IsLocked.Skip(1)
                .Where(isLocked => !isLocked)
                .Subscribe(_ => Break(!m_Door.IsRestoringState))
                .AddTo(m_Disposables);
        }

        private void Break(bool isPlaySound)
        {
            if (m_IsBroken)
            {
                return;
            }

            m_IsBroken = true;

            Transform door = m_Door.transform;
            Vector3 normal = DoorSwing.GetLeafNormal(door);
            float side = Mathf.Sign(Vector3.Dot(transform.position - door.position, normal));

            // Замок больше не висит на створке: не поворачивается вместе с ней и падает сам.
            transform.SetParent(door.parent, true);

            if (!TryGetComponent(out Collider _))
            {
                gameObject.AddComponent<BoxCollider>();
            }

            if (!TryGetComponent(out Rigidbody body))
            {
                body = gameObject.AddComponent<Rigidbody>();
            }

            body.mass = m_Mass;
            body.linearVelocity = normal * (side * m_DropPush);
            body.angularVelocity = Random.insideUnitSphere * m_DropSpin;

            if (isPlaySound)
            {
                m_IsPlaySound.OnNext(Unit.Default);
            }
        }

        private void OnDestroy()
        {
            m_Disposables.Dispose();
            m_IsPlaySound.Dispose();
        }
    }
}
