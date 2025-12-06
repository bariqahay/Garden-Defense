using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject sprayPrefab;
    public Transform spawnPoint;
    public float shootCooldown = 0.3f;

    [Header("Shader Effect Settings")]
    public bool useCustomShader = true;
    public float shootFlashDuration = 0.15f;

    private float shootTimer = 0f;
    private PlayerShaderController shaderController;

    void Start()
    {
        // Setup custom shader controller
        if (useCustomShader)
        {
            shaderController = GetComponent<PlayerShaderController>();
            if (shaderController == null)
            {
                shaderController = gameObject.AddComponent<PlayerShaderController>();
                Debug.Log("[PlayerShooting] Added PlayerShaderController component");
            }
        }
    }

    void Update()
    {
        shootTimer += Time.deltaTime;

        if (Input.GetMouseButton(0) && shootTimer >= shootCooldown)
        {
            Shoot();
            shootTimer = 0f;
        }
    }

    void Shoot()
    {
        // Spawn spray di depan player
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward;
        Quaternion spawnRot = transform.rotation;

        GameObject spray = Instantiate(sprayPrefab, spawnPos, spawnRot);

        Debug.Log($"[PlayerShooting] Spawned spray at {spawnPos}");

        // Trigger custom shader glow effect
        if (useCustomShader && shaderController != null)
        {
            shaderController.TriggerShootGlow(shootFlashDuration);
        }
    }
}