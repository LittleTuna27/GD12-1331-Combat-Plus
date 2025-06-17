using UnityEngine;

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

    private Rigidbody2D rb;
    private float nextFireTime = 0f;
    private Bullet activeBullet;
    private bool canMove = true;

    // Explosive bullet tracking
    private bool nextBulletIsExplosive = false;
    private float explosionRadius = 3f;

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
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        // Stop any unwanted spinning
        if (canMove && rb != null)
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

        // Shooting (only if no active bullet)
        if (Input.GetKeyDown(fireKey) && Time.time >= nextFireTime && activeBullet == null)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Fire()
    {
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

    // Called by power-up system to set next bullet as explosive
    public void SetExplosiveBullet(float radius)
    {
        nextBulletIsExplosive = true;
        explosionRadius = radius;
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

        // Inform game manager that this player was hit
        GameManager.Instance?.PlayerHit(playerNumber);
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
        // Stop spinning on any collision
        rb.angularVelocity = 0f;

        // Handle power-up collection via collision too
        PowerUp powerUp = collision.gameObject.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            powerUp.CollectPowerUp(this);
        }
    }
}