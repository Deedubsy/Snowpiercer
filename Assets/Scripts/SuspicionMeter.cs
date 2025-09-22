using UnityEngine;
using System;

public class SuspicionMeter : MonoBehaviour
{
    [Header("Suspicion Settings")]
    [SerializeField] private float maxSuspicion = 100f;
    [SerializeField] private float currentSuspicion = 0f;
    [SerializeField] private float suspicionDecayRate = 5f; // Per second when not suspicious
    [SerializeField] private float suspicionCooldownTime = 10f; // Time before decay starts
    
    [Header("Suspicion Triggers")]
    [SerializeField] private float lurkingSuspicionRate = 10f; // Per second when lurking
    [SerializeField] private float witnessBloodDrainSuspicion = 50f; // Instant when witnessing
    [SerializeField] private float loudNoiseSuspicion = 20f; // Per loud noise event
    [SerializeField] private float runningNearbySuspicion = 15f; // When player sprints nearby
    
    [Header("Detection Settings")]
    [SerializeField] private float lurkingDetectionRadius = 5f;
    [SerializeField] private float lurkingTimeThreshold = 3f; // Seconds before considered lurking
    [SerializeField] private float witnessRange = 10f;
    
    [Header("Alert Behavior")]
    [SerializeField] private bool willRingBell = true; // Whether this NPC will ring bells
    [SerializeField] private float bellSearchRadius = 50f;
    
    private float timeSinceLastSuspiciousEvent = 0f;
    private float playerNearbyTime = 0f;
    private bool isAtMaxSuspicion = false;
    private bool isSearchingForBell = false;
    private Transform bellTarget = null;
    
    // References
    private Citizen citizen;
    private GuardAI guard;
    private Transform player;
    private VampireStats vampireStats;
    
    // Events
    public event Action<float> OnSuspicionChanged;
    public event Action OnMaxSuspicionReached;
    public event Action OnSuspicionCleared;
    
    private void Start()
    {
        citizen = GetComponent<Citizen>();
        guard = GetComponent<GuardAI>();
        
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            vampireStats = playerGO.GetComponent<VampireStats>();
        }
    }
    
    private void Update()
    {
        if (player == null) return;
        
        // Update suspicion based on player behavior
        CheckForLurking();
        
        // Decay suspicion over time
        UpdateSuspicionDecay();
        
        // Handle max suspicion behavior
        if (currentSuspicion >= maxSuspicion && !isAtMaxSuspicion)
        {
            OnReachMaxSuspicion();
        }
        
        // Search for bell if at max suspicion
        if (isSearchingForBell && bellTarget != null)
        {
            MoveTowardsBell();
        }
    }
    
    private void CheckForLurking()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= lurkingDetectionRadius)
        {
            playerNearbyTime += Time.deltaTime;
            
            if (playerNearbyTime >= lurkingTimeThreshold)
            {
                // Apply lurking suspicion with disguise modifier
                float suspicionRate = lurkingSuspicionRate;
                if (vampireStats != null && vampireStats.IsDisguised)
                {
                    suspicionRate *= vampireStats.disguiseSuspicionModifier;
                }
                
                AddSuspicion(suspicionRate * Time.deltaTime, "Lurking nearby");
            }
        }
        else
        {
            playerNearbyTime = 0f;
        }
    }
    
    private void UpdateSuspicionDecay()
    {
        timeSinceLastSuspiciousEvent += Time.deltaTime;
        
        if (timeSinceLastSuspiciousEvent >= suspicionCooldownTime && currentSuspicion > 0)
        {
            currentSuspicion = Mathf.Max(0, currentSuspicion - suspicionDecayRate * Time.deltaTime);
            OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);
            
            if (currentSuspicion == 0 && isAtMaxSuspicion)
            {
                isAtMaxSuspicion = false;
                isSearchingForBell = false;
                bellTarget = null;
                OnSuspicionCleared?.Invoke();
            }
        }
    }
    
    public void AddSuspicion(float amount, string reason = "")
    {
        float previousSuspicion = currentSuspicion;
        currentSuspicion = Mathf.Clamp(currentSuspicion + amount, 0, maxSuspicion);
        timeSinceLastSuspiciousEvent = 0f;
        
        if (currentSuspicion != previousSuspicion)
        {
            OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);
            
            if (!string.IsNullOrEmpty(reason))
            {
                GameLogger.Log(LogCategory.AI, $"{gameObject.name} suspicion increased: {reason} (+{amount})", this);
            }
        }
    }
    
    public void OnWitnessBloodDrain()
    {
        AddSuspicion(witnessBloodDrainSuspicion, "Witnessed blood drain");
    }
    
    public void OnHearLoudNoise(float noiseIntensity)
    {
        float suspicionAmount = loudNoiseSuspicion * noiseIntensity;
        AddSuspicion(suspicionAmount, "Heard loud noise");
    }
    
    public void OnPlayerSprintNearby()
    {
        AddSuspicion(runningNearbySuspicion, "Player sprinting nearby");
    }
    
    private void OnReachMaxSuspicion()
    {
        isAtMaxSuspicion = true;
        OnMaxSuspicionReached?.Invoke();
        
        if (willRingBell)
        {
            FindNearestBell();
        }
        
        // Alert nearby NPCs
        AlertNearbyNPCs();
    }
    
    private void FindNearestBell()
    {
        BellTower[] bells = FindObjectsOfType<BellTower>();
        float nearestDistance = float.MaxValue;
        BellTower nearestBell = null;
        
        foreach (var bell in bells)
        {
            if (!bell.IsSabotaged)
            {
                float distance = Vector3.Distance(transform.position, bell.transform.position);
                if (distance < nearestDistance && distance <= bellSearchRadius)
                {
                    nearestDistance = distance;
                    nearestBell = bell;
                }
            }
        }
        
        if (nearestBell != null)
        {
            bellTarget = nearestBell.transform;
            isSearchingForBell = true;
            
            // Notify the AI to move towards bell
            if (citizen != null)
            {
                citizen.SetOverrideDestination(bellTarget.position);
            }
            else if (guard != null)
            {
                guard.SetOverrideDestination(bellTarget.position);
            }
        }
    }
    
    private void MoveTowardsBell()
    {
        if (Vector3.Distance(transform.position, bellTarget.position) < 2f)
        {
            BellTower bell = bellTarget.GetComponent<BellTower>();
            if (bell != null && !bell.IsSabotaged)
            {
                bell.RingBell(this);
                isSearchingForBell = false;
                bellTarget = null;
                
                // Reset suspicion after ringing bell
                currentSuspicion = maxSuspicion * 0.5f; // Keep some suspicion
                OnSuspicionChanged?.Invoke(currentSuspicion / maxSuspicion);
            }
        }
    }
    
    private void AlertNearbyNPCs()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 15f);
        
        foreach (var collider in nearbyColliders)
        {
            if (collider.gameObject != gameObject)
            {
                SuspicionMeter otherMeter = collider.GetComponent<SuspicionMeter>();
                if (otherMeter != null)
                {
                    otherMeter.AddSuspicion(30f, "Alerted by nearby NPC");
                }
            }
        }
    }
    
    public void ResetSuspicion()
    {
        currentSuspicion = 0f;
        timeSinceLastSuspiciousEvent = suspicionCooldownTime;
        isAtMaxSuspicion = false;
        isSearchingForBell = false;
        bellTarget = null;
        OnSuspicionChanged?.Invoke(0f);
    }
    
    // Properties
    public float SuspicionLevel => currentSuspicion;
    public float SuspicionPercentage => currentSuspicion / maxSuspicion;
    public bool IsMaxSuspicion => isAtMaxSuspicion;
    public bool IsSearchingForBell => isSearchingForBell;
    
    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, lurkingDetectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, witnessRange);
        
        if (bellTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, bellTarget.position);
        }
    }
}