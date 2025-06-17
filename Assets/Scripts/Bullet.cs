using UnityEngine;
using System;

public class Bullet : MonoBehaviour
{
    private int ownerPlayer;
    private float speed;
    private Rigidbody2D rb;
    private float lifetime = 5f;
    private TankController ownerTank;

    [Header("Bullet Control")]
    public float controlForce = 200f;

    [Header("Explosion")]
    public GameObject explosionEffectPrefab;
    public AudioClip explosionSound;

    // Explosive properties
    private bool isExplosive = false;
    private float explosionRadius = 3f;

    // Event for when this bullet hits a tank
    public event Action<TankController> OnTankHit;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Destroy bullet after lifetime
        Destroy(gameObject, lifetime);
    }

    // Normal bullet initialization (player controllable)
    public void Initialize(TankController tank, int playerNumber, float bulletSpeed)
    {
        ownerTank = tank;
        ownerPlayer = playerNumber;
        speed = bulletSpeed;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        // Set initial velocity
        rb.velocity = transform.up * speed;
    }

    // Spread shot bullet initialization (NOT player controllable)
    public void InitializeSpreadBullet(int playerNumber, float bulletSpeed)
    {
        ownerPlayer = playerNumber;
        speed = bulletSpeed;
        ownerTank = null; // No tank reference = no player control

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        // Set initial velocity - spread bullets just fly straight
        rb.velocity = transform.up * speed;
    }

    public void SetExplosive(float radius)
    {
        isExplosive = true;
        explosionRadius = radius;
    }

    public void HandleInput(Vector2 input)
    {
        // Only allow control if this bullet has an owner tank (normal bullets only)
        if (rb == null || ownerTank == null) return;

        // Apply force to control bullet direction
        rb.AddForce(input * controlForce * Time.deltaTime);

        // Limit maximum speed
        if (rb.velocity.magnitude > speed * 1.5f)
        {
            rb.velocity = rb.velocity.normalized * speed * 1.5f;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandleCollision(other.gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    void HandleCollision(GameObject hitObject)
    {
        // Check if hit a tank
        if (CheckTankHit(hitObject))
            return;

        // Check if hit a wall
        if (CheckWallHit(hitObject))
            return;

        // Check for other destructible objects
        if (CheckOtherDestructibles(hitObject))
            return;
    }

    bool CheckTankHit(GameObject hitObject)
    {
        TankController tank = hitObject.GetComponent<TankController>();
        if (tank != null && tank.playerNumber != ownerPlayer)
        {
            // Fire the event BEFORE dealing damage/effects
            OnTankHit?.Invoke(tank);

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
        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            explosion.transform.localScale = Vector3.one * (explosionRadius * 2f); // Visual size

            ExplosionArea area = explosion.GetComponent<ExplosionArea>();
            if (area != null)
            {
                area.ownerPlayerNumber = ownerPlayer;
            }
        }

        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
    }

    bool CheckWallHit(GameObject hitObject)
    {
        if (hitObject.CompareTag("Wall") ||
            hitObject.name.ToLower().Contains("wall") ||
            hitObject.layer == LayerMask.NameToLayer("Walls"))
        {
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
        if (hitObject.CompareTag("Wall"))
        {
            DestroyBullet();
            return true;
        }
        if (hitObject.CompareTag("Player"))
        {
            DestroyBullet();

            if (ownerTank != null)
            {
                ownerTank.AddScore(1);
                Debug.Log("Hit Player");
            }

            return true;
        }
        return false;
    }

    void DestroyBullet()
    {
        // Notify owner tank that bullet is destroyed (only for controllable bullets)
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