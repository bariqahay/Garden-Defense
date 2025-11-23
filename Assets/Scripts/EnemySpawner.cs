using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public int enemyCount;
        public float spawnInterval;
    }

    [Header("Wave Settings")]
    public Wave[] waves;

    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public float spawnHeight = 0.5f;

    [Header("Procedural Movement Settings")]
    public float initialSpawnDistance = 15f;
    public float moveInSpeed = 2f;

    [HideInInspector] public int currentWave = 0;
    private int enemiesSpawned = 0;
    private float timer = 0f;
    public bool isSpawningActive = true;

    void Start()
    {
        // Initial wave notification
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyWaveChanged(currentWave, waves.Length);
    }

    void Update()
    {
        if (!isSpawningActive) return;

        if (currentWave < waves.Length)
        {
            timer += Time.deltaTime;

            // Spawn musuh tiap interval
            if (timer >= waves[currentWave].spawnInterval &&
                enemiesSpawned < waves[currentWave].enemyCount)
            {
                SpawnEnemy();
                timer = 0f;
            }

            // Cek kalau wave ini selesai
            if (enemiesSpawned >= waves[currentWave].enemyCount &&
                GameManager.Instance.GetActiveEnemyCount() == 0)
            {
                // Increment SEBELUM notify
                currentWave++;

                // Kalau wave terakhir sudah selesai
                if (currentWave >= waves.Length)
                {
                    Debug.Log("All waves completed!");

                    // Notify wave change dengan current (sudah >= length)
                    if (GameManager.Instance != null)
                        GameManager.Instance.NotifyWaveChanged(currentWave, waves.Length);

                    isSpawningActive = false;

                    // Check win SETELAH semua selesai
                    GameManager.Instance.CheckWinCondition();
                }
                else
                {
                    // Masih ada wave lagi
                    enemiesSpawned = 0;
                    Debug.Log($"Wave {currentWave + 1}/{waves.Length} starting...");

                    // Notify wave change
                    if (GameManager.Instance != null)
                        GameManager.Instance.NotifyWaveChanged(currentWave, waves.Length);
                }
            }
        }
    }

    void SpawnEnemy()
    {
        // Pilih spawn point random
        Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Hitung posisi spawn secara procedural
        Vector3 spawnDirection = (randomPoint.position - Vector3.zero).normalized;
        Vector3 spawnPosition = randomPoint.position + (spawnDirection * initialSpawnDistance);
        spawnPosition.y = spawnHeight;

        // Spawn musuh
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // Rotasi musuh menghadap pusat map
        Vector3 lookDirection = (Vector3.zero - spawnPosition).normalized;
        lookDirection.y = 0;
        float angle = Mathf.Atan2(lookDirection.x, lookDirection.z) * Mathf.Rad2Deg;
        enemy.transform.rotation = Quaternion.Euler(0, angle, 0);

        // Efek scale membesar
        enemy.transform.localScale = Vector3.zero;
        SpawnEffect spawnFX = enemy.AddComponent<SpawnEffect>();
        spawnFX.targetScale = Vector3.one;
        spawnFX.growSpeed = 2f;

        enemiesSpawned++;

        // Register ke GameManager
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterEnemy(enemy);
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }
}

// Component untuk efek spawn
public class SpawnEffect : MonoBehaviour
{
    public Vector3 targetScale = Vector3.one;
    public float growSpeed = 2f;
    private bool isGrowing = true;

    void Update()
    {
        if (isGrowing)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                growSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.localScale, targetScale) < 0.01f)
            {
                transform.localScale = targetScale;
                isGrowing = false;
                Destroy(this);
            }
        }
    }
}