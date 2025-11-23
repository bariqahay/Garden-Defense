using UnityEngine;

public class SprayProjectile : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float lifetime = 2f; // Hidup 2 detik

    [Header("Scale Settings")]
    public Vector3 startScale = Vector3.one;
    public Vector3 endScale = Vector3.one * 0.2f; // Mengecil ke 20%
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Damage Settings")]
    public int damage = 1;
    public string enemyTag = "Enemy";

    private float currentLifetime = 0f;
    private Vector3 moveDirection;
    private Rigidbody rb;

    void Start()
    {
        // Set initial scale
        transform.localScale = startScale;

        // Forward direction (dari rotasi saat spawn)
        moveDirection = transform.forward;

        // Try to get a Rigidbody if present (recommended)
        rb = GetComponent<Rigidbody>();

        Debug.Log($"[Spray] Spawned at {transform.position:F3} dir {moveDirection} lifetime {lifetime}s damage {damage}");

        // Safety: if no Rigidbody, warn (OnTrigger requires at least one Rigidbody in the collision pair)
        if (rb == null)
        {
            Debug.LogWarning("[Spray] No Rigidbody on spray prefab. Recommended: add Rigidbody (isKinematic = true) and set collider.IsTrigger = true.");
        }
    }

    void Update()
    {
        // Movement: prefer Rigidbody if available (more reliable with physics)
        if (rb != null)
        {
            rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.deltaTime);
        }
        else
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }

        // Update lifetime
        currentLifetime += Time.deltaTime;

        // Scale over life
        float lifeRatio = Mathf.Clamp01(currentLifetime / lifetime);
        float scaleMultiplier = scaleCurve.Evaluate(lifeRatio);
        transform.localScale = Vector3.Lerp(endScale, startScale, scaleMultiplier);

        if (currentLifetime >= lifetime)
        {
            Debug.Log("[Spray] Lifetime expired, destroying.");
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Debug everything for clarity
        Debug.Log($"[Spray] OnTriggerEnter with {other.name} (tag: {other.tag})");

        // Check tag first (fast)
        if (!other.CompareTag(enemyTag))
        {
            // if it's environment or player, just ignore
            Debug.Log($"[Spray] Hit non-enemy: {other.name}");
            return;
        }

        // It's tagged Enemy
        EnemyFly enemy = other.GetComponent<EnemyFly>();
        if (enemy != null)
        {
            Debug.Log($"[Spray] HIT enemy: {other.name} — applying damage {damage}");
            enemy.TakeDamage(damage);
        }
        else
        {
            Debug.LogWarning($"[Spray] Hit object tagged 'Enemy' but no EnemyFly found on {other.name}");
        }

        // Destroy spray after hitting anything relevant
        Destroy(gameObject);
    }

    // Extra safety if someone uses non-trigger colliders
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[Spray] OnCollisionEnter with {collision.collider.name} (tag: {collision.collider.tag})");
        // optional: replicate trigger logic for robustness
        if (collision.collider.CompareTag(enemyTag))
        {
            EnemyFly enemy = collision.collider.GetComponent<EnemyFly>();
            if (enemy != null)
            {
                Debug.Log($"[Spray] COLLISION HIT enemy: {collision.collider.name} — applying damage {damage}");
                enemy.TakeDamage(damage);
            }
            else
            {
                Debug.LogWarning($"[Spray] Collision with tag 'Enemy' but no EnemyFly script on {collision.collider.name}");
            }
        }

        Destroy(gameObject);
    }
}
