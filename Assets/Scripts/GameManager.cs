using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public EnemySpawner spawner;
    public PlantHealth[] plants;

    [Header("Game State")]
    public bool isGameOver = false;

    private List<GameObject> activeEnemies = new List<GameObject>();

    // ===== EVENTS FOR UI =====
    public event Action<int, int> OnWaveChanged;        // (currentWave, totalWaves)
    public event Action<int> OnEnemyCountChanged;       // (enemyCount)
    public event Action<int> OnPlantsCountChanged;      // (plantCount)
    public event Action OnGameWon;
    public event Action OnGameLost;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // Find all plants in scene
        plants = FindObjectsByType<PlantHealth>(FindObjectsSortMode.None);

        // Play gameplay music
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameplayMusic();
        }

        // Initial UI update
        UpdateUI();
    }

    // Register enemy saat spawn
    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
            OnEnemyCountChanged?.Invoke(GetActiveEnemyCount());
            Debug.Log($"Enemy registered. Total: {GetActiveEnemyCount()}");
        }
    }

    // Unregister enemy saat mati
    public void UnregisterEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            OnEnemyCountChanged?.Invoke(GetActiveEnemyCount());
            Debug.Log($"Enemy unregistered. Remaining: {GetActiveEnemyCount()}");

            // Check win condition after enemy dies
            CheckWinCondition();
        }
    }

    // Get jumlah enemy yang masih hidup
    public int GetActiveEnemyCount()
    {
        activeEnemies.RemoveAll(e => e == null);
        return activeEnemies.Count;
    }

    // Get jumlah tanaman yang masih hidup
    public int GetAlivePlantsCount()
    {
        plants = FindObjectsByType<PlantHealth>(FindObjectsSortMode.None);
        return plants.Count(p => p != null && p.currentHealth > 0);
    }

    // Call this when wave changes
    public void NotifyWaveChanged(int currentWave, int totalWaves)
    {
        OnWaveChanged?.Invoke(currentWave, totalWaves);
        Debug.Log($"Wave changed: {currentWave + 1}/{totalWaves}");
    }

    // Call this when plant count changes
    public void NotifyPlantsCountChanged()
    {
        int aliveCount = GetAlivePlantsCount();
        OnPlantsCountChanged?.Invoke(aliveCount);
        Debug.Log($"Plants alive: {aliveCount}");
    }

    // Update all UI elements
    public void UpdateUI()
    {
        if (spawner != null)
            OnWaveChanged?.Invoke(spawner.currentWave, spawner.waves.Length);

        OnPlantsCountChanged?.Invoke(GetAlivePlantsCount());
        OnEnemyCountChanged?.Invoke(GetActiveEnemyCount());
    }

    public void CheckWinCondition()
    {
        if (isGameOver) return;

        // Cek apakah ada tanaman yang masih hidup
        int alivePlants = GetAlivePlantsCount();
        bool hasAlivePlants = alivePlants > 0;

        // HARUS ADA PLANT YANG HIDUP dulu
        if (!hasAlivePlants)
        {
            Debug.Log("❌ Cannot win - all plants are dead!");
            return;
        }

        // Cek apakah semua wave selesai DAN tidak ada enemy lagi
        if (spawner != null)
        {
            bool allWavesComplete = spawner.currentWave >= spawner.waves.Length;
            bool isSpawningFinished = !spawner.isSpawningActive; // IMPORTANT: Cek spawning sudah selesai
            bool noEnemiesLeft = GetActiveEnemyCount() == 0;

            Debug.Log($"Win Check - Waves:{allWavesComplete} SpawningDone:{isSpawningFinished} NoEnemies:{noEnemiesLeft} PlantsAlive:{hasAlivePlants} ({alivePlants} plants)");

            // WIN CONDITION: Semua wave selesai + spawning selesai + tidak ada enemy + plant masih hidup
            if (hasAlivePlants && allWavesComplete && isSpawningFinished && noEnemiesLeft)
            {
                isGameOver = true;
                Debug.Log("✨✨✨ VICTORY! You protected the plants! ✨✨✨");

                // Play win music
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayWinMusic();
                }

                OnGameWon?.Invoke();
            }
        }
    }

    public void CheckLoseCondition()
    {
        if (isGameOver) return;

        // Update plants array
        plants = FindObjectsByType<PlantHealth>(FindObjectsSortMode.None);

        // Count plants with HP > 0 (even if they're dying/shrinking)
        int alivePlants = 0;
        foreach (PlantHealth plant in plants)
        {
            if (plant != null && plant.currentHealth > 0)
                alivePlants++;
        }

        Debug.Log($"💀 Lose Check - Total plants in scene: {plants.Length}, Alive: {alivePlants}");

        // Cek apakah semua tanaman mati
        if (alivePlants == 0)
        {
            isGameOver = true;

            // Stop spawning kalau masih aktif
            if (spawner != null)
                spawner.isSpawningActive = false;

            Debug.Log("💀💀💀 DEFEAT! All plants are dead! 💀💀💀");
            OnGameLost?.Invoke();
        }
    }
}