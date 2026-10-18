using Game.Scripts.LocationScene.Runtime.Interfaces;
using Game.Scripts.Story;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.LocationScene.Runtime.Controllers
{
    /// <summary>
    /// Чекпоинт на локации: пустой объект на полу в точке, где должен появляться игрок; синяя стрелка
    /// (ось Z) - куда он смотрит. Ключ чекпоинта выбирается из каталога ключей сюжета (вид Checkpoint).
    /// Чекпоинт игроку назначает сюжет - действие графа "Save/Set Player Checkpoint" с этим же ключом,
    /// а чекпоинт по умолчанию для локации указывается в её LocationDefinitionSO.
    /// </summary>
    [JDIMonoController]
    public class LocationCheckpoint : MonoBehaviour, ILocationCheckpoint
    {
        [Tooltip("Ключ чекпоинта, уникальный в пределах игры. Пишется в сохранение игрока.")]
        [SerializeField, StoryKey(StoryKeyKind.Checkpoint)] private string m_CheckpointId;

        public string CheckpointId => m_CheckpointId;
        public string ScenePath => gameObject.scene.path;
        public Vector3 Position => transform.position;
        public Quaternion Rotation => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        private void OnDrawGizmos()
        {
            var position = transform.position;
            var eyes = position + Vector3.up * 1.6f;

            Gizmos.color = new Color(0.3f, 1f, 0.4f);
            Gizmos.DrawWireSphere(position, 0.25f);
            Gizmos.DrawLine(position, eyes);
            Gizmos.DrawLine(eyes, eyes + Rotation * Vector3.forward * 0.8f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(eyes, string.IsNullOrEmpty(m_CheckpointId) ? "чекпоинт без ключа" : m_CheckpointId);
#endif
        }
    }
}
