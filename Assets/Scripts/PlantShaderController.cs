using UnityEngine;

/// <summary>
/// Controller untuk custom plant shader - update warna based on health
/// </summary>
public class PlantShaderController : MonoBehaviour
{
    [Header("Shader References")]
    private Renderer[] plantRenderers; // Array untuk multiple renderers
    private Material[] instanceMaterials; // Array untuk multiple materials

    [Header("Color Settings")]
    public Color healthyColor = new Color(0, 1, 0, 1);
    public Color damagedColor = new Color(1, 1, 0, 1);
    public Color criticalColor = new Color(0.6f, 0.3f, 0, 1);
    public Color deadColor = new Color(0.3f, 0.15f, 0, 1);

    [Header("Visual Effects")]
    [Range(0f, 1f)]
    public float healthRatio = 1f;
    [Range(0f, 1f)]
    public float wiltAmount = 0f;
    public float wiltSpeed = 1f;

    // Shader property IDs
    private static readonly int HealthyColorID = Shader.PropertyToID("_HealthyColor");
    private static readonly int DamagedColorID = Shader.PropertyToID("_DamagedColor");
    private static readonly int CriticalColorID = Shader.PropertyToID("_CriticalColor");
    private static readonly int DeadColorID = Shader.PropertyToID("_DeadColor");
    private static readonly int HealthRatioID = Shader.PropertyToID("_HealthRatio");
    private static readonly int WiltAmountID = Shader.PropertyToID("_WiltAmount");

    void Start()
    {
        // Cari SEMUA renderer di children (Cylinder, Sphere, Cube, dll)
        plantRenderers = GetComponentsInChildren<Renderer>();

        if (plantRenderers == null || plantRenderers.Length == 0)
        {
            Debug.LogError($"[PlantShader] No Renderers found on {gameObject.name} or its children!");
            return;
        }

        Debug.Log($"[PlantShader] Found {plantRenderers.Length} renderers on {gameObject.name}");

        // Buat material instance untuk SETIAP renderer
        instanceMaterials = new Material[plantRenderers.Length];

        for (int i = 0; i < plantRenderers.Length; i++)
        {
            if (plantRenderers[i].sharedMaterial != null)
            {
                instanceMaterials[i] = new Material(plantRenderers[i].sharedMaterial);
                plantRenderers[i].material = instanceMaterials[i];

                // Set initial values untuk setiap material
                instanceMaterials[i].SetColor(HealthyColorID, healthyColor);
                instanceMaterials[i].SetColor(DamagedColorID, damagedColor);
                instanceMaterials[i].SetColor(CriticalColorID, criticalColor);
                instanceMaterials[i].SetColor(DeadColorID, deadColor);
                instanceMaterials[i].SetFloat(HealthRatioID, healthRatio);
                instanceMaterials[i].SetFloat(WiltAmountID, wiltAmount);

                Debug.Log($"[PlantShader] Material {i} initialized for {plantRenderers[i].name}");
            }
        }
    }

    void Update()
    {
        if (instanceMaterials == null || instanceMaterials.Length == 0) return;

        // Auto-update wilt based on health
        float targetWilt = 1f - healthRatio;
        wiltAmount = Mathf.Lerp(wiltAmount, targetWilt, wiltSpeed * Time.deltaTime);

        // Update SEMUA material
        foreach (Material mat in instanceMaterials)
        {
            if (mat != null)
            {
                mat.SetFloat(WiltAmountID, wiltAmount);
            }
        }
    }

    /// <summary>
    /// Update health ratio (0-1) - called from PlantHealth script
    /// </summary>
    public void SetHealthRatio(float ratio)
    {
        healthRatio = Mathf.Clamp01(ratio);

        if (instanceMaterials != null)
        {
            // Update SEMUA material
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                {
                    mat.SetFloat(HealthRatioID, healthRatio);
                }
            }
        }

        Debug.Log($"[PlantShader] {gameObject.name} health ratio set to {healthRatio:F2}");
    }

    /// <summary>
    /// Update shader colors
    /// </summary>
    private void UpdateShaderColors()
    {
        if (instanceMaterials == null) return;

        foreach (Material mat in instanceMaterials)
        {
            if (mat != null)
            {
                mat.SetColor(HealthyColorID, healthyColor);
                mat.SetColor(DamagedColorID, damagedColor);
                mat.SetColor(CriticalColorID, criticalColor);
                mat.SetColor(DeadColorID, deadColor);
            }
        }
    }

    /// <summary>
    /// Update individual color
    /// </summary>
    public void SetHealthyColor(Color color)
    {
        healthyColor = color;
        if (instanceMaterials != null)
        {
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                    mat.SetColor(HealthyColorID, color);
            }
        }
    }

    public void SetDamagedColor(Color color)
    {
        damagedColor = color;
        if (instanceMaterials != null)
        {
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                    mat.SetColor(DamagedColorID, color);
            }
        }
    }

    public void SetCriticalColor(Color color)
    {
        criticalColor = color;
        if (instanceMaterials != null)
        {
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                    mat.SetColor(CriticalColorID, color);
            }
        }
    }

    public void SetDeadColor(Color color)
    {
        deadColor = color;
        if (instanceMaterials != null)
        {
            foreach (Material mat in instanceMaterials)
            {
                if (mat != null)
                    mat.SetColor(DeadColorID, color);
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
    public float CurrentHealthRatio => healthRatio;
    public float CurrentWiltAmount => wiltAmount;
}