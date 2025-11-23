using UnityEngine;

public class PlantHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 5;
    public int currentHealth;

    [Header("Visual Transform Settings")]
    public float minScale = 0.5f;
    public float scaleSpeed = 2f;

    [Header("Color Settings")]
    public Color healthyColor = Color.green;
    public Color damagedColor = Color.yellow;
    public Color criticalColor = new Color(0.6f, 0.3f, 0f);
    public float colorTransitionSpeed = 3f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color currentTargetColor;
    private Renderer plantRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        originalScale = transform.localScale;
        targetScale = originalScale;
        plantRenderer = GetComponent<Renderer>();

        if (plantRenderer != null)
        {
            currentTargetColor = healthyColor;
            plantRenderer.material.color = healthyColor;
        }

        // Initial notification to GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyPlantsCountChanged();
    }

    void Update()
    {
        // Smooth scale change
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);

        // Smooth color transition
        if (plantRenderer != null && plantRenderer.material != null)
        {
            Color currentColor = plantRenderer.material.color;
            plantRenderer.material.color = Color.Lerp(currentColor, currentTargetColor, colorTransitionSpeed * Time.deltaTime);
        }
    }

    public void TakeDamage(int amount)
    {
        // Subtract and clamp to 0
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} took {amount} damage. Health: {currentHealth}/{maxHealth}");

        // Update visual scale
        float healthRatio = (float)currentHealth / (float)maxHealth;
        healthRatio = Mathf.Clamp01(healthRatio);
        float scaleMultiplier = Mathf.Lerp(minScale, 1f, healthRatio);
        targetScale = originalScale * scaleMultiplier;

        // Update colour
        UpdateHealthColor(healthRatio);

        // Notify GameManager about plant count change
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyPlantsCountChanged();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void UpdateHealthColor(float healthRatio)
    {
        if (plantRenderer == null) return;

        if (healthRatio > 0.66f)
        {
            currentTargetColor = Color.Lerp(damagedColor, healthyColor, (healthRatio - 0.66f) / 0.34f);
        }
        else if (healthRatio > 0.33f)
        {
            currentTargetColor = Color.Lerp(criticalColor, damagedColor, (healthRatio - 0.33f) / 0.33f);
        }
        else
        {
            currentTargetColor = Color.Lerp(new Color(0.4f, 0.2f, 0f), criticalColor, healthRatio / 0.33f);
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} has died!");
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

        Vector3 shrinkTarget = Vector3.zero;
        Color fadeColor = new Color(0.3f, 0.15f, 0f, 0f);

        while (Vector3.Distance(transform.localScale, shrinkTarget) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, shrinkTarget, scaleSpeed * 2f * Time.deltaTime);

            if (plantRenderer != null && plantRenderer.material != null)
            {
                plantRenderer.material.color = Color.Lerp(plantRenderer.material.color, fadeColor, colorTransitionSpeed * Time.deltaTime);
            }

            yield return null;
        }

        // Destroy after animation
        Destroy(gameObject);
    }
}
