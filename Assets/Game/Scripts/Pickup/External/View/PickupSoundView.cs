using Game.Scripts.Audio.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.Pickup.View
{
    /// <summary>
    /// Звук подбора: предмет подобран (IPickupObject.IsPickedUp на этом же объекте) - IsPlaySound, на него подписан
    /// SoundPlayer. Чтобы звук не оборвался, PickupHideView должен убирать объект не раньше, чем звук доиграет (Delay).
    /// </summary>
    public class PickupSoundView : MonoBehaviour, ISoundPlay
    {
        public Subject<Unit> IsPlaySound => m_IsPlaySound;

        private readonly Subject<Unit> m_IsPlaySound = new();
        private readonly CompositeDisposable m_Disposables = new();

        private void Start()
        {
            if (!TryGetComponent(out IPickupObject pickup))
            {
                Debug.LogWarning($"[{nameof(PickupSoundView)}] {name}: на объекте нет подбираемого компонента (IPickupObject).", this);
                return;
            }

            pickup.IsPickedUp
                .Skip(1)
                .Where(isPickedUp => isPickedUp)
                .Subscribe(_ => m_IsPlaySound.OnNext(Unit.Default))
                .AddTo(m_Disposables);
        }

        private void OnDestroy()
        {
            m_Disposables.Dispose();
            m_IsPlaySound.Dispose();
        }
    }
}
