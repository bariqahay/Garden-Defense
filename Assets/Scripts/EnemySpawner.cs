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
    public float initialSpawnDistance = 15f; // Spawn di luar map
    public float moveInSpeed = 2f; // Gerak masuk ke map

    [HideInInspector] public int currentWave = 0; // Bisa diakses dari luar tapi gak tampil di Inspector
    private int enemiesSpawned = 0;
    private float timer = 0f;

    public bool isSpawningActive = true;

    void Update()
    {
        if (!isSpawningActive) return;

        // Selama wave masih ada
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
                currentWave++;
                enemiesSpawned = 0;

                // Kalau wave terakhir sudah selesai
                if (currentWave >= waves.Length)
                {
                    Debug.Log("All waves completed!");
                    GameManager.Instance.CheckWinCondition();
                    isSpawningActive = false;
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

        // Efek scale membesar (spawn effect)
        enemy.transform.localScale = Vector3.zero;
        SpawnEffect spawnFX = enemy.AddComponent<SpawnEffect>();
        spawnFX.targetScale = Vector3.one;
        spawnFX.growSpeed = 2f;

        enemiesSpawned++;
        GameManager.Instance.RegisterEnemy(enemy);
    }

    // Getter biar bisa dipanggil dari UI/GameManager
    public int GetCurrentWave()
    {
        return currentWave;
    }
}


// Component untuk efek spawn (scale up)
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
