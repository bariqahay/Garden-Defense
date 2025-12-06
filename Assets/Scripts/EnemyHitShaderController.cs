using UnityEngine;
using System.Collections;

/// <summary>
/// Controller untuk custom shader hit flash effect
/// Mengontrol parameter shader secara real-time dari script
/// </summary>
public class EnemyHitShaderController : MonoBehaviour
{
    [Header("Shader References")]
    private Renderer targetRenderer;
    private Material instanceMaterial;

    [Header("Flash Settings")]
    public Color hitFlashColor = Color.red;
    public float flashDuration = 0.2f;
    public float emissionIntensity = 2f;

    private bool isFlashing = false;
    private Coroutine flashCoroutine;

    // Shader property IDs (untuk performa lebih baik)
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    private static readonly int HitFlashColorID = Shader.PropertyToID("_HitFlashColor");
    private static readonly int EmissionIntensityID = Shader.PropertyToID("_EmissionIntensity");

    void Start()
    {
        // Cari renderer di GameObject atau children-nya
        targetRenderer = GetComponentInChildren<Renderer>();

        if (targetRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] No Renderer found!");
            return;
        }

        // Buat material instance (agar tidak mengubah asset original)
        if (targetRenderer.sharedMaterial != null)
        {
            instanceMaterial = new Material(targetRenderer.sharedMaterial);
            targetRenderer.material = instanceMaterial;

            // Set initial values
            instanceMaterial.SetColor(HitFlashColorID, hitFlashColor);
            instanceMaterial.SetFloat(EmissionIntensityID, emissionIntensity);
            instanceMaterial.SetFloat(FlashAmountID, 0f);

            Debug.Log($"[{gameObject.name}] Custom shader material initialized");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Renderer has no material!");
        }
    }

    /// <summary>
    /// Trigger hit flash effect - dipanggil dari script lain (misal saat kena damage)
    /// </summary>
    public void TriggerHitFlash()
    {
        if (instanceMaterial == null)
        {
            Debug.LogWarning($"[{gameObject.name}] Material not initialized!");
            return;
        }

        // Stop flash sebelumnya jika masih berjalan
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(HitFlashCoroutine());
    }

    /// <summary>
    /// Coroutine untuk animasi hit flash
    /// </summary>
    private IEnumerator HitFlashCoroutine()
    {
        isFlashing = true;
        float elapsed = 0f;

        // Flash ON - fade in
        while (elapsed < flashDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (flashDuration * 0.3f);
            float flashAmount = Mathf.Lerp(0f, 1f, t);

            instanceMaterial.SetFloat(FlashAmountID, flashAmount);
            yield return null;
        }

        // Hold at peak
        instanceMaterial.SetFloat(FlashAmountID, 1f);
        yield return new WaitForSeconds(flashDuration * 0.2f);

        // Flash OFF - fade out
        elapsed = 0f;
        while (elapsed < flashDuration * 0.5f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (flashDuration * 0.5f);
            float flashAmount = Mathf.Lerp(1f, 0f, t);

            instanceMaterial.SetFloat(FlashAmountID, flashAmount);
            yield return null;
        }

        // Ensure completely off
        instanceMaterial.SetFloat(FlashAmountID, 0f);
        isFlashing = false;
    }

    /// <summary>
    /// Update shader parameters on the fly
    /// </summary>
    public void UpdateFlashColor(Color newColor)
    {
        hitFlashColor = newColor;
        if (instanceMaterial != null)
        {
            instanceMaterial.SetColor(HitFlashColorID, hitFlashColor);
        }
    }

    public void UpdateEmissionIntensity(float intensity)
    {
        emissionIntensity = intensity;
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(EmissionIntensityID, emissionIntensity);
        }
    }

    void OnDestroy()
    {
        // Cleanup material instance
        if (instanceMaterial != null)
        {
            Destroy(instanceMaterial);
        }
    }

    // PUBLIC PROPERTIES untuk debug/testing
    public bool IsFlashing => isFlashing;
    public Material MaterialInstance => instanceMaterial;
}