using System;
using System.Collections.Generic;
using JuicyDI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Запасы, которые бот выкладывает игроку (сотрудник газовой службы): подбираемые предметы-префабы
    /// в точке выдачи. Полный набор - если игрок соблюдал протокол, урезанный - если бот его ударил.
    /// Вызывается шагом графа "NPC/Drop Supplies" или из UnityEvent (DropFull/DropReduced).
    /// Созданным предметам JuicyDI прокидывает зависимости (инвентарь), как предметам, лежащим в сцене.
    /// </summary>
    public class NpcSupplyDrop : MonoBehaviour
    {
        [Serializable]
        public struct SupplyItem
        {
            [Tooltip("Подбираемый предмет (префаб с InventoryInteraction или другим IInteraction).")]
            public GameObject Prefab;

            [Tooltip("Сколько штук в полном наборе.")]
            [Min(0)] public int FullCount;

            [Tooltip("Сколько штук в урезанном наборе (бот ударил игрока).")]
            [Min(0)] public int ReducedCount;
        }

        [SerializeField] private SupplyItem[] m_Items = Array.Empty<SupplyItem>();
        [Tooltip("Где выложить. Пусто - перед ботом.")]
        [SerializeField] private Transform m_DropPoint;
        [Tooltip("Разброс предметов вокруг точки (метры).")]
        [SerializeField, Min(0f)] private float m_Spread = 0.35f;

        private readonly List<GameObject> m_Dropped = new();

        public IReadOnlyList<GameObject> Dropped => m_Dropped;

        public void DropFull()
        {
            Drop(false);
        }

        public void DropReduced()
        {
            Drop(true);
        }

        public void Drop(bool isReduced)
        {
            var origin = m_DropPoint != null
                ? m_DropPoint.position
                : transform.position + transform.forward * 0.8f;

            int index = 0;

            foreach (var item in m_Items)
            {
                if (item.Prefab == null)
                {
                    continue;
                }

                int count = isReduced ? item.ReducedCount : item.FullCount;

                for (int i = 0; i < count; i++)
                {
                    var position = origin + SpreadOffset(index++);
                    var rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                    var supply = Instantiate(item.Prefab, position, rotation);
                    SceneManager.MoveGameObjectToScene(supply, gameObject.scene);
                    Inject(supply);
                    m_Dropped.Add(supply);
                }
            }

            Debug.Log($"[NPC] {name}: выложены запасы ({(isReduced ? "урезанный" : "полный")} набор, {index} шт.).", this);
        }

        private Vector3 SpreadOffset(int index)
        {
            if (index == 0 || m_Spread <= 0f)
            {
                return Vector3.zero;
            }

            // Раскладываем по спирали, чтобы предметы не лежали друг в друге.
            float angle = index * 2.4f;
            float radius = m_Spread * Mathf.Sqrt(index);
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static void Inject(GameObject supply)
        {
            var container = BinController.GetContext();

            if (container == null)
            {
                return;
            }

            foreach (var behaviour in supply.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null)
                {
                    container.LateInjection(behaviour);
                }
            }
        }
    }
}
