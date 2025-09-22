using System.Collections;
using UnityEngine;

public class AreaEffect : MonoBehaviour
{
    [Header("Area Effect Settings")]
    public float radius = 5f;
    public float damage = 5f;
    public float duration = 3f;
    public LayerMask targetLayer;

    [Header("Effects")]
    public GameObject explosionEffect;
    public GameObject areaEffectPrefab;
    public AudioClip explosionSound;
    public AudioClip areaEffectSound;

    private bool hasExploded = false;
    private AudioSource audioSource;

    private bool isActive = false;
    private float timer;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Automatically start the effect
        //Initialize(effectRadius, damagePerSecond, duration, targetLayer);
    }

    public void Initialize(float radius, float dps, float effectDuration, LayerMask layers, bool startActive = true)
    {
        this.radius = radius;
        this.damage = dps;
        this.duration = effectDuration;
        this.targetLayer = layers;
        this.timer = 0f;
        this.isActive = startActive;

        // Visual scaling
        transform.localScale = new Vector3(radius * 2, 0.1f, radius * 2);
    }

    void Update()
    {
        // Deactivate after duration
        if (timer >= duration)
        {
            isActive = false;
            // Optionally, return to an object pool
            gameObject.SetActive(false);
        }
    }

    public void Reset()
    {
        isActive = false;
        timer = 0f;
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // Play explosion sound
        if (explosionSound != null)
            audioSource.PlayOneShot(explosionSound);

        // Create explosion effect
        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);

        // Damage all targets in radius
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, targetLayer);
        foreach (Collider hitCollider in hitColliders)
        {
            PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($"Area effect hit player for {damage} damage!");
            }
        }

        // Create persistent area effect
        if (areaEffectPrefab != null)
        {
            GameObject areaEffect = Instantiate(areaEffectPrefab, transform.position, Quaternion.identity);
            Destroy(areaEffect, duration);
        }

        // Start area damage over time
        StartCoroutine(AreaDamageOverTime());

        // Destroy the projectile
        Destroy(gameObject, 0.1f);
    }

    IEnumerator AreaDamageOverTime()
    {
        float elapsed = 0f;
        float damageInterval = 0.5f;
        float lastDamageTime = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Damage every damageInterval seconds
            if (elapsed - lastDamageTime >= damageInterval)
            {
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, targetLayer);
                foreach (Collider hitCollider in hitColliders)
                {
                    PlayerHealth playerHealth = hitCollider.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(damage * 0.2f); // Reduced damage over time
                    }
                }

                lastDamageTime = elapsed;
            }

            yield return null;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw the area effect radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}