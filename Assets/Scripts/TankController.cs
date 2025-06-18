using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TankController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;

    [Header("Shooting Settings")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float fireRate = 0.5f;
    public float bulletSpeed = 5f;

    [Header("Player Settings")]
    public int playerNumber = 1;
    public KeyCode forwardKey = KeyCode.W;
    public KeyCode backwardKey = KeyCode.S;
    public KeyCode leftKey = KeyCode.A;
    public KeyCode rightKey = KeyCode.D;
    public KeyCode fireKey = KeyCode.Space;

    [Header("Hit Animation")]
    public float spinDuration = 1f;
    public int spinRotations = 3;

    private Rigidbody2D rb;
    private float nextFireTime = 0f;
    private Bullet activeBullet;
    private bool canMove = true;
    private bool isSpinning = false;

    // Explosive bullet tracking
    private bool nextBulletIsExplosive = false;
    private float explosionRadius = 3f;

    [Header("Score")]
    public int score = 0;
    public Text scoreText;

    [Header("Audio")]
    public AudioClip moveClip;
    public AudioClip shootClip;
    private AudioSource audioSource;
    private bool isMoveAudioPlaying = false;

    [Header("UI")]
    public UnityEngine.UI.Image powerupIcon;
    public Sprite bombIcon;
    public Sprite shieldIcon;
    public Sprite spreadShotIcon;

    public GameObject bubbleShieldPrefab;
    private GameObject activeShield;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Ensure proper rigidbody settings to prevent spinning
        if (rb != null)
        {
            rb.gravityScale = 0f; // No gravity for top-down
            rb.drag = 2f; // Add some drag to prevent sliding
            rb.angularDrag = 5f; // Add angular drag to prevent spinning
            rb.freezeRotation = false; // Allow controlled rotation
        }

        // Set up fire point if not assigned
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(1f, 0f, 0f);
            firePoint = fp.transform;
        }

        UpdateScoreUI();
        ClearPowerupIcon();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!isSpinning)
        {
            HandleInput();
        }
    }

    void FixedUpdate()
    {
        // Stop any unwanted spinning (only when not intentionally spinning)
        if (canMove && rb != null && !isSpinning)
        {
            // Clamp angular velocity to prevent uncontrolled spinning
            if (Mathf.Abs(rb.angularVelocity) > rotationSpeed * 2f)
            {
                rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * rotationSpeed * 2f;
            }
        }
    }

    void HandleInput()
    {
        // If we have an active bullet, control it instead of the tank
        if (activeBullet != null)
        {
            // Control bullet with same keys
            Vector2 bulletInput = Vector2.zero;
            if (Input.GetKey(forwardKey)) bulletInput.y = 1f;
            if (Input.GetKey(backwardKey)) bulletInput.y = -1f;
            if (Input.GetKey(leftKey)) bulletInput.x = -1f;
            if (Input.GetKey(rightKey)) bulletInput.x = 1f;

            activeBullet.HandleInput(bulletInput);

            // Freeze tank movement and rotation
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        // Normal tank movement (only if no active bullet)
        if (!canMove) return;

        // Movement
        float moveInput = 0f;
        if (Input.GetKey(forwardKey))
            moveInput = 1f;
        else if (Input.GetKey(backwardKey))
            moveInput = -0.5f; // Slower reverse

        // Rotation
        float rotationInput = 0f;
        if (Input.GetKey(leftKey))
            rotationInput = 1f;
        else if (Input.GetKey(rightKey))
            rotationInput = -1f;

        // Apply movement
        Vector2 movement = transform.up * moveInput * moveSpeed;
        rb.velocity = movement;

        // Apply rotation using transform instead of physics to prevent spinning
        if (rotationInput != 0f)
        {
            float rotation = rotationInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, 0, rotation);
            // Stop any physics-based angular velocity
            rb.angularVelocity = 0f;
        }
        else
        {
            // Stop rotation when no input
            rb.angularVelocity = 0f;
        }

        // Start movement sound if pressing a movement key
        if ((Input.GetKeyDown(forwardKey) || Input.GetKeyDown(backwardKey) ||
             Input.GetKeyDown(leftKey) || Input.GetKeyDown(rightKey)) && moveClip != null && !audioSource.isPlaying)
        {
            audioSource.clip = moveClip;
            audioSource.loop = true;
            audioSource.Play();
            isMoveAudioPlaying = true;
        }

        // Stop movement sound if all movement keys are released
        if (isMoveAudioPlaying && !Input.GetKey(forwardKey) && !Input.GetKey(backwardKey) &&
            !Input.GetKey(leftKey) && !Input.GetKey(rightKey))
        {
            audioSource.Stop();
            isMoveAudioPlaying = false;
        }

        // Shooting (only if no active bullet)
        if (Input.GetKeyDown(fireKey) && Time.time >= nextFireTime && activeBullet == null)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Fire()
    {
        if (shootClip != null)
        {
            audioSource.PlayOneShot(shootClip);
        }
        if (bulletPrefab != null && firePoint != null && activeBullet == null)
        {
            PowerUpEffect powerUp = GetComponent<PowerUpEffect>();

            // Check if we should fire spread shot
            if (powerUp != null && powerUp.ShouldFireSpreadShot())
            {
                FireSpreadShot(powerUp.GetSpreadShotBullets());
                powerUp.OnSpreadShotFired();
            }
            else
            {
                FireSingleBullet();
            }
        }
    }

    void FireSingleBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.Initialize(this, playerNumber, bulletSpeed);

            // Subscribe to the hit event
            bulletScript.OnTankHit += OnEnemyTankHit;

            // Set explosive properties if needed
            if (nextBulletIsExplosive)
            {
                bulletScript.SetExplosive(explosionRadius);
                nextBulletIsExplosive = false;

                // Notify power-up effect that explosive bullet was fired
                PowerUpEffect effect = GetComponent<PowerUpEffect>();
                if (effect != null)
                {
                    effect.OnExplosiveBulletFired();
                }
            }

            activeBullet = bulletScript;
            canMove = false;
        }
    }

    void FireSpreadShot(int bulletCount)
    {
        // Calculate angle spread - wider spread for more bullets
        float totalSpread = Mathf.Clamp(bulletCount * 15f, 30f, 90f);
        float angleStep = totalSpread / (bulletCount - 1);
        float startAngle = -totalSpread / 2f;

        for (int i = 0; i < bulletCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);

            // Calculate rotation for this bullet
            Quaternion bulletRotation = firePoint.rotation * Quaternion.Euler(0, 0, currentAngle);

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, bulletRotation);
            Bullet bulletScript = bullet.GetComponent<Bullet>();

            if (bulletScript != null)
            {
                // Initialize as NON-CONTROLLABLE bullets
                bulletScript.InitializeSpreadBullet(playerNumber, bulletSpeed);

                // Subscribe to hit events for spread bullets too
                bulletScript.OnTankHit += OnEnemyTankHit;

                // Apply explosive to all bullets if active
                if (nextBulletIsExplosive)
                {
                    bulletScript.SetExplosive(explosionRadius);
                }
            }
        }

        // Reset explosive after firing spread shot
        if (nextBulletIsExplosive)
        {
            nextBulletIsExplosive = false;
            PowerUpEffect effect = GetComponent<PowerUpEffect>();
            if (effect != null)
            {
                effect.OnExplosiveBulletFired();
            }
        }

        // Tank can move immediately after spread shot
    }

    private void OnEnemyTankHit(TankController hitTank)
    {

        AddScore(1); // The one who fires the bullet gets the point
        hitTank.StartSpinAnimation();
    }

    // Method to start the spin animation when this tank gets hit
    public void StartSpinAnimation()
    {
        if (!isSpinning)
        {
            StartCoroutine(SpinAnimation());
        }
    }

    private IEnumerator SpinAnimation()
    {
        isSpinning = true;
        canMove = false;

        // Stop current movement
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Calculate spin speed to complete the desired rotations in the given time
        float degreesPerSecond = (360f * spinRotations) / spinDuration;

        float elapsedTime = 0f;
        float startRotation = transform.eulerAngles.z;

        while (elapsedTime < spinDuration)
        {
            elapsedTime += Time.deltaTime;

            // Calculate current rotation
            float currentRotation = startRotation + (degreesPerSecond * elapsedTime);
            transform.rotation = Quaternion.Euler(0, 0, currentRotation);

            yield return null;
        }

        // Ensure we end at the correct rotation (complete rotations)
        float finalRotation = startRotation + (360f * spinRotations);
        transform.rotation = Quaternion.Euler(0, 0, finalRotation);

        isSpinning = false;
        canMove = true;
    }

    // Called by bullet when it's destroyed
    public void OnBulletDestroyed()
    {
        activeBullet = null;
        canMove = true;
    }

    public void TakeDamage()
    {
        // Check if power-up effects should prevent damage
        PowerUpEffect powerUpEffect = GetComponent<PowerUpEffect>();
        if (powerUpEffect != null && !powerUpEffect.ShouldTakeDamage())
        {
            return; // Damage was absorbed by shield
        }

        // Stop any current movement and rotation
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        // Destroy any active bullet
        if (activeBullet != null)
        {
            Destroy(activeBullet.gameObject);
            activeBullet = null;
            canMove = true;
        }

        // The spin animation and point awarding is now handled by the event system
        // This method now just handles the damage/reset logic
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle wall bouncing with controlled physics
        if (other.CompareTag("Wall"))
        {
            // Simple bounce - reverse velocity but stop angular velocity
            rb.velocity = -rb.velocity * 0.5f;
            rb.angularVelocity = 0f; // Stop spinning on wall hit
        }

        // Handle power-up collection
        PowerUp powerUp = other.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            powerUp.CollectPowerUp(this);
        }

    }

    // Also handle collision-based power-up pickup as backup
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Stop spinning on any collision (only if not intentionally spinning)
        if (!isSpinning)
        {
            rb.angularVelocity = 0f;
        }

        // Handle power-up collection via collision too
        PowerUp powerUp = collision.gameObject.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            powerUp.CollectPowerUp(this);
        }
    }
  
    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Player {playerNumber}: {score}";
        }
    }

    public void SetExplosiveBullet(float radius)
    {
        nextBulletIsExplosive = true;
        explosionRadius = radius;

        if (powerupIcon != null && bombIcon != null)
        {
            powerupIcon.sprite = bombIcon;
            powerupIcon.enabled = true;
        }
    }

    public void SetShieldIcon()
    {
        if (powerupIcon != null && shieldIcon != null)
        {
            powerupIcon.sprite = shieldIcon;
            powerupIcon.enabled = true;
        }
    }
    public void SetSpreadShotIcon()
    {
        if (powerupIcon != null && spreadShotIcon != null)
        {
            powerupIcon.sprite = spreadShotIcon;
            powerupIcon.enabled = true;
        }
    }

    public void ClearPowerupIcon()
    {
        if (powerupIcon != null)
        {
            powerupIcon.enabled = false;
        }
    }

    public bool HasShield()
    {
        return activeShield != null;
    }
}