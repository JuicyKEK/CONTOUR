using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.Rendering
{
    /// <summary>
    /// Эффект рендера URP: подсветка подбираемых предметов (компонент <see cref="PickupHighlight"/>) - обводка
    /// цветом предмета, мягкое свечение вокруг и лёгкая подкраска самого предмета.
    /// Как устроено: предметы рисуются в отдельную маску своим цветом (с проверкой глубины сцены - закрытое стеной не
    /// подсвечивается), маска размывается для свечения, и всё прибавляется к картинке камеры после пост-обработки.
    /// Добавлен в PC_Renderer и Mobile_Renderer; общие толщина и яркость - здесь, цвет - у каждого предмета.
    /// </summary>
    public class PickupHighlightFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            [Tooltip("Толщина обводки, пиксели при высоте экрана 1080 (на других разрешениях - пропорционально).")]
            [Range(0f, 8f)] public float OutlineWidth = 2f;
            [Tooltip("Яркость обводки.")]
            [Range(0f, 8f)] public float OutlineIntensity = 1f;
            [Tooltip("Размер мягкого свечения вокруг предмета, пиксели при высоте экрана 1080.")]
            [Range(0f, 64f)] public float GlowSize = 14f;
            [Tooltip("Яркость свечения.")]
            [Range(0f, 8f)] public float GlowIntensity = 1f;
            [Tooltip("Насколько сам предмет подкрашивается цветом подсветки.")]
            [Range(0f, 1f)] public float Fill = 0.12f;
            [Tooltip("Когда рисуется. После пост-обработки (по умолчанию) цвет на экране - ровно тот, что выбран у " +
                     "предмета. До пост-обработки цветокоррекция и тонмаппинг сцены его меняют (синий становится почти белым).")]
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingPostProcessing;
        }

        public Settings settings = new();

        [Tooltip("Hidden/CONTOUR/PickupHighlightMask. Ссылка нужна, чтобы шейдер попал в сборку игры.")]
        [SerializeField] private Shader m_MaskShader;
        [Tooltip("Hidden/CONTOUR/PickupHighlightComposite. Ссылка нужна, чтобы шейдер попал в сборку игры.")]
        [SerializeField] private Shader m_CompositeShader;

        private Material m_MaskMaterial;
        private Material m_CompositeMaterial;
        private PickupHighlightPass m_Pass;

        public override void Create()
        {
            if (m_MaskShader == null)
            {
                m_MaskShader = Shader.Find("Hidden/CONTOUR/PickupHighlightMask");
            }

            if (m_CompositeShader == null)
            {
                m_CompositeShader = Shader.Find("Hidden/CONTOUR/PickupHighlightComposite");
            }

            CoreUtils.Destroy(m_MaskMaterial);
            CoreUtils.Destroy(m_CompositeMaterial);
            m_MaskMaterial = m_MaskShader != null ? CoreUtils.CreateEngineMaterial(m_MaskShader) : null;
            m_CompositeMaterial = m_CompositeShader != null ? CoreUtils.CreateEngineMaterial(m_CompositeShader) : null;
            m_Pass = new PickupHighlightPass(settings, m_MaskMaterial, m_CompositeMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_MaskMaterial == null || m_CompositeMaterial == null || PickupHighlight.Active.Count == 0)
            {
                return;
            }

            CameraType cameraType = renderingData.cameraData.cameraType;

            // Только обычные камеры игры и окно Scene; камеры поверх (осмотр предмета), превью и отражения - без подсветки.
            if ((cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                || renderingData.cameraData.renderType == CameraRenderType.Overlay)
            {
                return;
            }

            // Игрок не управляет (осмотр предмета, катсцена) - подсказок "можно подобрать" нет.
            if (cameraType == CameraType.Game && !PickupHighlight.IsShown)
            {
                return;
            }

            m_Pass.renderPassEvent = settings.Event;
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_MaskMaterial);
            CoreUtils.Destroy(m_CompositeMaterial);
            m_MaskMaterial = null;
            m_CompositeMaterial = null;
        }

        private class PickupHighlightPass : ScriptableRenderPass
        {
            private static readonly int s_ColorId = Shader.PropertyToID("_PickupHighlightColor");
            private static readonly int s_OutlineColorId = Shader.PropertyToID("_PickupOutlineColor");
            private static readonly int s_GlowTexId = Shader.PropertyToID("_PickupGlowTex");
            private static readonly int s_OutlineTexId = Shader.PropertyToID("_PickupOutlineTex");
            private static readonly int s_TexelId = Shader.PropertyToID("_HighlightTexel");
            private static readonly int s_BlurStepId = Shader.PropertyToID("_BlurStep");
            private static readonly int s_OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
            private static readonly int s_OutlineIntensityId = Shader.PropertyToID("_OutlineIntensity");
            private static readonly int s_GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
            private static readonly int s_FillId = Shader.PropertyToID("_Fill");

            private const int BlurHorizontalPass = 0;
            private const int BlurVerticalPass = 1;
            private const int CompositePass = 2;

            private readonly Settings m_Settings;
            private readonly Material m_MaskMaterial;
            private readonly Material m_CompositeMaterial;
            private readonly List<DrawItem> m_Items = new();

            private struct DrawItem
            {
                public Renderer Renderer;
                public int SubMesh;
                public Color Color;
                public Color OutlineColor;
            }

            private class MaskPassData
            {
                public readonly List<DrawItem> Items = new();
                public Material Material;
            }

            private class CompositePassData
            {
                public TextureHandle Mask;
                public TextureHandle OutlineMask;
                public TextureHandle Glow;
                public Material Material;
            }

            public PickupHighlightPass(Settings settings, Material maskMaterial, Material compositeMaterial)
            {
                m_Settings = settings;
                m_MaskMaterial = maskMaterial;
                m_CompositeMaterial = compositeMaterial;
                renderPassEvent = settings.Event;
                profilingSampler = new ProfilingSampler("Pickup Highlight");
                // Нужна отдельная картинка камеры с глубиной, а не сразу экран.
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                if (resourceData.isActiveTargetBackBuffer || !resourceData.activeDepthTexture.IsValid())
                {
                    return;
                }

                m_Items.Clear();
                Collect(cameraData.camera, m_Items);

                if (m_Items.Count == 0)
                {
                    return;
                }

                RenderTextureDescriptor cameraDesc = cameraData.cameraTargetDescriptor;

                // Маска: того же размера, что картинка камеры, - чтобы проверять глубину сцены.
                RenderTextureDescriptor maskDesc = cameraDesc;
                maskDesc.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
                maskDesc.depthStencilFormat = GraphicsFormat.None;
                maskDesc.depthBufferBits = 0;
                maskDesc.useMipMap = false;
                maskDesc.autoGenerateMips = false;
                maskDesc.enableRandomWrite = false;

                // Две маски сразу: цвет свечения (из неё же - размытие и подкраска) и цвет обводки.
                TextureHandle mask = UniversalRenderer.CreateRenderGraphTexture(renderGraph, maskDesc,
                    "_PickupHighlightMask", true, FilterMode.Bilinear);
                TextureHandle outlineMask = UniversalRenderer.CreateRenderGraphTexture(renderGraph, maskDesc,
                    "_PickupHighlightOutlineMask", true, FilterMode.Bilinear);

                using (var builder = renderGraph.AddRasterRenderPass<MaskPassData>("Pickup Highlight Mask",
                           out var data, profilingSampler))
                {
                    data.Material = m_MaskMaterial;
                    data.Items.Clear();
                    data.Items.AddRange(m_Items);

                    builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                    builder.SetRenderAttachment(outlineMask, 1, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Read);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (MaskPassData passData, RasterGraphContext context) =>
                    {
                        foreach (DrawItem item in passData.Items)
                        {
                            if (item.Renderer == null)
                            {
                                continue;
                            }

                            context.cmd.SetGlobalColor(s_ColorId, item.Color);
                            context.cmd.SetGlobalColor(s_OutlineColorId, item.OutlineColor);
                            context.cmd.DrawRenderer(item.Renderer, passData.Material, item.SubMesh, 0);
                        }
                    });
                }

                float pixelScale = cameraDesc.height / 1080f;
                m_CompositeMaterial.SetVector(s_TexelId,
                    new Vector4(1f / Mathf.Max(1, cameraDesc.width), 1f / Mathf.Max(1, cameraDesc.height), 0f, 0f));
                m_CompositeMaterial.SetFloat(s_BlurStepId, m_Settings.GlowSize * pixelScale / 4f);
                m_CompositeMaterial.SetFloat(s_OutlineWidthId, m_Settings.OutlineWidth * pixelScale);
                m_CompositeMaterial.SetFloat(s_OutlineIntensityId, m_Settings.OutlineIntensity);
                m_CompositeMaterial.SetFloat(s_GlowIntensityId, m_Settings.GlowIntensity);
                m_CompositeMaterial.SetFloat(s_FillId, m_Settings.Fill);

                // Свечение: маска, размытая в половинном разрешении.
                RenderTextureDescriptor glowDesc = maskDesc;
                glowDesc.width = Mathf.Max(1, cameraDesc.width / 2);
                glowDesc.height = Mathf.Max(1, cameraDesc.height / 2);
                glowDesc.msaaSamples = 1;
                TextureHandle glowA = UniversalRenderer.CreateRenderGraphTexture(renderGraph, glowDesc,
                    "_PickupHighlightGlowA", false, FilterMode.Bilinear);
                TextureHandle glowB = UniversalRenderer.CreateRenderGraphTexture(renderGraph, glowDesc,
                    "_PickupHighlightGlowB", false, FilterMode.Bilinear);

                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(mask, glowA, m_CompositeMaterial,
                    BlurHorizontalPass), "Pickup Highlight Blur H");
                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(glowA, glowB, m_CompositeMaterial,
                    BlurVerticalPass), "Pickup Highlight Blur V");

                using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("Pickup Highlight Composite",
                           out var data, profilingSampler))
                {
                    data.Mask = mask;
                    data.OutlineMask = outlineMask;
                    data.Glow = glowB;
                    data.Material = m_CompositeMaterial;
                    builder.UseTexture(mask, AccessFlags.Read);
                    builder.UseTexture(outlineMask, AccessFlags.Read);
                    builder.UseTexture(glowB, AccessFlags.Read);
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                    builder.SetRenderFunc(static (CompositePassData passData, RasterGraphContext context) =>
                    {
                        passData.Material.SetTexture(s_GlowTexId, (RTHandle)passData.Glow);
                        passData.Material.SetTexture(s_OutlineTexId, (RTHandle)passData.OutlineMask);
                        Blitter.BlitTexture(context.cmd, passData.Mask, new Vector4(1f, 1f, 0f, 0f), passData.Material,
                            CompositePass);
                    });
                }
            }

            /// <summary>
            /// Что подсвечивать этой камерой: включённые рендереры подсвеченных предметов, которые камера рисует и видит.
            /// </summary>
            private static void Collect(Camera camera, List<DrawItem> items)
            {
                foreach (PickupHighlight highlight in PickupHighlight.Active)
                {
                    if (highlight == null || !highlight.isActiveAndEnabled)
                    {
                        continue;
                    }

                    highlight.GetFinalColors(out Color color, out Color outlineColor);

                    if (color.maxColorComponent <= 0f && outlineColor.maxColorComponent <= 0f)
                    {
                        continue;
                    }

                    foreach (Renderer renderer in highlight.Renderers)
                    {
                        if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy
                            || !renderer.isVisible || (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0)
                        {
                            continue;
                        }

                        int subMeshCount = renderer.sharedMaterials.Length;

                        for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                        {
                            items.Add(new DrawItem
                            {
                                Renderer = renderer, SubMesh = subMesh, Color = color, OutlineColor = outlineColor
                            });
                        }
                    }
                }
            }
        }
    }
}
