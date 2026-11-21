using System;
using System.Collections.Generic;
using AZE.AdvancedFirstPerson;
using Game.Scripts.InputController;
using Game.Scripts.Inventory.External.View;
using Game.Scripts.Story;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Game.Scripts.Inventory.External.Controllers
{
    /// <summary>
    /// Осмотр предмета при первом подборе (по примеру Resident Evil). Лежит в префабе игрока (объект ItemInspection).
    ///
    /// Как работает:
    /// 1. Предмет (InventoryInteraction) при подборе вызывает <see cref="TryInspect"/>. Если предмет с этим ключом
    ///    уже осматривали - осмотра нет, предмет сразу попадает в инвентарь.
    /// 2. С предмета снимается копия (только меши и материалы, без коллайдеров, света и скриптов) на отдельном слое
    ///    (InspectItem), сам предмет на сцене прячется. Копия летит от места, где лежал предмет, к камере.
    /// 3. Копию рисует отдельная камера (Overlay), которую на время осмотра ставят в стек основной камеры, - поэтому
    ///    предмет не размыт и не проваливается в стены. Основная камера перестаёт видеть слой предмета, а её
    ///    картинку размывает Volume с глубиной резкости (вес плавно растёт до 1).
    /// 4. Снизу - название предмета, подсказка и кнопка "Принять". Зажав ЛКМ и двигая мышь, предмет крутят.
    /// 5. По Esc или кнопке "Принять" предмет уменьшается и летит к ячейке инвентаря; когда долетел -
    ///    вызывается onFinished (предмет кладут в инвентарь), всё возвращается как было.
    ///
    /// На время осмотра: курсор виден, движение и поворот камеры выключены, клавиши E/F/R/TAB/цифры не действуют,
    /// игра по желанию на паузе (Time.timeScale = 0, сам осмотр анимируется по реальному времени).
    ///
    /// Осмотренные ключи хранит этот же компонент (<see cref="IItemInspectionRegistry"/>), сохраняет их
    /// InventorySaveController вместе с инвентарём.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(115)]
    public class ItemInspectionController : MonoBehaviour, ISequence, IUpdateSequence, IItemInspection, IItemInspectionRegistry
    {
        private enum Phase
        {
            None,
            FlyIn,
            Inspect,
            FlyOut
        }

        /// <summary>
        /// Часть предмета из статического батча: её меш - общий склеенный меш куска сцены (вершины уже в мировых
        /// координатах), а сама часть - лишь диапазон его сабмешей. Такую часть не скопировать объектом с
        /// MeshRenderer - она рисуется каждый кадр через Graphics.RenderMesh.
        /// </summary>
        private struct BatchedPart
        {
            public Mesh Mesh;
            public int SubMesh;
            public Material Material;
            // Из пространства вершин склеенного меша - в локальное пространство корня копии.
            public Matrix4x4 MeshToRoot;
        }

        [Header("Ссылки")]
        [Tooltip("Интерфейс осмотра: название предмета, подсказка, кнопка \"Принять\".")]
        [SerializeField] private ItemInspectionView m_View;
        [Tooltip("Камера, которая рисует осматриваемый предмет поверх размытой картинки (Render Type: Overlay). " +
                 "Объект камеры выключен, пока осмотра нет; на время осмотра камера встаёт в стек основной камеры.")]
        [SerializeField] private Camera m_InspectionCamera;
        [Tooltip("Размытие всего, что за предметом: Volume с глубиной резкости. Вес плавно меняется 0 -> 1 -> 0.")]
        [SerializeField] private Volume m_BlurVolume;
        [Tooltip("Панель инвентаря на экране - к её ячейке улетает предмет после осмотра.")]
        [SerializeField] private RectTransform m_InventoryPanel;
        [Tooltip("Элементы интерфейса, которые прячутся на время осмотра (прицел посреди экрана - поверх предмета).")]
        [SerializeField] private GameObject[] m_HideWhileInspecting;

        [Header("Предмет перед камерой")]
        [Tooltip("Слой, на котором рисуется копия предмета. Его видит только камера осмотра.")]
        [SerializeField] private string m_InspectionLayerName = "InspectItem";
        [Tooltip("Расстояние от камеры до предмета при осмотре, м. На вид не влияет (размер подгоняется), " +
                 "важно только для освещения предмета.")]
        [SerializeField, Min(0.1f)] private float m_Distance = 0.6f;
        [Tooltip("Какую часть высоты экрана занимает предмет (по описанной вокруг него сфере).")]
        [SerializeField, Range(0.1f, 0.9f)] private float m_ScreenSize = 0.44f;
        [Tooltip("Сдвиг предмета вверх от центра экрана, доля высоты экрана (чтобы не закрывал подпись).")]
        [SerializeField, Range(-0.4f, 0.4f)] private float m_ScreenOffsetY = 0.06f;
        [Tooltip("Скорость вращения предмета мышью, градусов на пиксель (при высоте экрана 1080).")]
        [SerializeField, Min(0f)] private float m_RotateSpeed = 0.35f;

        [Header("Анимация")]
        [Tooltip("Сколько секунд предмет летит к камере (размытие и подпись появляются за то же время).")]
        [SerializeField, Min(0.01f)] private float m_FlyInDuration = 0.45f;
        [Tooltip("Сколько секунд предмет летит в инвентарь.")]
        [SerializeField, Min(0.01f)] private float m_FlyOutDuration = 0.45f;
        [Tooltip("Размер предмета в конце полёта в инвентарь - доля от размера при осмотре.")]
        [SerializeField, Range(0.01f, 1f)] private float m_EndScale = 0.12f;

        [Header("Игра")]
        [Tooltip("Ставить игру на паузу на время осмотра (как в Resident Evil): монстры, физика и сюжетные " +
                 "таймеры замирают. Звук не останавливается.")]
        [SerializeField] private bool m_PauseGame = true;

        [Inject] private IInputActions m_InputActions;
        [Inject] private IPlayerMoveController m_PlayerMoveController;
        [Inject] private global::Game.Scripts.InputController.InputController m_InputController;
        [Inject] private List<IPlayerControlHandle> m_ControlHandles;

        private readonly HashSet<string> m_InspectedKeys = new();
        private readonly List<Mesh> m_BakedMeshes = new();
        private readonly List<BatchedPart> m_BatchedParts = new();
        private readonly List<RectTransform> m_SlotsBuffer = new();
        // Какие из m_HideWhileInspecting были включены до осмотра - только их и включаем обратно.
        private readonly List<GameObject> m_HiddenObjects = new();

        private Phase m_Phase = Phase.None;
        private float m_PhaseTime;
        private Transform m_Item;
        private int m_ItemLayer;
        private float m_ItemRadius;
        private Action m_OnFinished;

        private Camera m_BaseCamera;
        private int m_BaseCullingMask;
        private bool m_IsPaused;
        private float m_TimeScaleBefore = 1f;

        private Vector3 m_FromPosition;
        private Vector3 m_ToPosition;
        private Quaternion m_FromRotation;
        private Quaternion m_ToRotation;
        private float m_FromScale;
        private float m_ToScale;
        private float m_BlurAtFlyOut;
        private float m_AlphaAtFlyOut;

        private bool m_IsDragging;

        public bool IsInspecting => m_Phase != Phase.None;

        public void MethodInit()
        {
            if (m_InspectionCamera != null)
            {
                m_InspectionCamera.gameObject.SetActive(false);
            }

            SetBlur(0f);
        }

        public void MethodStart()
        {
            m_InputActions?.AddPressingMouseESCAction(Accept);

            if (m_View != null)
            {
                m_View.Init(Accept);
            }
        }

        #region IItemInspectionRegistry

        public bool WasInspected(string itemKey)
        {
            return !string.IsNullOrEmpty(itemKey) && m_InspectedKeys.Contains(itemKey);
        }

        public void MarkInspected(string itemKey)
        {
            if (!string.IsNullOrEmpty(itemKey))
            {
                m_InspectedKeys.Add(itemKey);
            }
        }

        public void GetInspectedKeys(List<string> result)
        {
            result.Clear();
            result.AddRange(m_InspectedKeys);
        }

        public void ClearInspected()
        {
            m_InspectedKeys.Clear();
        }

        #endregion

        public bool TryInspect(IInventoryObject item, string displayName, GameObject source, Action onFinished)
        {
            if (m_Phase != Phase.None || item == null || source == null)
            {
                return false;
            }

            if (WasInspected(item.ObjectKey))
            {
                return false;
            }

            // Курсор уже занят открытой панелью (кассетный плеер) или игрок обездвижен - не перехватываем управление.
            if (m_PlayerMoveController is PlayerMovementStateMachine movement && (movement.IsUseMouse || !movement.IsCanMove))
            {
                return false;
            }

            int layer = LayerMask.NameToLayer(m_InspectionLayerName);

            if (layer < 0)
            {
                Debug.LogError($"[ItemInspection] Нет слоя '{m_InspectionLayerName}' (Project Settings -> Tags and Layers) - " +
                               "предмет кладётся в инвентарь без осмотра.", this);
                return false;
            }

            var baseCamera = Camera.main;

            if (baseCamera == null || m_InspectionCamera == null || m_View == null)
            {
                Debug.LogWarning("[ItemInspection] Нет основной камеры (тег MainCamera), камеры осмотра или интерфейса " +
                                 "осмотра - предмет кладётся в инвентарь без осмотра.", this);
                return false;
            }

            var copy = BuildCopy(source, layer, out float radius);

            if (copy == null)
            {
                return false;
            }

            MarkInspected(item.ObjectKey);

            m_Item = copy;
            m_ItemLayer = layer;
            m_ItemRadius = radius;
            m_OnFinished = onFinished;

            SetupCameras(baseCamera, layer);
            SetupPoses(radius);
            LockControls();

            m_View.Show(string.IsNullOrEmpty(displayName) ? item.ObjectKey : displayName);
            m_IsDragging = false;
            m_Phase = Phase.FlyIn;
            m_PhaseTime = 0f;
            return true;
        }

        public void CustomUpdate()
        {
            if (m_Phase == Phase.None)
            {
                return;
            }

            if (m_Item == null)
            {
                // Копию кто-то удалил (выгрузка сцены) - просто завершаем осмотр.
                Finish();
                return;
            }

            m_PhaseTime += Time.unscaledDeltaTime;

            switch (m_Phase)
            {
                case Phase.FlyIn:
                    UpdateFlyIn();
                    break;
                case Phase.Inspect:
                    UpdateRotation();
                    break;
                case Phase.FlyOut:
                    UpdateFlyOut();
                    break;
            }
        }

        /// <summary>
        /// Esc или кнопка "Принять": предмет улетает в инвентарь.
        /// </summary>
        private void Accept()
        {
            if (m_Phase != Phase.FlyIn && m_Phase != Phase.Inspect)
            {
                return;
            }

            m_IsDragging = false;
            m_View.SetInteractable(false);

            m_FromPosition = m_Item.position;
            m_FromRotation = m_Item.rotation;
            m_FromScale = m_Item.localScale.x;
            m_ToPosition = GetInventoryWorldPoint();
            m_ToScale = m_FromScale * m_EndScale;
            m_BlurAtFlyOut = m_BlurVolume != null ? m_BlurVolume.weight : 0f;
            m_AlphaAtFlyOut = m_View.Alpha;

            m_Phase = Phase.FlyOut;
            m_PhaseTime = 0f;
        }

        private void UpdateFlyIn()
        {
            float t = Mathf.Clamp01(m_PhaseTime / m_FlyInDuration);
            float e = Mathf.SmoothStep(0f, 1f, t);

            m_Item.position = Vector3.LerpUnclamped(m_FromPosition, m_ToPosition, e);
            m_Item.rotation = Quaternion.Slerp(m_FromRotation, m_ToRotation, e);
            m_Item.localScale = Vector3.one * Mathf.Lerp(m_FromScale, m_ToScale, e);

            SetBlur(e);
            m_View.SetAlpha(Mathf.Clamp01((t - 0.3f) / 0.7f));

            if (t >= 1f)
            {
                m_Phase = Phase.Inspect;
                m_PhaseTime = 0f;
            }
        }

        private void UpdateRotation()
        {
            var mouse = Mouse.current;

            if (mouse == null)
            {
                return;
            }

            // Нажатие на кнопке "Принять" не начинает вращение.
            if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                m_IsDragging = true;
            }

            if (!mouse.leftButton.isPressed)
            {
                m_IsDragging = false;
            }

            if (!m_IsDragging)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            float speed = m_RotateSpeed * 1080f / Mathf.Max(Screen.height, 1);
            var cameraTransform = m_InspectionCamera.transform;

            // Мышь вправо - ближняя к игроку сторона предмета уходит вправо, мышь вверх - вверх.
            m_Item.Rotate(cameraTransform.up, -delta.x * speed, Space.World);
            m_Item.Rotate(cameraTransform.right, delta.y * speed, Space.World);
        }

        private void UpdateFlyOut()
        {
            float t = Mathf.Clamp01(m_PhaseTime / m_FlyOutDuration);
            // Разгон к инвентарю.
            float e = t * t;

            m_Item.position = Vector3.LerpUnclamped(m_FromPosition, m_ToPosition, e);
            m_Item.localScale = Vector3.one * Mathf.Lerp(m_FromScale, m_ToScale, e);

            SetBlur(m_BlurAtFlyOut * (1f - t));
            m_View.SetAlpha(m_AlphaAtFlyOut * (1f - Mathf.Clamp01(t / 0.3f)));

            if (t >= 1f)
            {
                Finish();
            }
        }

        private void Finish()
        {
            var onFinished = m_OnFinished;
            m_OnFinished = null;

            Cleanup(true);

            try
            {
                onFinished?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        /// <summary>
        /// Убирает копию, камеру осмотра, размытие и интерфейс, снимает паузу; с restoreControls - возвращает
        /// игроку управление (при уничтожении самого игрока контроллеры движения и ввода уже могут быть удалены).
        /// </summary>
        private void Cleanup(bool restoreControls)
        {
            m_Phase = Phase.None;
            m_IsDragging = false;

            if (m_Item != null)
            {
                Destroy(m_Item.gameObject);
            }

            m_Item = null;

            foreach (var mesh in m_BakedMeshes)
            {
                if (mesh != null)
                {
                    Destroy(mesh);
                }
            }

            m_BakedMeshes.Clear();
            m_BatchedParts.Clear();

            RestoreCameras();
            SetBlur(0f);

            if (m_View != null)
            {
                m_View.Hide();
            }

            if (restoreControls)
            {
                UnlockControls();
            }
            else if (m_IsPaused)
            {
                Time.timeScale = m_TimeScaleBefore;
                m_IsPaused = false;
            }
        }

        private void OnDestroy()
        {
            if (m_Phase != Phase.None)
            {
                m_OnFinished = null;
                Cleanup(false);
            }
        }

        #region Камеры и размытие

        private void SetupCameras(Camera baseCamera, int layer)
        {
            m_BaseCamera = baseCamera;
            m_BaseCullingMask = baseCamera.cullingMask;
            baseCamera.cullingMask &= ~(1 << layer);

            var cameraTransform = m_InspectionCamera.transform;
            cameraTransform.SetPositionAndRotation(baseCamera.transform.position, baseCamera.transform.rotation);
            m_InspectionCamera.fieldOfView = baseCamera.fieldOfView;
            m_InspectionCamera.cullingMask = 1 << layer;

            // Свет осмотра освещает только слой предмета: для основной камеры (она этот слой больше не видит)
            // он отсекается и сцену не подсвечивает.
            foreach (var inspectionLight in m_InspectionCamera.GetComponentsInChildren<Light>(true))
            {
                inspectionLight.cullingMask = 1 << layer;
            }

            m_InspectionCamera.gameObject.SetActive(true);

            var baseData = baseCamera.GetUniversalAdditionalCameraData();

            if (baseData.renderType != CameraRenderType.Base)
            {
                Debug.LogWarning($"[ItemInspection] Камера '{baseCamera.name}' не Base - предмет может не отрисоваться.", this);
            }

            if (!baseData.cameraStack.Contains(m_InspectionCamera))
            {
                baseData.cameraStack.Add(m_InspectionCamera);
            }

            // Части из статического батча рисуются прямо перед рендером камеры осмотра: вызовы RenderMesh из Update
            // в редакторе теряются, когда окно Game перерисовывается без кадра игрового цикла, - предмет пропадал.
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            if (renderingCamera == m_InspectionCamera)
            {
                DrawBatchedParts();
            }
        }

        private void RestoreCameras()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

            if (m_BaseCamera != null)
            {
                m_BaseCamera.cullingMask = m_BaseCullingMask;

                var baseData = m_BaseCamera.GetUniversalAdditionalCameraData();
                baseData.cameraStack.Remove(m_InspectionCamera);
            }

            m_BaseCamera = null;

            if (m_InspectionCamera != null)
            {
                m_InspectionCamera.gameObject.SetActive(false);
            }
        }

        private void SetBlur(float weight)
        {
            if (m_BlurVolume == null)
            {
                return;
            }

            m_BlurVolume.weight = weight;
            m_BlurVolume.enabled = weight > 0f;
        }

        #endregion

        #region Копия предмета и её положения

        /// <summary>
        /// Копия внешнего вида предмета: все включённые MeshRenderer (и SkinnedMeshRenderer - запечённым мешем)
        /// с теми же материалами, на слое осмотра, без теней. Корень копии - в центре габаритов предмета,
        /// вокруг него предмет и вращается. Части из статического батча (предмет помечен Static) объектами
        /// не копируются - их рисует <see cref="DrawBatchedParts"/>.
        /// </summary>
        private Transform BuildCopy(GameObject source, int layer, out float radius)
        {
            radius = 0f;
            var parts = new List<KeyValuePair<Renderer, Mesh>>();
            var batchedRenderers = new List<MeshRenderer>();
            var bounds = new Bounds();
            bool hasBounds = false;

            foreach (var sourceRenderer in source.GetComponentsInChildren<Renderer>(false))
            {
                if (!sourceRenderer.enabled)
                {
                    continue;
                }

                Mesh mesh = null;

                if (sourceRenderer is MeshRenderer batchedRenderer && batchedRenderer.isPartOfStaticBatch)
                {
                    // MeshFilter такой части держит склеенный меш всего куска сцены: копировать его объектом нельзя.
                    if (batchedRenderer.TryGetComponent(out MeshFilter batchedFilter) && batchedFilter.sharedMesh != null)
                    {
                        batchedRenderers.Add(batchedRenderer);
                        EncapsulateBounds(ref bounds, ref hasBounds, sourceRenderer.bounds);
                    }

                    continue;
                }

                if (sourceRenderer is MeshRenderer && sourceRenderer.TryGetComponent(out MeshFilter meshFilter))
                {
                    mesh = meshFilter.sharedMesh;
                }
                else if (sourceRenderer is SkinnedMeshRenderer skinned)
                {
                    mesh = new Mesh();
                    skinned.BakeMesh(mesh, true);
                    m_BakedMeshes.Add(mesh);
                }

                if (mesh == null)
                {
                    continue;
                }

                parts.Add(new KeyValuePair<Renderer, Mesh>(sourceRenderer, mesh));
                EncapsulateBounds(ref bounds, ref hasBounds, sourceRenderer.bounds);
            }

            if (parts.Count == 0 && batchedRenderers.Count == 0)
            {
                Debug.LogWarning($"[ItemInspection] У предмета '{source.name}' нет видимых мешей - осмотра не будет.", source);
                return null;
            }

            var root = new GameObject("InspectedItem_" + source.name) { layer = layer }.transform;
            root.SetPositionAndRotation(bounds.center, source.transform.rotation);

            m_BatchedParts.Clear();

            foreach (var batchedRenderer in batchedRenderers)
            {
                var mesh = batchedRenderer.GetComponent<MeshFilter>().sharedMesh;
                var materials = batchedRenderer.sharedMaterials;
                // Вершины склеенного меша статического батча (флаг Static у объекта) - в мировых координатах.
                // Батчи, собранные из кода через StaticBatchingUtility.Combine с корнем, в проекте не используются
                // (у них вершины в координатах корня).
                var meshToRoot = root.worldToLocalMatrix;

                for (int i = 0; i < materials.Length; i++)
                {
                    int subMesh = batchedRenderer.subMeshStartIndex + i;

                    if (materials[i] == null || subMesh >= mesh.subMeshCount)
                    {
                        continue;
                    }

                    m_BatchedParts.Add(new BatchedPart
                    {
                        Mesh = mesh,
                        SubMesh = subMesh,
                        Material = materials[i],
                        MeshToRoot = meshToRoot
                    });
                }
            }

            foreach (var part in parts)
            {
                var sourceRenderer = part.Key;
                var partObject = new GameObject(sourceRenderer.name) { layer = layer };
                var partTransform = partObject.transform;
                partTransform.SetParent(root, false);
                partTransform.SetPositionAndRotation(sourceRenderer.transform.position, sourceRenderer.transform.rotation);
                partTransform.localScale = sourceRenderer is SkinnedMeshRenderer ? Vector3.one : sourceRenderer.transform.lossyScale;

                partObject.AddComponent<MeshFilter>().sharedMesh = part.Value;
                var meshRenderer = partObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterials = sourceRenderer.sharedMaterials;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
            }

            radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
            return root;
        }

        private static void EncapsulateBounds(ref Bounds bounds, ref bool hasBounds, Bounds add)
        {
            if (hasBounds)
            {
                bounds.Encapsulate(add);
            }
            else
            {
                bounds = add;
                hasBounds = true;
            }
        }

        /// <summary>
        /// Части предмета из статического батча: каждый кадр рисуются только камерой осмотра, на слое осмотра,
        /// там, где сейчас корень копии.
        /// </summary>
        private void DrawBatchedParts()
        {
            if (m_BatchedParts.Count == 0 || m_Item == null || m_InspectionCamera == null)
            {
                return;
            }

            Matrix4x4 rootToWorld = m_Item.localToWorldMatrix;
            // Границы для отсечения и выборки световых проб - вокруг самого предмета, а не всего склеенного меша.
            var worldBounds = new Bounds(m_Item.position, Vector3.one * (2f * m_ItemRadius * m_Item.localScale.x));

            foreach (var part in m_BatchedParts)
            {
                var renderParams = new RenderParams(part.Material)
                {
                    camera = m_InspectionCamera,
                    layer = m_ItemLayer,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false,
                    lightProbeUsage = LightProbeUsage.BlendProbes,
                    reflectionProbeUsage = ReflectionProbeUsage.BlendProbes,
                    worldBounds = worldBounds
                };

                Graphics.RenderMesh(renderParams, part.Mesh, part.SubMesh, rootToWorld * part.MeshToRoot);
            }
        }

        private void SetupPoses(float radius)
        {
            var cameraTransform = m_InspectionCamera.transform;
            float halfHeight = m_Distance * Mathf.Tan(m_InspectionCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

            m_FromPosition = m_Item.position;
            m_FromRotation = m_Item.rotation;
            m_FromScale = 1f;

            m_ToPosition = cameraTransform.position + cameraTransform.forward * m_Distance +
                           cameraTransform.up * (2f * halfHeight * m_ScreenOffsetY);
            m_ToScale = halfHeight * m_ScreenSize / radius;

            // Предмет показывается так, как стоял относительно игрока, но без наклона камеры: если игрок смотрел
            // на лежащий внизу предмет, перед камерой он не будет "смотреть" на игрока сверху.
            Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);

            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up);
            }

            var yaw = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            m_ToRotation = cameraTransform.rotation * (Quaternion.Inverse(yaw) * m_FromRotation);
        }

        /// <summary>
        /// Точка, куда летит предмет: ячейка инвентаря, в которой он появится. Ряд ячеек выровнен по центру,
        /// новая ячейка встаёт справа, а весь ряд сдвигается на половину шага влево.
        /// </summary>
        private Vector3 GetInventoryWorldPoint()
        {
            Vector2 screenPoint = GetInventoryScreenPoint();
            var cameraTransform = m_InspectionCamera.transform;
            float depth = Vector3.Dot(m_Item.position - cameraTransform.position, cameraTransform.forward);

            return m_InspectionCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Mathf.Max(depth, 0.1f)));
        }

        private Vector2 GetInventoryScreenPoint()
        {
            if (m_InventoryPanel == null)
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.1f);
            }

            m_SlotsBuffer.Clear();

            foreach (Transform child in m_InventoryPanel)
            {
                if (child.gameObject.activeSelf && child is RectTransform slot)
                {
                    m_SlotsBuffer.Add(slot);
                }
            }

            var canvas = m_InventoryPanel.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;

            if (m_SlotsBuffer.Count == 0)
            {
                // Инвентарь пуст: ячейка встанет посередине у верхнего края панели.
                var corners = new Vector3[4];
                m_InventoryPanel.GetWorldCorners(corners);
                return new Vector2((corners[1].x + corners[2].x) * 0.5f, corners[1].y - 40f * scale);
            }

            var layout = m_InventoryPanel.GetComponent<HorizontalLayoutGroup>();
            float step = m_SlotsBuffer.Count >= 2
                ? m_SlotsBuffer[1].position.x - m_SlotsBuffer[0].position.x
                : (m_SlotsBuffer[0].rect.width + (layout != null ? layout.spacing : 0f)) * scale;

            Vector3 last = m_SlotsBuffer[m_SlotsBuffer.Count - 1].position;
            return new Vector2(last.x + step * 0.5f, last.y);
        }

        #endregion

        #region Управление игроком

        private void LockControls()
        {
            m_PlayerMoveController?.SetMouseState(true);
            m_InputController?.SetControlEnabled(false);

            if (m_ControlHandles != null)
            {
                foreach (var handle in m_ControlHandles)
                {
                    handle?.SetControlEnabled(false);
                }
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            m_HiddenObjects.Clear();

            if (m_HideWhileInspecting != null)
            {
                foreach (var hudObject in m_HideWhileInspecting)
                {
                    if (hudObject != null && hudObject.activeSelf)
                    {
                        hudObject.SetActive(false);
                        m_HiddenObjects.Add(hudObject);
                    }
                }
            }

            if (m_PauseGame)
            {
                m_TimeScaleBefore = Time.timeScale;
                Time.timeScale = 0f;
                m_IsPaused = true;
            }
        }

        private void UnlockControls()
        {
            if (m_IsPaused)
            {
                Time.timeScale = m_TimeScaleBefore;
                m_IsPaused = false;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            foreach (var hudObject in m_HiddenObjects)
            {
                if (hudObject != null)
                {
                    hudObject.SetActive(true);
                }
            }

            m_HiddenObjects.Clear();

            if (m_ControlHandles != null)
            {
                foreach (var handle in m_ControlHandles)
                {
                    handle?.SetControlEnabled(true);
                }
            }

            m_InputController?.SetControlEnabled(true);
            m_PlayerMoveController?.SetMouseState(false);
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        #endregion
    }
}
