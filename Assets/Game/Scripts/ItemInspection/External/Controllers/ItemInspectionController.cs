using System;
using System.Collections.Generic;
using AZE.AdvancedFirstPerson;
using DG.Tweening;
using Game.Scripts.InputController;
using Game.Scripts.ItemInspection.View;
using Game.Scripts.Story;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Scripts.ItemInspection.Controllers
{
    /// <summary>
    /// Осмотр предмета (по примеру Resident Evil) - отдельный модуль, <see cref="IItemInspection"/>. Лежит в префабе
    /// игрока (объект ItemInspection). Что за предмет и что с ним будет потом, модуль не знает: при подборе его зовёт
    /// InspectOnFirstPickup (только первый предмет каждого типа), но можно звать и откуда угодно ещё.
    ///
    /// Как работает:
    /// 1. Кто-то вызывает <see cref="TryInspect"/> с объектом на сцене.
    /// 2. С объекта снимается копия (только меши и материалы, без коллайдеров, света и скриптов) на отдельном слое
    ///    (InspectItem), сам объект прячется (рендереры и коллайдеры выключаются). Копия летит к камере.
    /// 3. Копию рисует отдельная камера (Overlay), которую на время осмотра ставят в стек основной камеры, - поэтому
    ///    предмет не размыт и не проваливается в стены. Основная камера перестаёт видеть слой предмета, а её
    ///    картинку размывает Volume с глубиной резкости (вес плавно растёт до 1).
    /// 4. Снизу - название предмета, подсказка и кнопка "Принять". Зажав ЛКМ и двигая мышь, предмет крутят.
    /// 5. По Esc или кнопке "Принять" предмет уменьшается и улетает (к панели инвентаря, см. Fly Target); когда
    ///    долетел - объект на сцене снова виден, вызывается onFinished, всё возвращается как было.
    ///    Осмотр через <see cref="TryInspectAndReturn"/> (документы в руке бота) - предмет не улетает, а возвращается
    ///    туда, откуда его взяли: копия летит обратно к объекту и встаёт точно на него.
    ///
    /// На время осмотра: курсор виден, движение и поворот камеры выключены, клавиши E/F/R/TAB/цифры не действуют,
    /// игра по желанию на паузе (Time.timeScale = 0, сам осмотр анимируется по реальному времени).
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(115)]
    public class ItemInspectionController : MonoBehaviour, ISequence, IUpdateSequence, IItemInspection
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
        [Tooltip("Камера, которая рисует осматриваемый предмет поверх размытой картинки (Render Type: Overlay). " +
                 "Объект камеры выключен, пока осмотра нет; на время осмотра камера встаёт в стек основной камеры.")]
        [SerializeField] private Camera m_InspectionCamera;
        [Tooltip("Размытие всего, что за предметом: Volume с глубиной резкости. Вес плавно меняется 0 -> 1 -> 0.")]
        [SerializeField] private Volume m_BlurVolume;
        [Tooltip("Куда на экране улетает предмет после осмотра. Сейчас - панель инвентаря: предмет летит на место " +
                 "после её последнего видимого элемента (туда, где появится новая ячейка). Пусто - вниз экрана.")]
        [FormerlySerializedAs("m_InventoryPanel")]
        [SerializeField] private RectTransform m_FlyTarget;
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
        [Tooltip("Сколько секунд предмет летит в инвентарь (или обратно на место).")]
        [SerializeField, Min(0.01f)] private float m_FlyOutDuration = 0.45f;
        [Tooltip("Размер предмета в конце полёта в инвентарь - доля от размера при осмотре.")]
        [SerializeField, Range(0.01f, 1f)] private float m_EndScale = 0.12f;

        [Header("Игра")]
        [Tooltip("Ставить игру на паузу на время осмотра (как в Resident Evil): монстры, физика и сюжетные " +
                 "таймеры замирают. Звук не останавливается.")]
        [SerializeField] private bool m_PauseGame = true;

        [Inject] private ItemInspectionView m_View;
        [Inject] private IInputActions m_InputActions;
        [Inject] private IPlayerMoveController m_PlayerMoveController;
        [Inject] private global::Game.Scripts.InputController.InputController m_InputController;
        [Inject] private List<IPlayerControlHandle> m_ControlHandles;

        private readonly List<Mesh> m_BakedMeshes = new();
        // Что выключено у самого объекта на время осмотра - только это и включается обратно.
        private readonly List<Renderer> m_HiddenSourceRenderers = new();
        private readonly List<Collider> m_DisabledSourceColliders = new();
        private readonly List<BatchedPart> m_BatchedParts = new();
        private readonly List<RectTransform> m_SlotsBuffer = new();
        // Какие из m_HideWhileInspecting были включены до осмотра - только их и включаем обратно.
        private readonly List<GameObject> m_HiddenObjects = new();

        private Phase m_Phase = Phase.None;
        private Sequence m_Sequence;
        private Transform m_Item;
        private int m_ItemLayer;
        private float m_ItemRadius;
        private Action m_OnFinished;
        // Куда улетает этот предмет (null - m_FlyTarget).
        private Transform m_ItemFlyTarget;
        // Предмет возвращается на место (TryInspectAndReturn): сам объект и центр копии в его координатах. null - улетает.
        private Transform m_ReturnTarget;
        private Vector3 m_ReturnLocalCenter;

        private Camera m_BaseCamera;
        private int m_BaseCullingMask;
        private bool m_IsPaused;
        private float m_TimeScaleBefore = 1f;

        // Поза предмета перед камерой (куда он летит при осмотре).
        private Vector3 m_ToPosition;
        private Quaternion m_ToRotation;
        private float m_ToScale;
        private float m_BlurWeight;

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

        public bool TryInspect(GameObject source, string displayName, Transform flyTarget, Action onFinished)
        {
            return StartInspection(source, displayName, flyTarget, false, onFinished);
        }

        public bool TryInspectAndReturn(GameObject source, string displayName, Action onFinished)
        {
            return StartInspection(source, displayName, null, true, onFinished);
        }

        private bool StartInspection(GameObject source, string displayName, Transform flyTarget, bool returnToSource,
            Action onFinished)
        {
            if (m_Phase != Phase.None || source == null)
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
                               "осмотра не будет.", this);
                return false;
            }

            var baseCamera = Camera.main;

            if (baseCamera == null || m_InspectionCamera == null || m_View == null)
            {
                Debug.LogWarning("[ItemInspection] Нет основной камеры (тег MainCamera), камеры осмотра или интерфейса " +
                                 "осмотра - осмотра не будет.", this);
                return false;
            }

            var copy = BuildCopy(source, layer, out float radius);

            if (copy == null)
            {
                return false;
            }

            m_Item = copy;
            m_ItemLayer = layer;
            m_ItemRadius = radius;
            m_OnFinished = onFinished;
            m_ItemFlyTarget = flyTarget;
            m_ReturnTarget = returnToSource ? source.transform : null;
            m_ReturnLocalCenter = source.transform.InverseTransformPoint(copy.position);

            HideSource(source);
            SetupCameras(baseCamera, layer);
            SetupPoses(radius);
            LockControls();

            m_View.Show(string.IsNullOrEmpty(displayName) ? source.name : displayName);
            m_IsDragging = false;
            PlayFlyIn();
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

            if (m_Phase == Phase.Inspect)
            {
                UpdateRotation();
            }
        }

        /// <summary>
        /// Esc или кнопка "Принять": предмет улетает (к Fly Target).
        /// </summary>
        private void Accept()
        {
            if (m_Phase != Phase.FlyIn && m_Phase != Phase.Inspect)
            {
                return;
            }

            m_IsDragging = false;
            m_View.SetInteractable(false);
            PlayFlyOut();
        }

        /// <summary>
        /// Предмет летит к камере, фон размывается, подпись появляется. Анимации - по реальному времени: игра на паузе.
        /// </summary>
        private void PlayFlyIn()
        {
            m_Phase = Phase.FlyIn;
            m_Sequence?.Kill();
            m_Sequence = DOTween.Sequence()
                .Join(m_Item.DOMove(m_ToPosition, m_FlyInDuration).SetEase(Ease.InOutSine))
                .Join(m_Item.DORotateQuaternion(m_ToRotation, m_FlyInDuration).SetEase(Ease.InOutSine))
                .Join(m_Item.DOScale(m_ToScale, m_FlyInDuration).SetEase(Ease.InOutSine))
                .Join(DOTween.To(() => m_BlurWeight, SetBlur, 1f, m_FlyInDuration).SetEase(Ease.InOutSine))
                .Insert(m_FlyInDuration * 0.3f, m_View.Fade(1f, m_FlyInDuration * 0.7f))
                .SetUpdate(true)
                .OnComplete(() => m_Phase = Phase.Inspect);
        }

        /// <summary>
        /// Предмет с разгоном уменьшается и улетает к Fly Target, размытие и подпись гаснут.
        /// </summary>
        private void PlayFlyOut()
        {
            m_Phase = Phase.FlyOut;
            m_Sequence?.Kill();

            // Объект, к которому предмет возвращается, могли удалить за время осмотра - тогда предмет просто улетает.
            if (m_ReturnTarget != null)
            {
                PlayReturn();
                return;
            }

            m_Sequence = DOTween.Sequence()
                .Join(m_Item.DOMove(GetFlyTargetWorldPoint(), m_FlyOutDuration).SetEase(Ease.InQuad))
                .Join(m_Item.DOScale(m_Item.localScale.x * m_EndScale, m_FlyOutDuration).SetEase(Ease.InQuad))
                .Join(DOTween.To(() => m_BlurWeight, SetBlur, 0f, m_FlyOutDuration).SetEase(Ease.Linear))
                .Join(m_View.Fade(0f, m_FlyOutDuration * 0.3f))
                .SetUpdate(true)
                .OnComplete(Finish);
        }

        /// <summary>
        /// Предмет возвращается на место (<see cref="TryInspectAndReturn"/>): летит обратно к объекту - туда, где объект
        /// сейчас, его поворотом и в его настоящем размере, - и там подменяется им самим. Размытие и подпись гаснут.
        /// </summary>
        private void PlayReturn()
        {
            m_Sequence = DOTween.Sequence()
                .Join(m_Item.DOMove(m_ReturnTarget.TransformPoint(m_ReturnLocalCenter), m_FlyOutDuration).SetEase(Ease.InOutSine))
                .Join(m_Item.DORotateQuaternion(m_ReturnTarget.rotation, m_FlyOutDuration).SetEase(Ease.InOutSine))
                .Join(m_Item.DOScale(1f, m_FlyOutDuration).SetEase(Ease.InOutSine))
                .Join(DOTween.To(() => m_BlurWeight, SetBlur, 0f, m_FlyOutDuration).SetEase(Ease.Linear))
                .Join(m_View.Fade(0f, m_FlyOutDuration * 0.3f))
                .SetUpdate(true)
                .OnComplete(Finish);
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
        /// Убирает копию, камеру осмотра, размытие и интерфейс, снова показывает сам объект, снимает паузу;
        /// с restoreControls - возвращает игроку управление (при уничтожении самого игрока контроллеры движения
        /// и ввода уже могут быть удалены).
        /// </summary>
        private void Cleanup(bool restoreControls)
        {
            m_Phase = Phase.None;
            m_IsDragging = false;
            m_ItemFlyTarget = null;
            m_ReturnTarget = null;
            m_Sequence?.Kill();
            m_Sequence = null;

            if (m_Item != null)
            {
                Destroy(m_Item.gameObject);
            }

            ShowSource();

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
            m_BlurWeight = weight;

            if (m_BlurVolume == null)
            {
                return;
            }

            m_BlurVolume.weight = weight;
            m_BlurVolume.enabled = weight > 0f;
        }

        #endregion

        #region Сам объект на сцене

        /// <summary>
        /// На время осмотра объект на сцене не виден (его заменяет копия перед камерой) и с ним нельзя
        /// взаимодействовать: выключаются его включённые рендереры и коллайдеры.
        /// </summary>
        private void HideSource(GameObject source)
        {
            m_HiddenSourceRenderers.Clear();
            m_DisabledSourceColliders.Clear();

            foreach (var sourceRenderer in source.GetComponentsInChildren<Renderer>(false))
            {
                if (sourceRenderer.enabled)
                {
                    sourceRenderer.enabled = false;
                    m_HiddenSourceRenderers.Add(sourceRenderer);
                }
            }

            foreach (var sourceCollider in source.GetComponentsInChildren<Collider>(false))
            {
                if (sourceCollider.enabled)
                {
                    sourceCollider.enabled = false;
                    m_DisabledSourceColliders.Add(sourceCollider);
                }
            }
        }

        /// <summary>
        /// Включает обратно ровно то, что выключил <see cref="HideSource"/>.
        /// </summary>
        private void ShowSource()
        {
            foreach (var sourceRenderer in m_HiddenSourceRenderers)
            {
                if (sourceRenderer != null)
                {
                    sourceRenderer.enabled = true;
                }
            }

            foreach (var sourceCollider in m_DisabledSourceColliders)
            {
                if (sourceCollider != null)
                {
                    sourceCollider.enabled = true;
                }
            }

            m_HiddenSourceRenderers.Clear();
            m_DisabledSourceColliders.Clear();
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

            Quaternion fromRotation = m_Item.rotation;

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
            m_ToRotation = cameraTransform.rotation * (Quaternion.Inverse(yaw) * fromRotation);
        }

        /// <summary>
        /// Точка, куда летит предмет. Свой объект на сцене у предмета (например, кассетный плеер) - его позиция.
        /// Элемент интерфейса (по умолчанию - панель инвентаря) - место после его последнего элемента: ряд ячеек
        /// выровнен по центру, новая ячейка встаёт справа, а весь ряд сдвигается на половину шага влево.
        /// </summary>
        private Vector3 GetFlyTargetWorldPoint()
        {
            if (m_ItemFlyTarget != null && m_ItemFlyTarget is not RectTransform)
            {
                return m_ItemFlyTarget.position;
            }

            var uiTarget = m_ItemFlyTarget as RectTransform;
            Vector2 screenPoint = GetFlyTargetScreenPoint(uiTarget != null ? uiTarget : m_FlyTarget);
            var cameraTransform = m_InspectionCamera.transform;
            float depth = Vector3.Dot(m_Item.position - cameraTransform.position, cameraTransform.forward);

            return m_InspectionCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, Mathf.Max(depth, 0.1f)));
        }

        private Vector2 GetFlyTargetScreenPoint(RectTransform target)
        {
            if (target == null)
            {
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.1f);
            }

            m_SlotsBuffer.Clear();

            foreach (Transform child in target)
            {
                if (child.gameObject.activeSelf && child is RectTransform slot)
                {
                    m_SlotsBuffer.Add(slot);
                }
            }

            var canvas = target.GetComponentInParent<Canvas>();
            float scale = canvas != null ? canvas.scaleFactor : 1f;

            if (m_SlotsBuffer.Count == 0)
            {
                // Панель пуста: новый элемент встанет посередине у её верхнего края.
                var corners = new Vector3[4];
                target.GetWorldCorners(corners);
                return new Vector2((corners[1].x + corners[2].x) * 0.5f, corners[1].y - 40f * scale);
            }

            var layout = target.GetComponent<HorizontalLayoutGroup>();
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
