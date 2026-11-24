using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Game.Scripts.InfectionZone.External.View
{
    /// <summary>
    /// Отметины на месте, где при большем заражении что-то вырастет (например, мост через провал): видны, пока этого ещё
    /// нет, и прячутся, пока оно есть. Смотрит, виден ли сам объект (Spawn Object включён в иерархии), а не на диапазоны
    /// заражения: объект могут включать и выключать несколько объектов диапазона и его вид (плавление) - отметины всё равно
    /// совпадают с тем, что видит игрок.
    /// Стоит на всегда включённом объекте и включает/выключает дочерний объект с отметинами (Markers).
    /// </summary>
    public class InfectionSpawnMarkersView : MonoBehaviour
    {
        [Tooltip("То, что вырастет (например, Growth моста): пока его не видно, отметины видны.")]
        [SerializeField] private GameObject m_SpawnObject;

        [Tooltip("Отметины: выключаются, пока то, что они обозначают, видно.")]
        [SerializeField] private GameObject m_Markers;

        private void Start()
        {
            if (m_SpawnObject == null || m_Markers == null)
            {
                Debug.LogWarning($"[{nameof(InfectionSpawnMarkersView)}] {name}: не назначены объект или отметины.", this);
                return;
            }

            UniTaskAsyncEnumerable.EveryValueChanged(m_SpawnObject, spawn => spawn.activeInHierarchy)
                .ForEachAsync(isSpawned => m_Markers.SetActive(!isSpawned), this.GetCancellationTokenOnDestroy())
                .Forget();
        }
    }
}
