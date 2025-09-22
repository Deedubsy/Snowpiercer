using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class AudioTriggerCondition
{
    public enum TriggerType
    {
        OnEnter,
        OnExit,
        OnStay,
        OnInteract,
        OnProximity,
        OnTimeOfDay,
        OnPlayerHealth,
        OnPlayerStealth
    }

    public TriggerType triggerType = TriggerType.OnEnter;
    public string soundName = "";
    public bool playOnce = false;
    public bool stopOnExit = false;
    public float minDistance = 0f;
    public float maxDistance = 10f;
    public float cooldown = 0f;
    public bool requirePlayer = true;
    public bool requireVampire = false;
    public bool requireGuard = false;
    public bool requireCitizen = false;

    [Header("Time Conditions")]
    public bool useTimeCondition = false;
    public float startTime = 0f; // 0-24 hour format
    public float endTime = 24f;

    [Header("Health Conditions")]
    public bool useHealthCondition = false;
    public float minHealth = 0f;
    public float maxHealth = 100f;

    [Header("Stealth Conditions")]
    public bool useStealthCondition = false;
    public float minStealth = 0f;
    public float maxStealth = 1f;

    [Header("Events")]
    public UnityEvent onTriggerActivated;
    public UnityEvent onTriggerDeactivated;
}

public class AudioTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    public List<AudioTriggerCondition> triggerConditions = new List<AudioTriggerCondition>();
    public bool useCollider = true;
    public bool useProximity = false;
    public float proximityRadius = 5f;

    [Header("Audio Settings")]
    public bool useSpatialAudio = true;
    public float spatialBlend = 1f;
    public float maxDistance = 10f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;

    [Header("Debug")]
    public bool debugMode = false;
    public bool showTriggerArea = false;

    // Private variables
    private Collider triggerCollider;
    private Dictionary<AudioTriggerCondition, float> lastTriggerTimes = new Dictionary<AudioTriggerCondition, float>();
    private Dictionary<AudioTriggerCondition, bool> hasTriggered = new Dictionary<AudioTriggerCondition, bool>();
    private Transform playerTransform;
    private bool isPlayerInRange = false;

    void Start()
    {
        InitializeTrigger();
    }

    void InitializeTrigger()
    {
        // Get or create collider
        if (useCollider)
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
                ((SphereCollider)triggerCollider).radius = proximityRadius;
            }
            triggerCollider.isTrigger = true;
        }

        // Initialize trigger tracking
        foreach (var condition in triggerConditions)
        {
            lastTriggerTimes[condition] = -condition.cooldown;
            hasTriggered[condition] = false;
        }

        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (debugMode)
        {
            Debug.Log($"AudioTrigger initialized with {triggerConditions.Count} conditions");
        }
    }

    void Update()
    {
        if (useProximity && playerTransform != null)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool wasInRange = isPlayerInRange;
            isPlayerInRange = distance <= proximityRadius;

            // Handle proximity triggers
            if (isPlayerInRange && !wasInRange)
            {
                HandleProximityTriggers(true);
            }
            else if (!isPlayerInRange && wasInRange)
            {
                HandleProximityTriggers(false);
            }
        }

        // Handle time-based triggers
        HandleTimeBasedTriggers();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useCollider) return;

        if (IsValidTriggerTarget(other.gameObject))
        {
            HandleTriggerConditions(AudioTriggerCondition.TriggerType.OnEnter, other.gameObject);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!useCollider) return;

        if (IsValidTriggerTarget(other.gameObject))
        {
            HandleTriggerConditions(AudioTriggerCondition.TriggerType.OnExit, other.gameObject);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!useCollider) return;

        if (IsValidTriggerTarget(other.gameObject))
        {
            HandleTriggerConditions(AudioTriggerCondition.TriggerType.OnStay, other.gameObject);
        }
    }

    bool IsValidTriggerTarget(GameObject target)
    {
        if (target == null) return false;

        // Check if it's the player
        bool isPlayer = target.CompareTag("Player");
        if (isPlayer && !target.GetComponent<PlayerController>()) return false;

        // Check entity type requirements
        foreach (var condition in triggerConditions)
        {
            if (condition.requirePlayer && !isPlayer) return false;
            if (condition.requireVampire && !target.GetComponent<VampireStats>()) return false;
            if (condition.requireGuard && !target.GetComponent<GuardAI>()) return false;
            if (condition.requireCitizen && !target.GetComponent<Citizen>()) return false;
        }

        return true;
    }

    void HandleTriggerConditions(AudioTriggerCondition.TriggerType triggerType, GameObject target)
    {
        foreach (var condition in triggerConditions)
        {
            if (condition.triggerType == triggerType)
            {
                if (CanTrigger(condition, target))
                {
                    ActivateTrigger(condition, target);
                }
            }
        }
    }

    void HandleProximityTriggers(bool entered)
    {
        foreach (var condition in triggerConditions)
        {
            if (condition.triggerType == AudioTriggerCondition.TriggerType.OnProximity)
            {
                if (entered && CanTrigger(condition, playerTransform.gameObject))
                {
                    ActivateTrigger(condition, playerTransform.gameObject);
                }
                else if (!entered && condition.stopOnExit)
                {
                    DeactivateTrigger(condition);
                }
            }
        }
    }

    void HandleTimeBasedTriggers()
    {
        if (AudioManager.Instance == null) return;

        // Get current time (assuming 24-hour cycle)
        float currentTime = (Time.time % 86400f) / 3600f; // Convert seconds to hours

        foreach (var condition in triggerConditions)
        {
            if (condition.triggerType == AudioTriggerCondition.TriggerType.OnTimeOfDay && condition.useTimeCondition)
            {
                bool isInTimeRange = IsInTimeRange(currentTime, condition.startTime, condition.endTime);

                if (isInTimeRange && CanTrigger(condition, playerTransform?.gameObject))
                {
                    ActivateTrigger(condition, playerTransform?.gameObject);
                }
                else if (!isInTimeRange && condition.stopOnExit)
                {
                    DeactivateTrigger(condition);
                }
            }
        }
    }

    bool IsInTimeRange(float currentTime, float startTime, float endTime)
    {
        if (startTime <= endTime)
        {
            return currentTime >= startTime && currentTime <= endTime;
        }
        else
        {
            // Handle time ranges that cross midnight
            return currentTime >= startTime || currentTime <= endTime;
        }
    }

    bool CanTrigger(AudioTriggerCondition condition, GameObject target)
    {
        // Check cooldown
        if (Time.time - lastTriggerTimes[condition] < condition.cooldown)
        {
            return false;
        }

        // Check if already triggered (for playOnce)
        if (condition.playOnce && hasTriggered[condition])
        {
            return false;
        }

        // Check distance
        if (target != null)
        {
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance < condition.minDistance || distance > condition.maxDistance)
            {
                return false;
            }
        }

        // Check health condition
        if (condition.useHealthCondition && target != null)
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                float healthPercent = playerHealth.currentHealth / playerHealth.maxHealth * 100f;
                if (healthPercent < condition.minHealth || healthPercent > condition.maxHealth)
                {
                    return false;
                }
            }
        }

        // Check stealth condition
        if (condition.useStealthCondition && target != null)
        {
            // Assuming there's a stealth component or system
            // This would need to be implemented based on your stealth system
            float stealthLevel = 0f; // Get from stealth system
            if (stealthLevel < condition.minStealth || stealthLevel > condition.maxStealth)
            {
                return false;
            }
        }

        return true;
    }

    void ActivateTrigger(AudioTriggerCondition condition, GameObject target)
    {
        // Play sound
        if (!string.IsNullOrEmpty(condition.soundName))
        {
            Vector3 soundPosition = useSpatialAudio ? transform.position : target?.transform.position ?? transform.position;
            AudioSource source = AudioManager.Instance.PlaySoundEffect(condition.soundName, soundPosition);

            if (source != null && useSpatialAudio)
            {
                source.spatialBlend = spatialBlend;
                source.maxDistance = maxDistance;
                source.rolloffMode = rolloffMode;
            }
        }

        // Update tracking
        lastTriggerTimes[condition] = Time.time;
        hasTriggered[condition] = true;

        // Invoke events
        condition.onTriggerActivated?.Invoke();

        if (debugMode)
        {
            Debug.Log($"AudioTrigger activated: {condition.soundName} for {target?.name ?? "unknown"}");
        }
    }

    void DeactivateTrigger(AudioTriggerCondition condition)
    {
        // Stop sound if needed
        if (!string.IsNullOrEmpty(condition.soundName))
        {
            // This would need to be implemented based on how you want to stop sounds
            // AudioManager.Instance.StopSoundEffect(condition.soundName);
        }

        // Invoke events
        condition.onTriggerDeactivated?.Invoke();

        if (debugMode)
        {
            Debug.Log($"AudioTrigger deactivated: {condition.soundName}");
        }
    }

    // Public methods for external triggering
    public void TriggerManually(string conditionName)
    {
        foreach (var condition in triggerConditions)
        {
            if (condition.soundName == conditionName)
            {
                ActivateTrigger(condition, playerTransform?.gameObject);
                break;
            }
        }
    }

    public void ResetTrigger(string conditionName)
    {
        foreach (var condition in triggerConditions)
        {
            if (condition.soundName == conditionName)
            {
                hasTriggered[condition] = false;
                break;
            }
        }
    }

    public void ResetAllTriggers()
    {
        foreach (var condition in triggerConditions)
        {
            hasTriggered[condition] = false;
        }
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (showTriggerArea)
        {
            Gizmos.color = isPlayerInRange ? Color.green : Color.yellow;

            if (useCollider && triggerCollider != null)
            {
                if (triggerCollider is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(transform.position, sphere.radius);
                }
                else if (triggerCollider is BoxCollider box)
                {
                    Gizmos.DrawWireCube(transform.position + box.center, box.size);
                }
            }
            else if (useProximity)
            {
                Gizmos.DrawWireSphere(transform.position, proximityRadius);
            }
        }
    }

    // Context menu methods
    [ContextMenu("Test Trigger")]
    public void TestTrigger()
    {
        if (triggerConditions.Count > 0)
        {
            ActivateTrigger(triggerConditions[0], playerTransform?.gameObject);
        }
    }

    [ContextMenu("Reset All Triggers")]
    public void ResetAllTriggersFromContext()
    {
        ResetAllTriggers();
    }
}