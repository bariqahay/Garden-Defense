using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject sprayPrefab;
    public Transform spawnPoint; // Posisi spawn spray (di depan player)
    public float shootCooldown = 0.3f;

    [Header("Shader Effect")]
    public float shootFlashIntensity = 2f;
    public float shootFlashDuration = 0.1f;

    private float shootTimer = 0f;
    private Renderer playerRenderer;
    private Color originalColor;

    void Start()
    {
        playerRenderer = GetComponent<Renderer>();
        if (playerRenderer != null)
        {
            originalColor = playerRenderer.material.color;
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

        // MANUAL ROTASI: Spray menghadap arah player
        Quaternion spawnRot = transform.rotation;

        GameObject spray = Instantiate(sprayPrefab, spawnPos, spawnRot);

        // Shader flash effect (emissive boost)
        StartCoroutine(ShootFlash());
    }

    System.Collections.IEnumerator ShootFlash()
    {
        // SHADER EFFECT: Player menyala saat shoot
        if (playerRenderer != null && playerRenderer.material != null)
        {
            Color flashColor = originalColor * shootFlashIntensity;
            playerRenderer.material.color = flashColor;
        }

        yield return new WaitForSeconds(shootFlashDuration);

        // Kembali ke warna normal
        if (playerRenderer != null && playerRenderer.material != null)
        {
            playerRenderer.material.color = originalColor;
        }
    }
}
