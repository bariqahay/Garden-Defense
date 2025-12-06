using UnityEngine;

/// <summary>
/// Controller untuk custom spray shader - dissolve effect saat lifetime habis
/// </summary>
public class SprayShaderController : MonoBehaviour
{
    [Header("Shader References")]
    private Renderer sprayRenderer;
    private Material instanceMaterial;

    [Header("Dissolve Settings")]
    [Range(0f, 1f)]
    public float dissolveSpeed = 2f;
    public AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Glow Settings")]
    public Color glowColor = Color.green;
    [Range(0f, 10f)]
    public float glowIntensity = 3f;

    private float dissolveAmount = 0f;
    private bool isDissolving = false;
    private float dissolveProgress = 0f;

    // Shader property IDs
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int GlowIntensityID = Shader.PropertyToID("_GlowIntensity");

    void Start()
    {
        // Cari renderer
        sprayRenderer = GetComponent<Renderer>();

        if (sprayRenderer == null)
        {
            sprayRenderer = GetComponentInChildren<Renderer>();
        }

        if (sprayRenderer == null)
        {
            Debug.LogError($"[SprayShader] No Renderer found on {gameObject.name}!");
            return;
        }

        // Buat material instance
        if (sprayRenderer.sharedMaterial != null)
        {
            instanceMaterial = new Material(sprayRenderer.sharedMaterial);
            sprayRenderer.material = instanceMaterial;

            // Set initial values
            instanceMaterial.SetColor(ColorID, glowColor);
            instanceMaterial.SetFloat(GlowIntensityID, glowIntensity);
            instanceMaterial.SetFloat(DissolveAmountID, 0f);

            Debug.Log($"[SprayShader] Material initialized for {gameObject.name}");
        }
        else
        {
            Debug.LogError($"[SprayShader] No material on renderer!");
        }
    }

    void Update()
    {
        if (instanceMaterial == null) return;

        // Auto dissolve saat dissolving
        if (isDissolving)
        {
            dissolveProgress += Time.deltaTime * dissolveSpeed;
            dissolveProgress = Mathf.Clamp01(dissolveProgress);

            dissolveAmount = dissolveCurve.Evaluate(dissolveProgress);
            instanceMaterial.SetFloat(DissolveAmountID, dissolveAmount);

            // Jika sudah selesai dissolve
            if (dissolveProgress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Trigger dissolve effect - dipanggil dari SprayProjectile saat lifetime habis
    /// </summary>
    public void StartDissolve()
    {
        if (!isDissolving)
        {
            isDissolving = true;
            dissolveProgress = 0f;
            Debug.Log($"[SprayShader] Dissolve started for {gameObject.name}");
        }
    }

    /// <summary>
    /// Set dissolve amount manual (0-1)
    /// </summary>
    public void SetDissolveAmount(float amount)
    {
        dissolveAmount = Mathf.Clamp01(amount);
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(DissolveAmountID, dissolveAmount);
        }
    }

    /// <summary>
    /// Update glow color on the fly
    /// </summary>
    public void SetGlowColor(Color color)
    {
        glowColor = color;
        if (instanceMaterial != null)
        {
            instanceMaterial.SetColor(ColorID, glowColor);
        }
    }

    /// <summary>
    /// Update glow intensity on the fly
    /// </summary>
    public void SetGlowIntensity(float intensity)
    {
        glowIntensity = intensity;
        if (instanceMaterial != null)
        {
            instanceMaterial.SetFloat(GlowIntensityID, glowIntensity);
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

    // PUBLIC PROPERTIES
    public bool IsDissolving => isDissolving;
    public float DissolveProgress => dissolveProgress;
}