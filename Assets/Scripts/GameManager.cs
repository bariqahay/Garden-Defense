using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public EnemySpawner spawner;
    public PlantHealth[] plants;

    [Header("Game State")]
    public bool isGameOver = false;

    private List<GameObject> activeEnemies = new List<GameObject>();

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
    }

    // Register enemy saat spawn
    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);
    }

    // Unregister enemy saat mati
    public void UnregisterEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
            activeEnemies.Remove(enemy);
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

    public void CheckWinCondition()
    {
        if (isGameOver) return;

        // Cek apakah ada tanaman yang masih hidup
        bool hasAlivePlants = plants.Any(p => p != null && p.currentHealth > 0);

        // Cek apakah semua wave selesai DAN tidak ada enemy lagi
        if (spawner != null)
        {
            bool allWavesComplete = spawner.currentWave >= spawner.waves.Length;
            bool noEnemiesLeft = GetActiveEnemyCount() == 0;

            if (hasAlivePlants && allWavesComplete && noEnemiesLeft)
            {
                isGameOver = true;
                Debug.Log("✨ VICTORY! You protected the plants!");

                // Show win screen (nanti kalau UI sudah ada)
                // if (UIManager.Instance != null)
                //     UIManager.Instance.ShowWinScreen();
            }
        }
    }

    public void CheckLoseCondition()
    {
        if (isGameOver) return;

        // Update plants array
        plants = FindObjectsByType<PlantHealth>(FindObjectsSortMode.None);

        // Cek apakah semua tanaman mati
        if (plants.Length == 0 || plants.All(p => p.currentHealth <= 0))
        {
            isGameOver = true;
            if (spawner != null)
                spawner.isSpawningActive = false;

            Debug.Log("💀 DEFEAT! All plants are dead!");

            // Show lose screen (nanti kalau UI sudah ada)
            // if (UIManager.Instance != null)
            //     UIManager.Instance.ShowLoseScreen();
        }
    }
}