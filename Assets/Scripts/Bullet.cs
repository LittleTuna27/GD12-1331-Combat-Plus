using UnityEngine;

public class Bullet : MonoBehaviour
{
    private int ownerPlayer;
    private float speed;
    private Rigidbody2D rb;
    private float lifetime = 5f;
    private TankController ownerTank;

    [Header("Bullet Control")]
    public float controlForce = 200f;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    [Header("Explosion")]
    public GameObject explosionEffectPrefab;
    public AudioClip explosionSound;

    // Explosive properties
    private bool isExplosive = false;
    private float explosionRadius = 3f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Ensure rigidbody settings are correct
        if (rb != null)
        {
            rb.gravityScale = 0f; // No gravity for top-down
            rb.drag = 0f; // No drag unless you want it
            rb.angularDrag = 0f;
        }

        // Destroy bullet after lifetime
        Destroy(gameObject, lifetime);

        if (enableDebugLogs)
            Debug.Log($"Bullet created by player {ownerPlayer}");
    }

    public void Initialize(TankController tank, int playerNumber, float bulletSpeed)
    {
        ownerTank = tank;
        ownerPlayer = playerNumber;
        speed = bulletSpeed;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        // Set initial velocity
        rb.velocity = transform.up * speed;

        if (enableDebugLogs)
            Debug.Log($"Bullet initialized with speed {speed}");
    }

    public void SetExplosive(float radius)
    {
        isExplosive = true;
        explosionRadius = radius;

        if (enableDebugLogs)
            Debug.Log($"Bullet set to explosive with radius {radius}");
    }

    public void HandleInput(Vector2 input)
    {
        if (rb == null) return;

        // Apply force to control bullet direction
        rb.AddForce(input * controlForce * Time.deltaTime);

        // Limit maximum speed
        if (rb.velocity.magnitude > speed * 1.5f)
        {
            rb.velocity = rb.velocity.normalized * speed * 1.5f;
        }
    }

    // Use both OnTriggerEnter2D and OnCollisionEnter2D for better detection
    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject, "Trigger");
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject, "Collision");
    }

    void HandleCollision(GameObject hitObject, string collisionType)
    {
        if (enableDebugLogs)
            Debug.Log($"Bullet {collisionType}: {hitObject.name} with tag: {hitObject.tag}");

        // Check if hit a tank
        if (CheckTankHit(hitObject))
            return;

        // Check if hit a wall
        if (CheckWallHit(hitObject))
            return;

        // Check for other objects that should destroy the bullet
        if (CheckOtherDestructibles(hitObject))
            return;
    }

    bool CheckTankHit(GameObject hitObject)
    {
        TankController tank = hitObject.GetComponent<TankController>();
        if (tank != null && tank.playerNumber != ownerPlayer)
        {
            if (enableDebugLogs)
                Debug.Log($"Bullet hit enemy tank {tank.playerNumber}");

            if (isExplosive)
            {
                CreateExplosion();
            }
            else
            {
                tank.TakeDamage();
            }

            DestroyBullet();
            return true;
        }
        return false;
    }

    void CreateExplosion()
    {
        if (enableDebugLogs)
            Debug.Log($"Creating explosion at {transform.position} with radius {explosionRadius}");

        // Spawn explosion visual effect
        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);

            // Scale the explosion effect based on radius
            explosion.transform.localScale = Vector3.one * (explosionRadius / 2f);

            // Destroy explosion effect after a few seconds
            Destroy(explosion, 3f);
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // Find all colliders within explosion radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (Collider2D hitCollider in hitColliders)
        {
            // Check if it's a tank
            TankController tank = hitCollider.GetComponent<TankController>();
            if (tank != null && tank.playerNumber != ownerPlayer)
            {
                if (enableDebugLogs)
                    Debug.Log($"Explosion hit tank {tank.playerNumber}");

                tank.TakeDamage();
            }

            // You could also damage destructible objects here
            // DestructibleObject destructible = hitCollider.GetComponent<DestructibleObject>();
            // if (destructible != null) destructible.TakeDamage();
        }
    }

    bool CheckWallHit(GameObject hitObject)
    {
        // Check by tag
        if (hitObject.CompareTag("Wall"))
        {
            if (enableDebugLogs)
                Debug.Log("Bullet hit wall (by tag)");

            if (isExplosive)
            {
                CreateExplosion();
            }

            DestroyBullet();
            return true;
        }

        // Check by name (backup method)
        if (hitObject.name.ToLower().Contains("wall"))
        {
            if (enableDebugLogs)
                Debug.Log("Bullet hit wall (by name)");

            if (isExplosive)
            {
                CreateExplosion();
            }

            DestroyBullet();
            return true;
        }

        // Check by layer (if you're using layers)
        if (hitObject.layer == LayerMask.NameToLayer("Walls"))
        {
            if (enableDebugLogs)
                Debug.Log("Bullet hit wall (by layer)");

            if (isExplosive)
            {
                CreateExplosion();
            }

            DestroyBullet();
            return true;
        }

        return false;
    }

    bool CheckOtherDestructibles(GameObject hitObject)
    {
        // Add other objects that should destroy bullets
        if (hitObject.CompareTag("Obstacle") || hitObject.CompareTag("Barrier"))
        {
            if (enableDebugLogs)
                Debug.Log($"Bullet hit destructible: {hitObject.tag}");

            DestroyBullet();
            return true;
        }

        return false;
    }

    void DestroyBullet()
    {
        if (enableDebugLogs)
            Debug.Log("Destroying bullet");

        // Notify owner tank that bullet is destroyed
        if (ownerTank != null)
        {
            ownerTank.OnBulletDestroyed();
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        // Make sure tank is notified even if destroyed by lifetime
        if (ownerTank != null)
        {
            ownerTank.OnBulletDestroyed();
        }
    }
}