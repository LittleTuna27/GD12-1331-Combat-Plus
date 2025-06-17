using UnityEngine;

public enum PowerUpType
{
    SpreadShot,     // Replaces SpeedBoost and RapidFire
    ShieldBoost,
    ExplosiveBomb
}

public class PowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public PowerUpType powerUpType;
    public float effectDuration = 5f;
    public float effectStrength = 2f;
    public AudioClip collectSound;

    [Header("Visual Effects")]
    public GameObject collectEffect;
    public float rotateSpeed = 90f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // If no audio source exists, create one
        if (audioSource == null && collectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = collectSound;
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        // Rotate only (bobbing removed)
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }

    public void CollectPowerUp(TankController tank)
    {
        // Play sound effect
        if (audioSource != null && collectSound != null)
        {
            audioSource.Play();
        }

        // Spawn visual effect
        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        // Apply power-up effect
        ApplyPowerUp(tank);

        // Destroy the power-up (after a short delay if sound is playing)
        float destroyDelay = (audioSource != null && collectSound != null) ? collectSound.length : 0f;
        Destroy(gameObject, destroyDelay);
    }

    void ApplyPowerUp(TankController tank)
    {
        PowerUpEffect effect = tank.GetComponent<PowerUpEffect>();
        if (effect == null)
        {
            effect = tank.gameObject.AddComponent<PowerUpEffect>();
        }

        switch (powerUpType)
        {
            case PowerUpType.SpreadShot:
                // effectStrength determines number of bullets (3 = 3 bullets, 5 = 5 bullets, etc.)
                effect.ApplySpreadShot((int)effectStrength);
                break;
            case PowerUpType.ShieldBoost:
                effect.ApplyShield(effectDuration);
                break;
            case PowerUpType.ExplosiveBomb:
                effect.ApplyExplosiveBomb(effectStrength);
                break;
        }
    }
}

// Component to handle power-up effects on tanks
public class PowerUpEffect : MonoBehaviour
{
    private TankController tankController;

    // Effect tracking
    private bool hasShield = false;
    private bool hasExplosiveBomb = false;
    private bool hasSpreadShot = false;
    private int spreadShotBullets = 3; // Number of bullets in spread

    void Awake()
    {
        // Initialize tankController in Awake instead of Start
        // This ensures it's available immediately when the component is added
        tankController = GetComponent<TankController>();
    }

    void Start()
    {
        // Keep this as backup in case Awake didn't run
        if (tankController == null)
        {
            tankController = GetComponent<TankController>();
        }
    }

    public void ApplySpreadShot(int bulletCount)
    {
        hasSpreadShot = true;
        spreadShotBullets = Mathf.Clamp(bulletCount, 3, 7); // Limit between 3-7 bullets

        // Ensure tankController is available
        if (tankController == null)
            tankController = GetComponent<TankController>();

        Debug.Log($"Player {tankController?.playerNumber ?? 0} got Spread Shot! Next shot will fire {spreadShotBullets} bullets!");
    }

    public void ApplyShield(float duration)
    {
        // Ensure tankController is available
        if (tankController == null)
            tankController = GetComponent<TankController>();

        if (!hasShield)
        {
            hasShield = true;
            Invoke(nameof(RemoveShield), duration);

            // Visual indicator for shield (you can add a shield sprite here)
            Debug.Log($"Player {tankController?.playerNumber ?? 0} got Shield!");
        }
    }

    public void ApplyExplosiveBomb(float explosionRadius)
    {
        // Ensure tankController is available
        if (tankController == null)
            tankController = GetComponent<TankController>();

        hasExplosiveBomb = true;
        Debug.Log($"Player {tankController?.playerNumber ?? 0} got Explosive Bomb! Next shot will explode!");

        // Store explosion radius for when bullet hits
        GetComponent<TankController>().SetExplosiveBullet(explosionRadius);
    }

    // Remove effect methods
    void RemoveShield()
    {
        hasShield = false;

        // Ensure tankController is available
        if (tankController == null)
            tankController = GetComponent<TankController>();

        Debug.Log($"Player {tankController?.playerNumber ?? 0} Shield ended");
    }

    // Method to check if tank should take damage (called from TakeDamage)
    public bool ShouldTakeDamage()
    {
        if (hasShield)
        {
            RemoveShield();
            CancelInvoke(nameof(RemoveShield));
            return false; // Shield absorbed the hit
        }

        return true; // Take normal damage
    }

    // Called when explosive bullet is fired
    public void OnExplosiveBulletFired()
    {
        hasExplosiveBomb = false;
    }

    // Check if next shot should be spread shot
    public bool ShouldFireSpreadShot()
    {
        return hasSpreadShot;
    }

    // Get number of bullets for spread shot
    public int GetSpreadShotBullets()
    {
        return spreadShotBullets;
    }

    // Called when spread shot is fired
    public void OnSpreadShotFired()
    {
        hasSpreadShot = false;
        spreadShotBullets = 3; // Reset to default
    }
}