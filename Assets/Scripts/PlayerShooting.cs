using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject sprayPrefab;
    public Transform spawnPoint;
    public float shootCooldown = 0.3f;

    [Header("Audio Settings")]
    public AudioClip shootSound;
    [Range(0f, 1f)]
    public float shootSoundVolume = 1f;

    [Header("Input Settings")]
    [Tooltip("Nama axis untuk trigger (biasanya R2/RT)")]
    public string triggerAxisName = "Fire1"; // Default, bisa diganti
    [Tooltip("Threshold untuk trigger axis (0-1)")]
    public float triggerThreshold = 0.1f;

    [Header("Shader Effect Settings")]
    public bool useCustomShader = true;
    public float shootFlashDuration = 0.15f;

    [Header("Debug")]
    public bool showInputDebug = false;

    private float shootTimer = 0f;
    private PlayerShaderController shaderController;
    private bool wasShootingLastFrame = false;

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

        bool wantsToShoot = GetShootInput();

        if (showInputDebug && wantsToShoot)
        {
            Debug.Log($"🔫 Shoot input detected!");
        }

        // Shoot jika input detected dan cooldown selesai
        if (wantsToShoot && shootTimer >= shootCooldown)
        {
            Shoot();
            shootTimer = 0f;
            wasShootingLastFrame = true;
        }
        else
        {
            wasShootingLastFrame = false;
        }
    }

    /// <summary>
    /// Detect shoot input dari mouse ATAU controller
    /// </summary>
    bool GetShootInput()
    {
        // Method 1: Mouse button (GetMouseButton = hold, GetMouseButtonDown = single press)
        bool mouseInput = Input.GetMouseButton(0); // Hold to auto-fire

        // Method 2: Controller trigger/button
        bool controllerInput = false;

        try
        {
            // Cek axis trigger (R2/RT = analog trigger 0-1)
            float triggerValue = Input.GetAxis(triggerAxisName);
            controllerInput = triggerValue > triggerThreshold;

            if (showInputDebug && triggerValue > 0)
            {
                Debug.Log($"Trigger value: {triggerValue:F2}");
            }
        }
        catch (System.ArgumentException)
        {
            // Axis tidak ada, coba pakai button fallback
            try
            {
                controllerInput = Input.GetButton(triggerAxisName);
            }
            catch
            {
                // Ignore jika button juga ga ada
            }
        }

        // Return true jika salah satu input aktif
        return mouseInput || controllerInput;
    }

    void Shoot()
    {
        // Spawn spray di depan player
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward;
        Quaternion spawnRot = transform.rotation;

        GameObject spray = Instantiate(sprayPrefab, spawnPos, spawnRot);

        // Play shoot sound via AudioManager (if exists)
        if (AudioManager.Instance != null && shootSound != null)
        {
            AudioManager.Instance.PlaySFX(shootSound, shootSoundVolume);
        }

        if (showInputDebug)
        {
            Debug.Log($"[PlayerShooting] 💥 Spawned spray at {spawnPos}");
        }

        // Trigger custom shader glow effect
        if (useCustomShader && shaderController != null)
        {
            shaderController.TriggerShootGlow(shootFlashDuration);
        }
    }
}