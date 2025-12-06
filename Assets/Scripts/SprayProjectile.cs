using UnityEngine;

public class SprayProjectile : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float lifetime = 2f;

    [Header("Scale Settings")]
    public Vector3 startScale = Vector3.one;
    public Vector3 endScale = Vector3.one * 0.2f;
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Damage Settings")]
    public int damage = 1;
    public string enemyTag = "Enemy";

    [Header("Shader Settings")]
    public bool useShaderDissolve = true; // Toggle shader dissolve

    private float currentLifetime = 0f;
    private Vector3 moveDirection;
    private Rigidbody rb;
    private SprayShaderController shaderController;

    void Start()
    {
        transform.localScale = startScale;
        moveDirection = transform.forward;
        rb = GetComponent<Rigidbody>();

        // Setup shader controller
        shaderController = GetComponent<SprayShaderController>();
        if (shaderController == null && useShaderDissolve)
        {
            shaderController = gameObject.AddComponent<SprayShaderController>();
            Debug.Log("[Spray] Added SprayShaderController component");
        }

        Debug.Log($"[Spray] Spawned at {transform.position:F3} dir {moveDirection} lifetime {lifetime}s damage {damage}");

        if (rb == null)
        {
            Debug.LogWarning("[Spray] No Rigidbody on spray prefab. Recommended: add Rigidbody (isKinematic = true)");
        }
    }

    void Update()
    {
        // Movement
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
        float lifeRatio = Mathf.Clamp01(currentLifetime / lifetime);

        // Scale over life
        float scaleMultiplier = scaleCurve.Evaluate(lifeRatio);
        transform.localScale = Vector3.Lerp(endScale, startScale, scaleMultiplier);

        // Update shader dissolve based on lifetime
        if (useShaderDissolve && shaderController != null)
        {
            // Start dissolving in last 30% of lifetime
            if (lifeRatio > 0.7f)
            {
                float dissolveProgress = (lifeRatio - 0.7f) / 0.3f;
                shaderController.SetDissolveAmount(dissolveProgress);
            }
        }

        // Destroy when lifetime ends
        if (currentLifetime >= lifetime)
        {
            Debug.Log("[Spray] Lifetime expired, destroying.");

            // Trigger dissolve animation before destroy (if not already dissolving)
            if (useShaderDissolve && shaderController != null && !shaderController.IsDissolving)
            {
                shaderController.StartDissolve();
                // Let shader controller handle destruction
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Spray] OnTriggerEnter with {other.name} (tag: {other.tag})");

        if (!other.CompareTag(enemyTag))
        {
            Debug.Log($"[Spray] Hit non-enemy: {other.name}");
            return;
        }

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

        // Quick dissolve on hit
        if (useShaderDissolve && shaderController != null)
        {
            shaderController.dissolveSpeed = 5f; // Faster dissolve on impact
            shaderController.StartDissolve();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[Spray] OnCollisionEnter with {collision.collider.name} (tag: {collision.collider.tag})");

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

        if (useShaderDissolve && shaderController != null)
        {
            shaderController.dissolveSpeed = 5f;
            shaderController.StartDissolve();
        }
        else
        {
            Destroy(gameObject);
        }
    }
}