using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] powerUpPrefabs;
    public int maxPowerUps = 3;
    public float spawnInterval = 10f;
    public float powerUpLifetime = 15f;

    [Header("Spawn Area")]
    public Transform[] spawnPoints;
    public bool useRandomSpawning = true;
    public Vector2 spawnAreaMin = new Vector2(-8f, -4f);
    public Vector2 spawnAreaMax = new Vector2(8f, 4f);
    public LayerMask obstacleLayer = 1;
    public float spawnCheckRadius = 1f;

    [Header("Spawn Chances")]
    [Range(0f, 1f)] public float spreadShotChance = 0.4f;
    [Range(0f, 1f)] public float shieldChance = 0.3f;
    [Range(0f, 1f)] public float bombChance = 0.3f;

    private List<GameObject> activePowerUps = new List<GameObject>();
    private float nextSpawnTime;

    void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
        ValidateSpawnChances();
    }

    void Update()
    {
        CleanupDestroyedPowerUps();

        if (Time.time >= nextSpawnTime && activePowerUps.Count < maxPowerUps)
        {
            SpawnPowerUp();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnPowerUp()
    {
        Vector3 spawnPosition;

        if (FindValidSpawnPosition(out spawnPosition))
        {
            GameObject powerUpPrefab = ChoosePowerUpPrefab();

            if (powerUpPrefab != null)
            {
                GameObject newPowerUp = Instantiate(powerUpPrefab, spawnPosition, Quaternion.identity);
                activePowerUps.Add(newPowerUp);

                StartCoroutine(DestroyPowerUpAfterTime(newPowerUp, powerUpLifetime));

                Debug.Log($"Spawned {newPowerUp.name} at {spawnPosition}");
            }
        }
        else
        {
            SpawnPowerUp();
            Debug.LogWarning("Could not find valid spawn position for power-up");
        }
    }

    bool FindValidSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;
        int maxAttempts = 20;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 attemptPosition;

            if (useRandomSpawning)
            {
                attemptPosition = new Vector3(
                    Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                    Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                    0f
                );
            }
            else if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                attemptPosition = randomPoint.position;
            }
            else
            {
                attemptPosition = new Vector3(
                    Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                    Random.Range(spawnAreaMin.y, spawnAreaMax.y),
                    0f
                );
            }

            if (IsPositionClear(attemptPosition))
            {
                position = attemptPosition;
                return true;
            }
        }

        return false;
    }

    bool IsPositionClear(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, spawnCheckRadius, obstacleLayer);
        return hit == null;
    }

    GameObject ChoosePowerUpPrefab()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0)
            return null;

        if (powerUpPrefabs.Length == 1)
            return powerUpPrefabs[0];

        float randomValue = Random.Range(0f, 1f);
        float cumulativeChance = 0f;

        // Spread Shot
        cumulativeChance += spreadShotChance;
        if (randomValue <= cumulativeChance && HasPowerUpOfType(PowerUpType.SpreadShot))
            return GetPowerUpPrefabOfType(PowerUpType.SpreadShot);

        // Shield
        cumulativeChance += shieldChance;
        if (randomValue <= cumulativeChance && HasPowerUpOfType(PowerUpType.ShieldBoost))
            return GetPowerUpPrefabOfType(PowerUpType.ShieldBoost);

        // Explosive Bomb
        if (HasPowerUpOfType(PowerUpType.ExplosiveBomb))
            return GetPowerUpPrefabOfType(PowerUpType.ExplosiveBomb);

        // Fallback to random prefab
        return powerUpPrefabs[Random.Range(0, powerUpPrefabs.Length)];
    }

    bool HasPowerUpOfType(PowerUpType type)
    {
        foreach (GameObject prefab in powerUpPrefabs)
        {
            PowerUp powerUp = prefab.GetComponent<PowerUp>();
            if (powerUp != null && powerUp.powerUpType == type)
                return true;
        }
        return false;
    }

    GameObject GetPowerUpPrefabOfType(PowerUpType type)
    {
        foreach (GameObject prefab in powerUpPrefabs)
        {
            PowerUp powerUp = prefab.GetComponent<PowerUp>();
            if (powerUp != null && powerUp.powerUpType == type)
                return prefab;
        }
        return null;
    }

    void ValidateSpawnChances()
    {
        float total = spreadShotChance + shieldChance + bombChance;
        if (total > 1f)
        {
            Debug.LogWarning($"Power-up spawn chances total {total:F2}, which is greater than 1.0. Consider adjusting values.");
        }
    }

    void CleanupDestroyedPowerUps()
    {
        for (int i = activePowerUps.Count - 1; i >= 0; i--)
        {
            if (activePowerUps[i] == null)
            {
                activePowerUps.RemoveAt(i);
            }
        }
    }

    IEnumerator DestroyPowerUpAfterTime(GameObject powerUp, float time)
    {
        yield return new WaitForSeconds(time);

        if (powerUp != null)
        {
            activePowerUps.Remove(powerUp);
            Destroy(powerUp);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw spawn area
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((spawnAreaMin.x + spawnAreaMax.x) / 2f, (spawnAreaMin.y + spawnAreaMax.y) / 2f, 0f);
        Vector3 size = new Vector3(spawnAreaMax.x - spawnAreaMin.x, spawnAreaMax.y - spawnAreaMin.y, 0f);
        Gizmos.DrawWireCube(center, size);

        // Draw spawn points
        if (spawnPoints != null)
        {
            Gizmos.color = Color.green;
            foreach (Transform point in spawnPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawWireSphere(point.position, spawnCheckRadius);
                }
            }
        }
    }
}