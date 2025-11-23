using UnityEngine;

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
    public Color normalColor = Color.white;
    public Color hitColor = Color.red;
    public float hitFlashDuration = 0.2f;

    private Transform targetPlant;
    private PlantHealth targetPlantHealth;
    private float attackTimer = 0f;
    private Renderer enemyRenderer;
    private bool isFlashing = false;

    void Start()
    {
        currentHealth = maxHealth;
        enemyRenderer = GetComponent<Renderer>();
        FindNearestPlant();

        // Register self to GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterEnemy(gameObject);
    }

    void Update()
    {
        // If no target or target is dead, find another
        if (!HasValidTarget())
        {
            ResetTarget();
            FindNearestPlant();
            if (!HasValidTarget())
                return;
        }

        MoveTowardsPlant();
        RotateTowardsPlant();
        TryAttackPlant();
    }

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
            Debug.Log($"Enemy [{name}] targeting: {targetPlant.name} (dist {nearestDistance:F2})");
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
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
                Debug.Log($"Enemy attacking! Plant HP before: {targetPlantHealth.currentHealth}");
                targetPlantHealth.TakeDamage(damage);
                Debug.Log($"Plant HP after: {targetPlantHealth.currentHealth}");

                if (targetPlantHealth.currentHealth <= 0)
                {
                    ResetTarget();
                }

                attackTimer = 0f;
            }
        }
    }

    public void TakeDamage(int damageAmount)
    {
        Debug.Log($"[{name}] TakeDamage called: {damageAmount}. HP before: {currentHealth}");

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"[{name}] HP after: {currentHealth}");

        if (!isFlashing)
            StartCoroutine(HitFlash());

        if (currentHealth <= 0)
        {
            Debug.Log($"[{name}] DEAD -> Die()");
            Die();
        }
    }

    System.Collections.IEnumerator HitFlash()
    {
        isFlashing = true;

        if (enemyRenderer != null && enemyRenderer.material != null)
            enemyRenderer.material.color = hitColor;

        yield return new WaitForSeconds(hitFlashDuration);

        if (enemyRenderer != null && enemyRenderer.material != null)
            enemyRenderer.material.color = normalColor;

        isFlashing = false;
    }

    public void Die()
    {
        StartCoroutine(ShrinkAndDestroy());
    }

    System.Collections.IEnumerator ShrinkAndDestroy()
    {
        Vector3 targetScale = Vector3.zero;
        float shrinkSpeed = 3f;

        while (Vector3.Distance(transform.localScale, targetScale) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, shrinkSpeed * Time.deltaTime);
            yield return null;
        }

        // Unregister FIRST, then destroy
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy(gameObject);

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Safety unregister
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterEnemy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}