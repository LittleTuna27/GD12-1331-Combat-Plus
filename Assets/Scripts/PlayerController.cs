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

    [Header("Animation")]
    public Animator tankAnimator;
    public string idleAnimationName = "Tank_Idle";
    public string movingAnimationName = "Tank_Moving";
    public string spinningAnimationName = "Tank_Spinning";

    private Rigidbody2D rb;
    private float nextFireTime = 0f;
    private int health = 1;
    private Bullet activeBullet;
    private bool canMove = true;
    private bool isSpinning = false;
    private float spinDuration = 2f;
    private float spinSpeed = 720f;
    private float spinTimer = 0f;

    // Animation state tracking
    private bool isMoving = false;
    private string currentAnimationState = "";

    // Explosive bullet tracking
    private bool nextBulletIsExplosive = false;
    private float explosionRadius = 3f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Get animator if not assigned
        if (tankAnimator == null)
            tankAnimator = GetComponent<Animator>();

        // Set up fire point if not assigned
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(transform);
            fp.transform.localPosition = new Vector3(1f, 0f, 0f);
            firePoint = fp.transform;
        }

        // Start with idle animation
        ChangeAnimationState(idleAnimationName);
    }

    void Update()
    {
        if (isSpinning)
        {
            HandleSpinning();
        }
        else
        {
            HandleInput();
        }
    }

    void HandleSpinning()
    {
        // The spinning animation handles the visual rotation
        // We just need to manage the timer and movement
        rb.velocity = Vector2.zero;

        // Update spin timer
        spinTimer -= Time.deltaTime;
        if (spinTimer <= 0f)
        {
            isSpinning = false;
            canMove = true;
            ChangeAnimationState(idleAnimationName);
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

            // Freeze tank movement
            rb.velocity = Vector2.zero;

            // Set to idle since tank isn't moving
            if (isMoving)
            {
                isMoving = false;
                ChangeAnimationState(idleAnimationName);
            }
            return;
        }

        // Normal tank movement (only if no active bullet and not spinning)
        if (!canMove || isSpinning) return;

        // Movement
        float moveInput = 0f;
        if (Input.GetKey(forwardKey))
            moveInput = 1f;
        else if (Input.GetKey(backwardKey))
            moveInput = -0.5f; // Slower reverse like original Combat

        // Rotation
        float rotationInput = 0f;
        if (Input.GetKey(leftKey))
            rotationInput = 1f;
        else if (Input.GetKey(rightKey))
            rotationInput = -1f;

        // Check if tank is moving for animation
        bool shouldBeMoving = (moveInput != 0f || rotationInput != 0f);

        // Update animation state
        if (shouldBeMoving && !isMoving)
        {
            isMoving = true;
            ChangeAnimationState(movingAnimationName);
        }
        else if (!shouldBeMoving && isMoving)
        {
            isMoving = false;
            ChangeAnimationState(idleAnimationName);
        }

        // Apply movement
        Vector2 movement = transform.up * moveInput * moveSpeed;
        rb.velocity = movement;

        // Apply rotation
        float rotation = rotationInput * rotationSpeed * Time.deltaTime;
        transform.Rotate(0, 0, rotation);

        // Shooting (only if no active bullet and not spinning)
        if (Input.GetKeyDown(fireKey) && Time.time >= nextFireTime && activeBullet == null && !isSpinning)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void ChangeAnimationState(string newState)
    {
        // Stop the same animation from interrupting itself
        if (currentAnimationState == newState) return;

        // Play the animation
        if (tankAnimator != null)
        {
            tankAnimator.Play(newState);
        }

        currentAnimationState = newState;
    }

    void Fire()
    {
        if (bulletPrefab != null && firePoint != null && activeBullet == null)
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
            return; // Damage was absorbed by shield or extra life
        }

        // Start spinning animation
        isSpinning = true;
        canMove = false;
        isMoving = false;
        spinTimer = spinDuration;

        // Change to spinning animation
        ChangeAnimationState(spinningAnimationName);

        // Stop any current movement
        rb.velocity = Vector2.zero;

        // Destroy any active bullet
        if (activeBullet != null)
        {
            Destroy(activeBullet.gameObject);
            activeBullet = null;
        }

        // Inform game manager that this player was hit
        GameManager.Instance?.PlayerHit(playerNumber);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Handle wall bouncing if needed
        if (other.CompareTag("Wall"))
        {
            // Simple bounce - reverse velocity
            rb.velocity = -rb.velocity * 0.5f;
        }

        // Handle power-up collection
        PowerUp powerUp = other.GetComponent<PowerUp>();
        if (powerUp != null)
        {
            powerUp.CollectPowerUp(this);
        }
    }
}