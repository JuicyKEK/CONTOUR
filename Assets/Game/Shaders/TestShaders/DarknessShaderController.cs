using UnityEngine;

public class DarknessShaderController : MonoBehaviour
{
    [Header("References")]
    public Renderer targetRenderer;
    public Material darknessMaterial;
    
    [Header("Transition Settings")]
    public float transitionDuration = 2f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Material instanceMaterial;
    private Material originalMaterial;
    private float currentConsumeAmount = 0f;
    
    void Start()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
            
        originalMaterial = targetRenderer.material;
        
        // Создаём экземпляр материала тьмы
        instanceMaterial = new Material(darknessMaterial);
        instanceMaterial.SetFloat("_ConsumeAmount", 0);
    }
    
    [ContextMenu("Start Darkness")]
    public void StartDarknessEffect()
    {
        StartCoroutine(TransitionToDarkness());
    }
    
    [ContextMenu("Remove Darkness")]
    public void RemoveDarknessEffect()
    {
        StartCoroutine(TransitionFromDarkness());
    }
    
    private System.Collections.IEnumerator TransitionToDarkness()
    {
        // Меняем материал
        targetRenderer.material = instanceMaterial;
        
        float elapsed = 0f;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / transitionDuration);
            
            currentConsumeAmount = t;
            instanceMaterial.SetFloat("_ConsumeAmount", currentConsumeAmount);
            
            yield return null;
        }
        
        instanceMaterial.SetFloat("_ConsumeAmount", 1f);
    }
    
    private System.Collections.IEnumerator TransitionFromDarkness()
    {
        float elapsed = 0f;
        float startAmount = currentConsumeAmount;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - transitionCurve.Evaluate(elapsed / transitionDuration);
            
            currentConsumeAmount = startAmount * t;
            instanceMaterial.SetFloat("_ConsumeAmount", currentConsumeAmount);
            
            yield return null;
        }
        
        // Возвращаем оригинальный материал
        targetRenderer.material = originalMaterial;
    }
    
    void OnDestroy()
    {
        if (instanceMaterial != null)
            Destroy(instanceMaterial);
    }
}