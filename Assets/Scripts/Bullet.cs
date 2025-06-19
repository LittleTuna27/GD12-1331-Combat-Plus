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

    private bool hasHit = false;

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
        if (hasHit) return;

        // BLOCKED BY SHIELD?
        if (other.CompareTag("Shield"))
        {
            hasHit = true;

            Debug.Log("Bullet blocked by shield!");

            // Optional: destroy shield on hit
            Destroy(other.gameObject);

            Destroy(gameObject);
            return;
        }

        // THEN check for tanks
        TankController tank = other.GetComponent<TankController>();
        if (tank != null && tank.playerNumber != ownerPlayer)
        {
            hasHit = true;
            OnTankHit?.Invoke(tank);
            Destroy(gameObject);
        }
    }




    bool CheckTankHit(GameObject hitObject)
    {
        Debug.Log($"CheckTankHit called for: {hitObject.name}");

        TankController tank = hitObject.GetComponent<TankController>();
        if (tank != null)
        {
            Debug.Log($"Found TankController on {hitObject.name}, player number: {tank.playerNumber}, owner: {ownerPlayer}");

            if (tank.playerNumber != ownerPlayer)
            {
                Debug.Log($"Valid hit! Tank {tank.playerNumber} hit by player {ownerPlayer}'s bullet");

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
            else
            {
                Debug.Log($"Same player hit - ignoring (tank: {tank.playerNumber}, owner: {ownerPlayer})");
            }
        }
        else
        {
            Debug.Log($"No TankController found on {hitObject.name}");
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
        // Check for any remaining tank hits that might have been missed
        TankController tank = hitObject.GetComponent<TankController>();
        if (tank != null && tank.playerNumber != ownerPlayer)
        {
            Debug.Log("Caught tank hit in CheckOtherDestructibles!");
            OnTankHit?.Invoke(tank);
            tank.TakeDamage();
            DestroyBullet();
            return true;
        }

        if (hitObject.CompareTag("Wall"))
        {
            DestroyBullet();
            return true;
        }
        if (hitObject.CompareTag("Player"))
        {
            DestroyBullet();
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