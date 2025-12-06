using UnityEngine;
using System.Collections;

/// <summary>
/// Controller untuk custom player shader - emissive glow saat shoot
/// </summary>
public class PlayerShaderController : MonoBehaviour
{
    [Header("Shader References")]
    private Renderer[] playerRenderers;
    private Material[] instanceMaterials;
    private Material[] originalMaterials; // Simpan material asli

    [Header("Material Filter")]
    [Tooltip("Kosongkan untuk apply ke semua material. Isi dengan nama material yang mau di-glow")]
    public string[] targetMaterialNames; // e.g. "model_shirt", "model_pants"
    public bool applyToAllMaterials = true; // Apply ke semua atau hanya yang di-list

    [Header("Emission Settings")]
    public Color emissionColor = Color.green;
    [Range(0f, 10f)]
    public float maxEmissionIntensity = 5f;
    public float emissionFadeSpeed = 10f;

    [Header("Pulse Settings")]
    public bool enablePulse = true;
    [Range(0f, 5f)]
    public float pulseSpeed = 2f;

    private float currentEmissionIntensity = 0f;
    private bool isGlowing = false;
    private Coroutine glowCoroutine;

    // Shader property IDs
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionIntensityID = Shader.PropertyToID("_EmissionIntensity");
    private static readonly int PulseSpeedID = Shader.PropertyToID("_PulseSpeed");

    void Start()
    {
        // Cari SEMUA renderer
        playerRenderers = GetComponentsInChildren<Renderer>();

        if (playerRenderers == null || playerRenderers.Length == 0)
        {
            Debug.LogError($"[PlayerShader] No Renderers found on {gameObject.name}!");
            return;
        }

        Debug.Log($"[PlayerShader] Found {playerRenderers.Length} renderers");

        // Hitung total materials dari semua renderers
        int totalMaterials = 0;
        foreach (Renderer r in playerRenderers)
        {
            totalMaterials += r.sharedMaterials.Length;
        }

        instanceMaterials = new Material[totalMaterials];
        originalMaterials = new Material[totalMaterials];

        int matIndex = 0;

        // Loop setiap renderer
        foreach (Renderer renderer in playerRenderers)
        {
            Material[] mats = renderer.sharedMaterials;
            Material[] newMats = new Material[mats.Length];

            // Loop setiap material dalam renderer
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null)
                {
                    // Simpan original
                    originalMaterials[matIndex] = mats[i];

                    // Cek apakah material ini harus di-process
                    bool shouldProcess = applyToAllMaterials || ShouldProcessMaterial(mats[i].name);

                    if (shouldProcess)
                    {
                        // Buat instance
                        instanceMaterials[matIndex] = new Material(mats[i]);
                        newMats[i] = instanceMaterials[matIndex];

                        // Coba set emission properties (jika shader support)
                        if (instanceMaterials[matIndex].HasProperty(EmissionColorID))
                        {
                            instanceMaterials[matIndex].SetColor(EmissionColorID, emissionColor);
                            instanceMaterials[matIndex].SetFloat(EmissionIntensityID, 0f);

                            if (instanceMaterials[matIndex].HasProperty(PulseSpeedID))
                                instanceMaterials[matIndex].SetFloat(PulseSpeedID, pulseSpeed);

                            Debug.Log($"[PlayerShader] ✓ Processed material: {mats[i].name}");
                        }
                        else
                        {
                            Debug.LogWarning($"[PlayerShader] Material '{mats[i].name}' doesn't have emission properties. Use custom shader or enable emission in material.");
                        }
                    }
                    else
                    {
                        // Skip, pakai material asli
                        newMats[i] = mats[i];
                        Debug.Log($"[PlayerShader] ✗ Skipped material: {mats[i].name}");
                    }

                    matIndex++;
                }
            }

            // Apply materials ke renderer
            renderer.materials = newMats;
        }

        Debug.Log($"[PlayerShader] Total materials processed: {matIndex}");
    }

    /// <summary>
    /// Cek apakah material harus di-process berdasarkan filter
    /// </summary>
    bool ShouldProcessMaterial(string materialName)
    {
        if (targetMaterialNames == null || targetMaterialNames.Length == 0)
            return true;

        foreach (string target in targetMaterialNames)
        {
            if (materialName.Contains(target))
                return true;
        }

        return false;
    }

    void Update()
    {
        if (instanceMaterials == null || instanceMaterials.Length == 0) return;

        // Smooth fade emission
        if (!isGlowing && currentEmissionIntensity > 0f)
        {
            currentEmissionIntensity -= emissionFadeSpeed * Time.deltaTime;
            currentEmissionIntensity = Mathf.Max(0f, currentEmissionIntensity);

            UpdateEmissionIntensity(currentEmissionIntensity);
        }
    }

    /// <summary>
    /// Trigger emission glow - dipanggil dari PlayerShooting saat shoot
    /// </summary>
    public void TriggerShootGlow(float duration = 0.2f)
    {
        if (glowCoroutine != null)
        {
            StopCoroutine(glowCoroutine);
        }

        glowCoroutine = StartCoroutine(ShootGlowCoroutine(duration));
    }

    /// <summary>
    /// Coroutine untuk shoot glow effect
    /// </summary>
    private IEnumerator ShootGlowCoroutine(float duration)
    {
        isGlowing = true;

        // Flash ON - instant max intensity
        currentEmissionIntensity = maxEmissionIntensity;
        UpdateEmissionIntensity(currentEmissionIntensity);

        yield return new WaitForSeconds(duration);

        // Start fade out
        isGlowing = false;
    }

    /// <summary>
    /// Update emission intensity untuk semua material
    /// </summary>
    private void UpdateEmissionIntensity(float intensity)
    {
        if (instanceMaterials == null) return;

        foreach (Material mat in instanceMaterials)
        {
            if (mat != null)
            {
                mat.SetFloat(EmissionIntensityID, intensity);
            }
        }
    }

    /// <summary>
    /// Set emission color on the fly
    /// </summary>
    public void SetEmissionColor(Color color)
    {
        emissionColor = color;

        if (instanceMaterials != null)
        {
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                {
                    mat.SetColor(EmissionColorID, color);
                }
            }
        }
    }

    /// <summary>
    /// Set pulse speed on the fly
    /// </summary>
    public void SetPulseSpeed(float speed)
    {
        pulseSpeed = speed;

        if (instanceMaterials != null)
        {
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                {
                    mat.SetFloat(PulseSpeedID, enablePulse ? speed : 0f);
                }
            }
        }
    }

    void OnDestroy()
    {
        // Cleanup SEMUA material instance
        if (instanceMaterials != null)
        {
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }

    // PUBLIC PROPERTIES
    public bool IsGlowing => isGlowing;
    public float CurrentIntensity => currentEmissionIntensity;
}