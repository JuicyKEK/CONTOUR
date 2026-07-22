using UnityEngine;

[ExecuteInEditMode]
public class ConeFogSetup : MonoBehaviour
{
    [Header("Auto-Detect Cone")]
    [Tooltip("Автоматически определить размеры конуса из меша")]
    public bool autoDetectSize = true;
    
    [Header("Manual Cone Settings")]
    public float coneHeight = 2f;
    public float coneBaseRadius = 1f;
    public bool apexAtTop = true;
    
    [Header("Effect Settings")]
    [Range(0, 0.2f)] public float jitterIntensity = 0.05f;
    [Range(0, 5f)] public float fogRotationSpeed = 1f;
    [Range(0, 10f)] public float spiralTightness = 3f;
    [Range(1, 8)] public int spiralArms = 3;
    
    [Header("Apex Glow")]
    public bool enableApexGlow = true;
    public Color apexGlowColor = new Color(0.5f, 0.2f, 0.8f, 1f);
    [Range(0, 3f)] public float apexGlowIntensity = 1.5f;
    
    [Header("Colors")]
    public Color silhouetteColor = Color.black;
    public Color fogColor = new Color(0.1f, 0.1f, 0.15f, 1f);
    public Color fogEdgeColor = new Color(0.2f, 0.1f, 0.3f, 1f);
    
    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private MeshFilter meshFilter;
    
    void OnEnable()
    {
        targetRenderer = GetComponent<Renderer>();
        meshFilter = GetComponent<MeshFilter>();
        propertyBlock = new MaterialPropertyBlock();
        
        if (autoDetectSize)
        {
            DetectConeSize();
        }
        
        ApplySettings();
    }
    
    void OnValidate()
    {
        if (targetRenderer == null) return;
        
        if (autoDetectSize && meshFilter != null)
        {
            DetectConeSize();
        }
        
        ApplySettings();
    }
    
    void DetectConeSize()
    {
        if (meshFilter == null || meshFilter.sharedMesh == null) return;
        
        Mesh mesh = meshFilter.sharedMesh;
        Bounds bounds = mesh.bounds;
        
        // Определяем высоту
        coneHeight = bounds.size.y;
        
        // Определяем радиус основания
        // Для стандартного конуса это максимальный радиус в XZ плоскости
        coneBaseRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        
        // Определяем где вершина - смотрим на центр bounds
        // Если центр смещён вверх от геометрического центра, вершина внизу
        Vector3[] vertices = mesh.vertices;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float topRadius = 0f;
        float bottomRadius = 0f;
        
        foreach (var v in vertices)
        {
            if (v.y < minY) minY = v.y;
            if (v.y > maxY) maxY = v.y;
        }
        
        // Проверяем радиус на верхней и нижней границе
        foreach (var v in vertices)
        {
            float r = Mathf.Sqrt(v.x * v.x + v.z * v.z);
            if (Mathf.Abs(v.y - maxY) < 0.01f) topRadius = Mathf.Max(topRadius, r);
            if (Mathf.Abs(v.y - minY) < 0.01f) bottomRadius = Mathf.Max(bottomRadius, r);
        }
        
        // Вершина там, где радиус меньше
        apexAtTop = topRadius < bottomRadius;
        coneBaseRadius = Mathf.Max(topRadius, bottomRadius);
        
        Debug.Log($"Cone detected: Height={coneHeight}, BaseRadius={coneBaseRadius}, ApexAtTop={apexAtTop}");
    }
    
    void ApplySettings()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
            if (targetRenderer == null) return;
        }
        
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        
        targetRenderer.GetPropertyBlock(propertyBlock);
        
        // Cone settings
        propertyBlock.SetFloat("_ConeHeight", coneHeight);
        propertyBlock.SetFloat("_ConeBaseRadius", coneBaseRadius);
        propertyBlock.SetFloat("_ApexAtTop", apexAtTop ? 1f : 0f);
        
        // Effect settings
        propertyBlock.SetFloat("_JitterIntensity", jitterIntensity);
        propertyBlock.SetFloat("_FogRotationSpeed", fogRotationSpeed);
        propertyBlock.SetFloat("_SpiralTightness", spiralTightness);
        propertyBlock.SetFloat("_SpiralArms", spiralArms);
        
        // Apex glow
        propertyBlock.SetFloat("_EnableApexGlow", enableApexGlow ? 1f : 0f);
        propertyBlock.SetColor("_ApexGlowColor", apexGlowColor);
        propertyBlock.SetFloat("_ApexGlowIntensity", apexGlowIntensity);
        
        // Colors
        propertyBlock.SetColor("_SilhouetteColor", silhouetteColor);
        propertyBlock.SetColor("_FogColor", fogColor);
        propertyBlock.SetColor("_FogEdgeColor", fogEdgeColor);
        
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
    
    void OnDrawGizmosSelected()
    {
        // Визуализация конуса
        Vector3 baseCenter = transform.position;
        Vector3 apexPos;
        
        if (apexAtTop)
        {
            baseCenter += Vector3.down * coneHeight * 0.5f * transform.lossyScale.y;
            apexPos = baseCenter + Vector3.up * coneHeight * transform.lossyScale.y;
        }
        else
        {
            baseCenter += Vector3.up * coneHeight * 0.5f * transform.lossyScale.y;
            apexPos = baseCenter + Vector3.down * coneHeight * transform.lossyScale.y;
        }
        
        // Рисуем основание
        Gizmos.color = Color.cyan;
        float scaledRadius = coneBaseRadius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        
        int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)i / segments * Mathf.PI * 2;
            float angle2 = (float)(i + 1) / segments * Mathf.PI * 2;
            
            Vector3 p1 = baseCenter + new Vector3(Mathf.Cos(angle1), 0, Mathf.Sin(angle1)) * scaledRadius;
            Vector3 p2 = baseCenter + new Vector3(Mathf.Cos(angle2), 0, Mathf.Sin(angle2)) * scaledRadius;
            
            Gizmos.DrawLine(p1, p2);
            
            // Линии к вершине
            if (i % 4 == 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(p1, apexPos);
                Gizmos.color = Color.cyan;
            }
        }
        
        // Вершина
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(apexPos, 0.1f);
    }
}