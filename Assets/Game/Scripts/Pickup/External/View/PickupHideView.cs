using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Game.Scripts.Pickup.View
{
    /// <summary>
    /// Убирает подобранный предмет со сцены (IPickupObject.IsPickedUp на этом же объекте). С задержкой (например, пока
    /// доигрывает звук подбора) предмет сразу перестаёт быть виден и мешать - выключаются его рендереры и коллайдеры, -
    /// а выключается или удаляется потом.
    /// </summary>
    public class PickupHideView : MonoBehaviour
    {
        [Tooltip("Удалить предмет со сцены. Выключено - объект только выключается.")]
        [SerializeField] private bool m_Destroy = true;
        [Tooltip("Через сколько секунд после подбора выключить или удалить объект (например, пока доигрывает звук), с. " +
                 "До этого предмет уже не виден и не мешает.")]
        [SerializeField, Min(0f)] private float m_Delay;

        private readonly CompositeDisposable m_Disposables = new();

        private void Start()
        {
            if (!TryGetComponent(out IPickupObject pickup))
            {
                Debug.LogWarning($"[{nameof(PickupHideView)}] {name}: на объекте нет подбираемого компонента (IPickupObject).", this);
                return;
            }

            pickup.IsPickedUp
                .Where(isPickedUp => isPickedUp)
                .Take(1)
                .Subscribe(_ => HideAsync().Forget())
                .AddTo(m_Disposables);
        }

        private async UniTaskVoid HideAsync()
        {
            if (m_Delay > 0f)
            {
                HideRenderersAndColliders();

                bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(m_Delay),
                        cancellationToken: this.GetCancellationTokenOnDestroy())
                    .SuppressCancellationThrow();

                if (isCanceled)
                {
                    return;
                }
            }

            if (m_Destroy)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void HideRenderersAndColliders()
        {
            foreach (var itemRenderer in GetComponentsInChildren<Renderer>())
            {
                itemRenderer.enabled = false;
            }

            foreach (var itemCollider in GetComponentsInChildren<Collider>())
            {
                itemCollider.enabled = false;
            }
        }

        private void OnDestroy()
        {
            m_Disposables.Dispose();
        }
    }
}
