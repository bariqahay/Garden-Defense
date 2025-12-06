using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyFly : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float rotationSpeed = 180f;

    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackInterval = 1f;
    public int damage = 1;

    [Header("Health Settings")]
    public int maxHealth = 3;
    private int currentHealth;

    [Header("Hit Effect Settings")]
    public Color hitColor = Color.red;
    public float hitFlashDuration = 0.2f;
    public float emissionIntensity = 2f; // Brightness of flash
    private Color originalColor;
    private Color originalEmission;
    private bool hadEmission = false;

    // Custom shader controller
    private EnemyHitShaderController shaderController;

    // -------------------- SOUND SECTION (FIXED) --------------------
    [Header("Sound Settings")]
    public AudioClip buzzClip; // Assign your buzz sound here in Inspector
    [Range(0f, 1f)]
    public float buzzVolume = 0.5f;
    public bool loopBuzz = true;

    private AudioSource buzzSource;
    private bool isMoving = false;
    // ---------------------------------------------------------------

    private Transform targetPlant;
    private PlantHealth targetPlantHealth;
    private float attackTimer = 0f;
    private bool isFlashing = false;

    void Awake()
    {
        buzzSource = GetComponent<AudioSource>();

        // Configure AudioSource
        if (buzzSource != null)
        {
            buzzSource.clip = buzzClip;
            buzzSource.loop = loopBuzz;
            buzzSource.volume = buzzVolume;
            buzzSource.playOnAwake = false;
            buzzSource.spatialBlend = 1f; // 3D sound (optional, set to 0 for 2D)
        }
    }

    void Start()
    {
        currentHealth = maxHealth;

        // Setup custom shader controller
        shaderController = GetComponent<EnemyHitShaderController>();
        if (shaderController == null)
        {
            // Tambahkan component jika belum ada
            shaderController = gameObject.AddComponent<EnemyHitShaderController>();
            shaderController.hitFlashColor = hitColor;
            shaderController.flashDuration = hitFlashDuration;
            shaderController.emissionIntensity = emissionIntensity;
        }

        FindNearestPlant();

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterEnemy(gameObject);
    }

    void Update()
    {
        if (!HasValidTarget())
        {
            ResetTarget();
            FindNearestPlant();
            if (!HasValidTarget())
            {
                // No target = not moving
                UpdateBuzzSound(false);
                return;
            }
        }

        // Check if we're actually moving (have a valid target to move to)
        bool shouldMove = HasValidTarget();

        if (shouldMove)
        {
            MoveTowardsPlant();
            RotateTowardsPlant();
            TryAttackPlant();
        }

        // Update sound based on movement state
        UpdateBuzzSound(shouldMove);
    }

    // -------------------- SOUND LOGIC (SIMPLIFIED & FIXED) --------------------
    void UpdateBuzzSound(bool moving)
    {
        if (buzzSource == null || buzzClip == null) return;

        isMoving = moving;

        // Play sound if moving and not already playing
        if (isMoving && !buzzSource.isPlaying)
        {
            buzzSource.Play();
        }
        // Stop sound if not moving and currently playing
        else if (!isMoving && buzzSource.isPlaying)
        {
            buzzSource.Stop();
        }
    }
    // -------------------------------------------------------------------------

    bool HasValidTarget()
    {
        return targetPlant != null && targetPlantHealth != null && targetPlantHealth.currentHealth > 0;
    }

    void ResetTarget()
    {
        targetPlant = null;
        targetPlantHealth = null;
        attackTimer = 0f;
    }

    void FindNearestPlant()
    {
        PlantHealth[] plants = FindObjectsByType<PlantHealth>(FindObjectsSortMode.None);

        if (plants == null || plants.Length == 0)
            return;

        float nearestDistance = Mathf.Infinity;
        Transform best = null;
        PlantHealth bestHealth = null;

        foreach (PlantHealth plant in plants)
        {
            if (plant == null) continue;
            if (plant.currentHealth <= 0) continue;

            float distance = Vector3.Distance(transform.position, plant.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                best = plant.transform;
                bestHealth = plant;
            }
        }

        if (best != null)
        {
            targetPlant = best;
            targetPlantHealth = bestHealth;
            attackTimer = 0f;
        }
        else
        {
            ResetTarget();
        }
    }

    void MoveTowardsPlant()
    {
        if (targetPlant == null) return;

        Vector3 direction = (targetPlant.position - transform.position);
        direction.y = 0;
        if (direction.sqrMagnitude <= 0.001f) return;

        direction.Normalize();
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    void RotateTowardsPlant()
    {
        if (targetPlant == null) return;

        Vector3 lookDirection = (targetPlant.position - transform.position);
        lookDirection.y = 0;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void TryAttackPlant()
    {
        if (targetPlant == null || targetPlantHealth == null) return;

        if (targetPlantHealth.currentHealth <= 0)
        {
            ResetTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, targetPlant.position);
        if (distance <= attackRange)
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval)
            {
                targetPlantHealth.TakeDamage(damage);

                if (targetPlantHealth.currentHealth <= 0)
                    ResetTarget();

                attackTimer = 0f;
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);

        if (!isFlashing)
            StartCoroutine(HitFlash());

        if (currentHealth <= 0)
            Die();
    }

    System.Collections.IEnumerator HitFlash()
    {
        isFlashing = true;

        // Trigger custom shader animation
        if (shaderController != null)
        {
            shaderController.TriggerHitFlash();
            Debug.Log("Custom shader hit flash triggered!");
        }
        else
        {
            Debug.LogWarning("Shader controller not found!");
        }

        yield return new WaitForSeconds(hitFlashDuration);

        isFlashing = false;
    }

    public void Die()
    {
        // Stop buzz sound immediately when dying
        if (buzzSource != null && buzzSource.isPlaying)
        {
            buzzSource.Stop();
        }

        StartCoroutine(ShrinkAndDestroy());
    }

    System.Collections.IEnumerator ShrinkAndDestroy()
    {
        Vector3 targetScale = Vector3.zero;
        float shrinkSpeed = 3f;

        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale =
                Vector3.Lerp(transform.localScale, targetScale, shrinkSpeed * Time.deltaTime);
            yield return null;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy(gameObject);

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Ensure audio stops on destroy
        if (buzzSource != null && buzzSource.isPlaying)
        {
            buzzSource.Stop();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}