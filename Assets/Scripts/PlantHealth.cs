using UnityEngine;

public class PlantHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth;

    [Header("Visual Transform Settings")]
    public float minScale = 0.5f;
    public float scaleSpeed = 2f;

    [Header("Shader Settings")]
    public bool useCustomShader = true;

    [Header("Audio Settings")]
    public AudioClip damageSound; // Sound saat kena damage
    public AudioClip deathSound; // Sound saat mati
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private PlantShaderController shaderController;

    void Start()
    {
        currentHealth = maxHealth;
        originalScale = transform.localScale;
        targetScale = originalScale;

        // Setup custom shader controller
        if (useCustomShader)
        {
            shaderController = GetComponent<PlantShaderController>();
            if (shaderController == null)
            {
                shaderController = gameObject.AddComponent<PlantShaderController>();
                Debug.Log($"[PlantHealth] Added PlantShaderController to {gameObject.name}");
            }

            // Set initial health ratio
            shaderController.SetHealthRatio(1f);
        }

        // Initial notification to GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyPlantsCountChanged();
    }

    void Update()
    {
        // Smooth scale change
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
    }

    public void TakeDamage(int amount)
    {
        // Subtract and clamp to 0
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} took {amount} damage. Health: {currentHealth}/{maxHealth}");

        // Play damage sound via AudioManager (only if not dead)
        if (currentHealth > 0 && AudioManager.Instance != null && damageSound != null)
        {
            AudioManager.Instance.PlaySFX(damageSound, sfxVolume);
        }

        // Calculate health ratio
        float healthRatio = (float)currentHealth / (float)maxHealth;
        healthRatio = Mathf.Clamp01(healthRatio);

        // Update visual scale
        float scaleMultiplier = Mathf.Lerp(minScale, 1f, healthRatio);
        targetScale = originalScale * scaleMultiplier;

        // Update shader health ratio
        if (useCustomShader && shaderController != null)
        {
            shaderController.SetHealthRatio(healthRatio);
        }

        // Notify GameManager about plant count change
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyPlantsCountChanged();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died!");

        // Play death sound via AudioManager
        if (AudioManager.Instance != null && deathSound != null)
        {
            AudioManager.Instance.PlaySFX(deathSound, sfxVolume);
        }

        StartCoroutine(ShrinkAndDestroy());
    }

    System.Collections.IEnumerator ShrinkAndDestroy()
    {
        // Notify IMMEDIATELY that plant is dead (before visual shrink)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyPlantsCountChanged();
            GameManager.Instance.CheckLoseCondition();
        }

        // Set health ratio to 0 for shader
        if (useCustomShader && shaderController != null)
        {
            shaderController.SetHealthRatio(0f);
        }

        Vector3 shrinkTarget = Vector3.zero;

        while (Vector3.Distance(transform.localScale, shrinkTarget) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, shrinkTarget, scaleSpeed * 2f * Time.deltaTime);
            yield return null;
        }

        // Destroy after animation
        Destroy(gameObject);
    }
}