using UnityEngine;

public class SpikeTrap : InteractiveObject
{
    public float damage = 25f;
    public bool canDisarm = false;
    public bool isArmed = true;
    public string disarmPrompt = "Press E to disarm trap";

    private GuardAlertnessManager alertnessManager;

    void Start()
    {
        alertnessManager = GuardAlertnessManager.instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isArmed) return;
        // Damage player
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(damage);
            
            // Notify alertness manager
            if (alertnessManager != null)
                alertnessManager.OnTrapTriggered();
            
            return;
        }
        // Optionally, damage NPCs
        Citizen citizen = other.GetComponent<Citizen>();
        if (citizen != null)
        {
            Debug.Log($"Citizen hit by spike trap for {damage} damage!");
            // citizen.TakeDamage(damage);
            return;
        }
        GuardAI guard = other.GetComponent<GuardAI>();
        if (guard != null)
        {
            Debug.Log($"Guard hit by spike trap for {damage} damage!");
            // guard.TakeDamage(damage);
            return;
        }
    }

    public override void Interact(PlayerController player)
    {
        if (canDisarm && isArmed)
        {
            isArmed = false;
            promptText = "Trap disarmed";
            Debug.Log("Trap disarmed!");
            // Optionally, play animation or sound
        }
    }

    public override void OnFocus(PlayerController player)
    {
        if (canDisarm && isArmed)
            promptText = disarmPrompt;
        else if (!isArmed)
            promptText = "Trap disarmed";
        else
            promptText = "";
    }
} 