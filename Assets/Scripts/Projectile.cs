using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    public float speed = 15f;
    public float damage = 10f;
    public float lifetime = 5f;
    public LayerMask targetLayer;

    [Header("Effects")]
    public GameObject hitEffect;
    public AudioClip hitSound;

    private Vector3 direction;
    private bool isInitialized = false;
    private AudioSource audioSource;
    private float autoReturnDelay;
    private ProjectilePool pool;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        pool = FindObjectOfType<ProjectilePool>();
    }

    /// <summary>
    /// Initializes the projectile to fly in a specific direction. Used by the ProjectilePool.
    /// </summary>
    public void Initialize(Vector3 moveDirection, float projectileSpeed, float projectileDamage, LayerMask targetLayers, float returnDelay)
    {
        this.direction = moveDirection;
        this.speed = projectileSpeed;
        this.damage = projectileDamage;
        this.targetLayer = targetLayers;
        this.autoReturnDelay = returnDelay;
        this.isInitialized = true;

        // Use the pool's auto-return mechanism instead of Destroy
        if (pool != null)
        {
            pool.AutoReturnProjectile(gameObject, autoReturnDelay);
        }
        else
        {
            Destroy(gameObject, autoReturnDelay);
        }
    }

    void Update()
    {
        if (!isInitialized) return;

        transform.position += direction * speed * Time.deltaTime;

        CheckCollisions();
    }

    void CheckCollisions()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, speed * Time.deltaTime, targetLayer))
        {
            OnHit(hit.collider, hit.point);
        }
    }

    void OnHit(Collider hitCollider, Vector3 hitPoint)
    {
        // Damage the target
        PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        // Play effects
        if (hitSound != null && audioSource != null)
            audioSource.PlayOneShot(hitSound);

        if (hitEffect != null)
            Instantiate(hitEffect, hitPoint, Quaternion.identity);

        // Return to pool instead of destroying
        isInitialized = false;
        if (pool != null)
        {
            pool.ReturnProjectile(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if we hit the target layer
        if (isInitialized && ((1 << other.gameObject.layer) & targetLayer) != 0)
        {
            OnHit(other, transform.position);
        }
    }

    /// <summary>
    /// Resets the projectile's state so it can be reused by the object pool.
    /// </summary>
    public void Reset()
    {
        isInitialized = false;
        // The pool handles resetting Rigidbody and TrailRenderer.
    }
}