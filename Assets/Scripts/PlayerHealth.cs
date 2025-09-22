using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public Slider healthBar;
    public System.Action OnDeath;
    public bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth -= amount;
        
        if (amount > 0)
        {
            if (DynamicObjectiveSystem.Instance != null)
            {
                DynamicObjectiveSystem.Instance.OnPlayerDamaged();
                Debug.Log($"Notified objective system: player took {amount} damage");
            }
            else
            {
                Debug.LogWarning("DynamicObjectiveSystem not found - damage not tracked for objectives");
            }
        }
        
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
        UpdateHealthUI();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth += amount;
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
        UpdateHealthUI();
    }

    void Die()
    {
        isDead = true;
        Debug.Log("Player died!");
        OnDeath?.Invoke();
        
        // Trigger game over
        GameManager gameManager = GameManager.instance;
        if (gameManager != null)
        {
            gameManager.GameOver();
        }
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.value = currentHealth / maxHealth;
    }
} 