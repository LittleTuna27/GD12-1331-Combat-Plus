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

    [Header("Score")]
    public int score = 0;
    public Text scoreText;

    [Header("Audio")]
    public AudioClip moveClip;
    public AudioClip shootClip;
    private AudioSource audioSource;
    private bool isMoveAudioPlaying = false;

    [Header("UI References - Used by PowerUpManager")]
    public Image powerupIcon;
    public Sprite bombIcon;
    public Sprite shieldIcon;
    public Sprite spreadShotIcon;
    public GameObject bubbleShieldPrefab;

    // Core components
    private Rigidbody2D rb;
    private PowerUpManager powerUpManager;
    
    // Movement and shooting state
    private float nextFireTime = 0f;
    private Bullet activeBullet;
    private bool canMove = true;
    private bool isSpinning = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        powerUpManager = GetComponent<PowerUpManager>();
        audioSource = GetComponent<AudioSource>();

        // Add PowerUpManager if it doesn't exist
        if (powerUpManager == null)
        {
            powerUpManager = gameObject.AddComponent<PowerUpManager>();
        }

        // Setup rigidbody
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.drag = 2f;
            rb.angularDrag = 5f;
            rb.freezeRotation = false;
        }

        // Setup fire point
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(1f, 0f, 0f);
            firePoint = fp.transform;
        }

        UpdateScoreUI();
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
        if (canMove && rb != null && !isSpinning)
        {
            if (Mathf.Abs(rb.angularVelocity) > rotationSpeed * 2f)
            {
                rb.angularVelocity = Mathf.Sign(rb.angularVelocity) * rotationSpeed * 2f;
            }
        }
    }

    void HandleInput()
    {
        // Control active bullet if it exists
        if (activeBullet != null)
        {
            Vector2 bulletInput = Vector2.zero;
            if (Input.GetKey(forwardKey)) bulletInput.y = 1f;
            if (Input.GetKey(backwardKey)) bulletInput.y = -1f;
            if (Input.GetKey(leftKey)) bulletInput.x = -1f;
            if (Input.GetKey(rightKey)) bulletInput.x = 1f;

            activeBullet.HandleInput(bulletInput);
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        if (!canMove) return;

        // Movement input
        float moveInput = 0f;
        if (Input.GetKey(forwardKey))
            moveInput = 1f;
        else if (Input.GetKey(backwardKey))
            moveInput = -0.5f;

        // Rotation input
        float rotationInput = 0f;
        if (Input.GetKey(leftKey))
            rotationInput = 1f;
        else if (Input.GetKey(rightKey))
            rotationInput = -1f;

        // Apply movement and rotation
        Vector2 movement = transform.up * moveInput * moveSpeed;
        rb.velocity = movement;

        if (rotationInput != 0f)
        {
            float rotation = rotationInput * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, 0, rotation);
            rb.angularVelocity = 0f;
        }
        else
        {
            rb.angularVelocity = 0f;
        }

        // Handle movement audio
        HandleMovementAudio();

        // Shooting
        if (Input.GetKeyDown(fireKey) && Time.time >= nextFireTime && activeBullet == null)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void HandleMovementAudio()
    {
        bool isMoving = Input.GetKey(forwardKey) || Input.GetKey(backwardKey) || 
                       Input.GetKey(leftKey) || Input.GetKey(rightKey);

        if (isMoving && !isMoveAudioPlaying && moveClip != null && !audioSource.isPlaying)
        {
            audioSource.clip = moveClip;
            audioSource.loop = true;
            audioSource.Play();
            isMoveAudioPlaying = true;
        }
        else if (!isMoving && isMoveAudioPlaying)
        {
            audioSource.Stop();
            isMoveAudioPlaying = false;
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
            // Check if we should fire spread shot
            if (powerUpManager.ShouldFireSpreadShot())
            {
                FireSpreadShot();
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
            bulletScript.OnTankHit += OnEnemyTankHit;

            // Apply explosive properties if needed
            if (powerUpManager.ShouldSetExplosiveBullet())
            {
                bulletScript.SetExplosive(powerUpManager.GetExplosionRadius());
                powerUpManager.OnExplosiveBulletFired();
            }

            activeBullet = bulletScript;
            canMove = false;
        }
    }

    void FireSpreadShot()
    {
        int bulletCount = powerUpManager.GetSpreadShotBullets();
        float totalSpread = Mathf.Clamp(bulletCount * 15f, 30f, 90f);
        float angleStep = totalSpread / (bulletCount - 1);
        float startAngle = -totalSpread / 2f;

        for (int i = 0; i < bulletCount; i++)
        {
            float currentAngle = startAngle + (angleStep * i);
            Quaternion bulletRotation = firePoint.rotation * Quaternion.Euler(0, 0, currentAngle);
            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, bulletRotation);
            Bullet bulletScript = bullet.GetComponent<Bullet>();

            if (bulletScript != null)
            {
                bulletScript.InitializeSpreadBullet(playerNumber, bulletSpeed);
                bulletScript.OnTankHit += OnEnemyTankHit;

                // Apply explosive to all bullets if active
                if (powerUpManager.ShouldSetExplosiveBullet())
                {
                    bulletScript.SetExplosive(powerUpManager.GetExplosionRadius());
                }
            }
        }

        // Clean up after spread shot
        if (powerUpManager.ShouldSetExplosiveBullet())
        {
            powerUpManager.OnExplosiveBulletFired();
        }
        
        powerUpManager.OnSpreadShotFired();
    }

    private void OnEnemyTankHit(TankController hitTank)
    {
        AddScore(1);
        hitTank.StartSpinAnimation();
    }

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

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        float degreesPerSecond = (360f * spinRotations) / spinDuration;
        float elapsedTime = 0f;
        float startRotation = transform.eulerAngles.z;

        while (elapsedTime < spinDuration)
        {
            elapsedTime += Time.deltaTime;
            float currentRotation = startRotation + (degreesPerSecond * elapsedTime);
            transform.rotation = Quaternion.Euler(0, 0, currentRotation);
            yield return null;
        }

        float finalRotation = startRotation + (360f * spinRotations);
        transform.rotation = Quaternion.Euler(0, 0, finalRotation);

        isSpinning = false;
        canMove = true;
    }

    public void OnBulletDestroyed()
    {
        activeBullet = null;
        canMove = true;
    }

    public void TakeDamage()
    {
        // Check if power-up effects should prevent damage
        if (!powerUpManager.ShouldTakeDamage())
        {
            return; // Damage was absorbed by shield
        }

        // Stop movement and destroy active bullet
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (activeBullet != null)
        {
            Destroy(activeBullet.gameObject);
            activeBullet = null;
            canMove = true;
        }
    }

    public bool HasShield()
    {
        return powerUpManager.HasShield();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
        {
            rb.velocity = -rb.velocity * 0.5f;
            rb.angularVelocity = 0f;
        }

        PowerUp powerUp = other.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            powerUp.CollectPowerUp(this);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isSpinning)
        {
            rb.angularVelocity = 0f;
        }

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
}