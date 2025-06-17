using UnityEngine;

public enum PowerUpType
{
    SpeedBoost,
    RapidFire,
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
    public float bobSpeed = 2f;
    public float bobHeight = 0.5f;
    public float rotateSpeed = 90f;

    private Vector3 startPos;
    private AudioSource audioSource;

    void Start()
    {
        startPos = transform.position;
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
        // Bob up and down
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // Rotate
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
            case PowerUpType.SpeedBoost:
                effect.ApplySpeedBoost(effectDuration, effectStrength);
                break;
            case PowerUpType.RapidFire:
                effect.ApplyRapidFire(effectDuration, effectStrength);
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
    private float originalMoveSpeed;
    private float originalFireRate;

    // Effect tracking
    private bool hasSpeedBoost = false;
    private bool hasRapidFire = false;
    private bool hasShield = false;
    private bool hasExplosiveBomb = false;

    void Start()
    {
        tankController = GetComponent<TankController>();
        if (tankController != null)
        {
            originalMoveSpeed = tankController.moveSpeed;
            originalFireRate = tankController.fireRate;
        }
    }

    public void ApplySpeedBoost(float duration, float multiplier)
    {
        if (tankController == null) return;

        if (!hasSpeedBoost)
        {
            tankController.moveSpeed *= multiplier;
            hasSpeedBoost = true;
            Invoke(nameof(RemoveSpeedBoost), duration);

            Debug.Log($"Player {tankController.playerNumber} got Speed Boost!");
        }
    }

    public void ApplyRapidFire(float duration, float multiplier)
    {
        if (tankController == null) return;

        if (!hasRapidFire)
        {
            tankController.fireRate /= multiplier; // Lower fire rate = faster shooting
            hasRapidFire = true;
            Invoke(nameof(RemoveRapidFire), duration);

            Debug.Log($"Player {tankController.playerNumber} got Rapid Fire!");
        }
    }

    public void ApplyShield(float duration)
    {
        if (!hasShield)
        {
            hasShield = true;
            Invoke(nameof(RemoveShield), duration);

            // Visual indicator for shield (you can add a shield sprite here)
            Debug.Log($"Player {tankController.playerNumber} got Shield!");
        }
    }

    public void ApplyExplosiveBomb(float explosionRadius)
    {
        hasExplosiveBomb = true;
        Debug.Log($"Player {tankController.playerNumber} got Explosive Bomb! Next shot will explode!");

        // Store explosion radius for when bullet hits
        GetComponent<TankController>().SetExplosiveBullet(explosionRadius);
    }

    // Remove effect methods
    void RemoveSpeedBoost()
    {
        if (tankController != null)
        {
            tankController.moveSpeed = originalMoveSpeed;
        }
        hasSpeedBoost = false;
        Debug.Log($"Player {tankController.playerNumber} Speed Boost ended");
    }

    void RemoveRapidFire()
    {
        if (tankController != null)
        {
            tankController.fireRate = originalFireRate;
        }
        hasRapidFire = false;
        Debug.Log($"Player {tankController.playerNumber} Rapid Fire ended");
    }

    void RemoveShield()
    {
        hasShield = false;
        Debug.Log($"Player {tankController.playerNumber} Shield ended");
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
}