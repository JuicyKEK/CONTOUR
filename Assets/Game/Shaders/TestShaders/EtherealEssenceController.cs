using UnityEngine;

public class EtherealEssenceController : MonoBehaviour
{
    [Header("Material")]
    public Material etherealMaterial;
    
    [Header("Color Presets")]
    public EtherealPreset currentPreset = EtherealPreset.Cosmic;
    
    [Header("Dynamic Effects")]
    public bool reactToPlayer = true;
    public Transform playerTransform;
    public float reactionDistance = 5f;
    
    [Header("Audio Reaction")]
    public bool reactToAudio = false;
    public AudioSource audioSource;
    public float audioSensitivity = 1f;
    
    private Renderer targetRenderer;
    private MaterialPropertyBlock propertyBlock;
    private float[] audioSpectrum = new float[256];
    
    public enum EtherealPreset
    {
        Cosmic,
        Fire,
        Ice,
        Nature,
        Void,
        Holy,
        Corrupt,
        Electric,
        Rainbow
    }
    
    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        
        if (etherealMaterial == null && targetRenderer != null)
        {
            etherealMaterial = targetRenderer.material;
        }
        
        ApplyPreset(currentPreset);
    }
    
    void Update()
    {
        if (reactToPlayer && playerTransform != null)
        {
            ReactToPlayer();
        }
        
        if (reactToAudio && audioSource != null)
        {
            ReactToAudio();
        }
    }
    
    void ReactToPlayer()
    {
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        float proximity = 1f - Mathf.Clamp01(distance / reactionDistance);
        
        // Усиливаем эффекты когда игрок близко
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_PulseIntensity", 0.3f + proximity * 0.4f);
        propertyBlock.SetFloat("_StreamIntensity", 1.5f + proximity * 1.5f);
        propertyBlock.SetFloat("_FresnelIntensity", 2f + proximity * 2f);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
    
    void ReactToAudio()
    {
        audioSource.GetSpectrumData(audioSpectrum, 0, FFTWindow.Blackman);
        
        float bass = 0f;
        float mid = 0f;
        float high = 0f;
        
        for (int i = 0; i < 20; i++) bass += audioSpectrum[i];
        for (int i = 20; i < 100; i++) mid += audioSpectrum[i];
        for (int i = 100; i < 256; i++) high += audioSpectrum[i];
        
        bass *= audioSensitivity * 10f;
        mid *= audioSensitivity * 5f;
        high *= audioSensitivity * 2f;
        
        targetRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_VertexDisplacement", 0.03f + bass * 0.1f);
        propertyBlock.SetFloat("_StreamIntensity", 1.5f + mid * 2f);
        propertyBlock.SetFloat("_StarBrightness", 1.5f + high * 3f);
        targetRenderer.SetPropertyBlock(propertyBlock);
    }
    
    public void ApplyPreset(EtherealPreset preset)
    {
        currentPreset = preset;
        
        switch (preset)
        {
            case EtherealPreset.Cosmic:
                SetColors(
                    new Color(0.1f, 0.3f, 1f),
                    new Color(1f, 0.2f, 0.5f),
                    new Color(0.2f, 0.8f, 1f),
                    Color.white
                );
                break;
                
            case EtherealPreset.Fire:
                SetColors(
                    new Color(1f, 0.3f, 0f),
                    new Color(1f, 0.8f, 0f),
                    new Color(1f, 0.1f, 0f),
                    new Color(1f, 0.9f, 0.5f)
                );
                break;
                
            case EtherealPreset.Ice:
                SetColors(
                    new Color(0.3f, 0.7f, 1f),
                    new Color(0.8f, 0.95f, 1f),
                    new Color(0.1f, 0.4f, 0.8f),
                    new Color(0.9f, 0.95f, 1f)
                );
                break;
                
            case EtherealPreset.Nature:
                SetColors(
                    new Color(0.2f, 0.8f, 0.3f),
                    new Color(0.8f, 1f, 0.2f),
                    new Color(0.1f, 0.5f, 0.2f),
                    new Color(0.8f, 1f, 0.6f)
                );
                break;
                
            case EtherealPreset.Void:
                SetColors(
                    new Color(0.2f, 0f, 0.3f),
                    new Color(0.5f, 0f, 0.5f),
                    new Color(0.1f, 0f, 0.15f),
                    new Color(0.4f, 0.2f, 0.6f)
                );
                break;
                
            case EtherealPreset.Holy:
                SetColors(
                    new Color(1f, 0.95f, 0.7f),
                    new Color(1f, 0.85f, 0.5f),
                    new Color(1f, 1f, 0.9f),
                    new Color(1f, 1f, 1f)
                );
                break;
                
            case EtherealPreset.Corrupt:
                SetColors(
                    new Color(0.3f, 0f, 0f),
                    new Color(0.1f, 0f, 0f),
                    new Color(0.5f, 0.1f, 0f),
                    new Color(1f, 0.2f, 0.1f)
                );
                break;
                
            case EtherealPreset.Electric:
                SetColors(
                    new Color(0f, 0.5f, 1f),
                    new Color(0.5f, 0.8f, 1f),
                    new Color(1f, 1f, 0.3f),
                    new Color(0.8f, 0.9f, 1f)
                );
                break;
                
            case EtherealPreset.Rainbow:
                // Rainbow использует иризацию
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_EnableIridescence", 1f);
                propertyBlock.SetFloat("_IridescenceIntensity", 2f);
                targetRenderer.SetPropertyBlock(propertyBlock);
                break;
        }
    }
    
    void SetColors(Color primary, Color secondary, Color tertiary, Color core)
    {
        if (etherealMaterial != null)
        {
            etherealMaterial.SetColor("_PrimaryColor", primary);
            etherealMaterial.SetColor("_SecondaryColor", secondary);
            etherealMaterial.SetColor("_TertiaryColor", tertiary);
            etherealMaterial.SetColor("_CoreGlow", core);
        }
    }
    
    // Публичные методы для анимации
    public void Pulse()
    {
        StartCoroutine(PulseCoroutine());
    }
    
    System.Collections.IEnumerator PulseCoroutine()
    {
        float originalIntensity = etherealMaterial.GetFloat("_PulseIntensity");
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float intensity = Mathf.Sin(t * Mathf.PI) * 0.5f + originalIntensity;
            
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat("_PulseIntensity", intensity);
            propertyBlock.SetFloat("_FresnelIntensity", 2f + intensity * 3f);
            targetRenderer.SetPropertyBlock(propertyBlock);
            
            yield return null;
        }
    }
}