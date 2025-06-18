using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum PowerUpType
{
    SpreadShot,
    ShieldBoost,
    ExplosiveBomb
}

public class PowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public PowerUpType powerUpType;
    public float effectDuration = 15f;
    public float effectStrength = 2f;
    public AudioClip collectSound;

    [Header("Visual Effects")]
    public GameObject collectEffect;
    public float rotateSpeed = 90f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && collectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = collectSound;
            audioSource.playOnAwake = false;
        }
    }

    void Update()
    {
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }

    public void CollectPowerUp(TankController tank)
    {
        if (audioSource != null && collectSound != null)
        {
            audioSource.Play();
        }

        if (collectEffect != null)
        {
            Instantiate(collectEffect, transform.position, Quaternion.identity);
        }

        ApplyPowerUp(tank);

        float destroyDelay = (audioSource != null && collectSound != null) ? collectSound.length : 0f;
        Destroy(gameObject, destroyDelay);
    }

    void ApplyPowerUp(TankController tank)
    {
        PowerUpManager powerUpManager = tank.GetComponent<PowerUpManager>();
        if (powerUpManager == null)
        {
            powerUpManager = tank.gameObject.AddComponent<PowerUpManager>();
        }

        switch (powerUpType)
        {
            case PowerUpType.SpreadShot:
                powerUpManager.ActivateSpreadShot((int)effectStrength);
                break;
            case PowerUpType.ShieldBoost:
                powerUpManager.ActivateShield(effectDuration);
                break;
            case PowerUpType.ExplosiveBomb:
                powerUpManager.ActivateExplosiveBomb(effectStrength);
                break;
        }
    }
}

// Consolidated power-up management
public class PowerUpManager : MonoBehaviour
{
    [Header("UI References")]
    public Image powerupIcon;
    public Sprite bombIcon;
    public Sprite shieldIcon;
    public Sprite spreadShotIcon;

    [Header("Shield")]
    public GameObject bubbleShieldPrefab;

    private TankController tankController;
    private GameObject activeShield;

    // Power-up states
    private bool hasShield = false;
    private bool hasExplosiveBomb = false;
    private bool hasSpreadShot = false;
    private int spreadShotBullets = 3;
    private float explosionRadius = 3f;

    void Awake()
    {
        tankController = GetComponent<TankController>();

        // Try to find UI references from TankController if not set
        if (powerupIcon == null && tankController != null)
        {
            powerupIcon = tankController.powerupIcon;
            bombIcon = tankController.bombIcon;
            shieldIcon = tankController.shieldIcon;
            spreadShotIcon = tankController.spreadShotIcon;
            bubbleShieldPrefab = tankController.bubbleShieldPrefab;
        }
    }

    // Public methods for TankController to check power-up states
    public bool ShouldFireSpreadShot() => hasSpreadShot;
    public int GetSpreadShotBullets() => spreadShotBullets;
    public bool ShouldSetExplosiveBullet() => hasExplosiveBomb;
    public float GetExplosionRadius() => explosionRadius;
    public bool HasShield() => hasShield;

    // Power-up activation methods
    public void ActivateSpreadShot(int bulletCount)
    {
        hasSpreadShot = true;
        spreadShotBullets = Mathf.Clamp(bulletCount, 3, 7);
        UpdatePowerUpIcon(spreadShotIcon);
        Debug.Log($"Player {tankController.playerNumber} got Spread Shot! Next shot will fire {spreadShotBullets} bullets!");
    }

    public void ActivateShield(float duration)
    {
        if (!hasShield)
        {
            hasShield = true;
            CreateBubbleShield();
            UpdatePowerUpIcon(shieldIcon);
            StartCoroutine(ShieldTimer(duration));
            Debug.Log($"Player {tankController.playerNumber} got Shield!");
        }
    }

    public void ActivateExplosiveBomb(float radius)
    {
        hasExplosiveBomb = true;
        explosionRadius = radius;
        UpdatePowerUpIcon(bombIcon);
        Debug.Log($"Player {tankController.playerNumber} got Explosive Bomb! Next shot will explode!");
    }

    // Called by TankController when effects are used
    public void OnSpreadShotFired()
    {
        hasSpreadShot = false;
        spreadShotBullets = 3;
        ClearPowerUpIcon();
    }

    public void OnExplosiveBulletFired()
    {
        hasExplosiveBomb = false;
        ClearPowerUpIcon();
    }

    // Damage checking - returns true if damage should be taken
    public bool ShouldTakeDamage()
    {
        if (hasShield)
        {
            DeactivateShield();
            return false; // Shield absorbed the hit
        }
        return true; // Take normal damage
    }

    // Private helper methods
    private void CreateBubbleShield()
    {
        if (bubbleShieldPrefab != null && activeShield == null)
        {
            activeShield = Instantiate(bubbleShieldPrefab, transform.position, Quaternion.identity);
            BubbleShield shieldScript = activeShield.GetComponent<BubbleShield>();
            if (shieldScript != null)
            {
                shieldScript.AttachToOwner(gameObject);
            }
        }
    }

    private IEnumerator ShieldTimer(float duration)
    {
        yield return new WaitForSeconds(duration);
        if (hasShield) // Check if shield wasn't already removed by damage
        {
            DeactivateShield();
        }
    }

    private void DeactivateShield()
    {
        hasShield = false;

        if (activeShield != null)
        {
            Destroy(activeShield);
            activeShield = null;
        }

        ClearPowerUpIcon();
        Debug.Log($"Player {tankController.playerNumber} Shield ended");
    }

    private void UpdatePowerUpIcon(Sprite icon)
    {
        if (powerupIcon != null && icon != null)
        {
            powerupIcon.sprite = icon;
            powerupIcon.enabled = true;
        }
    }

    private void ClearPowerUpIcon()
    {
        if (powerupIcon != null)
        {
            powerupIcon.enabled = false;
        }
    }
}