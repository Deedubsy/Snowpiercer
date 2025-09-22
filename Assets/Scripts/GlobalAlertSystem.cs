using UnityEngine;
using System;
using System.Collections.Generic;

public class GlobalAlertSystem : MonoBehaviour
{
    public static GlobalAlertSystem Instance { get; private set; }
    
    public enum AlertState
    {
        Calm,     // Normal state
        Yellow,   // Suspicious activity reported
        Orange,   // Active threat confirmed
        Red       // Full lockdown
    }
    
    [Header("Alert State Configuration")]
    [SerializeField] private AlertState currentAlertState = AlertState.Calm;
    [SerializeField] private float alertDecayTime = 300f; // 5 minutes to decay one level
    [SerializeField] private float timeInCurrentAlert = 0f;
    
    [Header("Alert State Effects")]
    [SerializeField] private AlertStateConfig[] alertConfigs = new AlertStateConfig[]
    {
        new AlertStateConfig { state = AlertState.Calm, guardSpeedMultiplier = 1f, detectionRangeMultiplier = 1f, audioSensitivityMultiplier = 1f },
        new AlertStateConfig { state = AlertState.Yellow, guardSpeedMultiplier = 2f, detectionRangeMultiplier = 1.2f, audioSensitivityMultiplier = 1.3f },
        new AlertStateConfig { state = AlertState.Orange, guardSpeedMultiplier = 2.5f, detectionRangeMultiplier = 1.5f, audioSensitivityMultiplier = 1.5f },
        new AlertStateConfig { state = AlertState.Red, guardSpeedMultiplier = 3f, detectionRangeMultiplier = 2f, audioSensitivityMultiplier = 2f }
    };
    
    [Header("Spawn Configuration")]
    [SerializeField] private GameObject searchDogPrefab;
    [SerializeField] private GameObject mountedPatrolPrefab;
    [SerializeField] private GameObject eliteGuardPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private int dogsPerOrangeAlert = 2;
    [SerializeField] private int eliteGuardsPerRedAlert = 4;
    
    [Header("Player Tracking")]
    [SerializeField] private Vector3 lastKnownPlayerPosition;
    [SerializeField] private float positionUpdateInterval = 0.5f;
    private float lastPositionUpdateTime = 0f;
    
    [Header("Gate Control")]
    [SerializeField] private List<GameObject> cityGates = new List<GameObject>();
    
    // Events
    public event Action<AlertState, AlertState> OnAlertStateChanged;
    public event Action<AlertState> OnAlertStateAdvanced;
    public event Action OnAlertDecayed;
    
    // Active spawned units
    private List<GameObject> spawnedAlertUnits = new List<GameObject>();
    
    // References
    private Transform player;
    private GuardAlertnessManager guardAlertnessManager;
    
    [System.Serializable]
    public class AlertStateConfig
    {
        public AlertState state;
        public float guardSpeedMultiplier;
        public float detectionRangeMultiplier;
        public float audioSensitivityMultiplier;
        public bool spawnDogs;
        public bool spawnEliteGuards;
        public bool lockGates;
        public bool civiliansFlee;
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
        }
        
        guardAlertnessManager = GuardAlertnessManager.Instance;
        
        // Find all city gates
        GameObject[] gates = GameObject.FindGameObjectsWithTag("CityGate");
        cityGates.AddRange(gates);
        
        // Apply initial alert state
        ApplyAlertStateEffects();
    }
    
    private void Update()
    {
        // Update player position tracking
        if (player != null && Time.time - lastPositionUpdateTime > positionUpdateInterval)
        {
            UpdateLastKnownPlayerPosition();
        }
        
        // Handle alert decay
        UpdateAlertDecay();
    }
    
    private void UpdateLastKnownPlayerPosition()
    {
        // Only update if player is visible to any guard or citizen
        bool isVisible = false;
        
        // Check guards
        GuardAI[] guards = FindObjectsOfType<GuardAI>();
        foreach (var guard in guards)
        {
            if (guard.GetCurrentState() == GuardAI.GuardState.Chase || 
                guard.GetCurrentState() == GuardAI.GuardState.Attack)
            {
                isVisible = true;
                break;
            }
        }
        
        // Check citizens with high suspicion
        if (!isVisible)
        {
            SuspicionMeter[] suspicionMeters = FindObjectsOfType<SuspicionMeter>();
            foreach (var meter in suspicionMeters)
            {
                if (meter.SuspicionPercentage > 0.8f)
                {
                    isVisible = true;
                    break;
                }
            }
        }
        
        if (isVisible)
        {
            lastKnownPlayerPosition = player.position;
            lastPositionUpdateTime = Time.time;
        }
    }
    
    private void UpdateAlertDecay()
    {
        if (currentAlertState == AlertState.Calm) return;
        
        timeInCurrentAlert += Time.deltaTime;
        
        if (timeInCurrentAlert >= alertDecayTime)
        {
            DecayAlertLevel();
        }
    }
    
    public void AdvanceAlertLevel()
    {
        if (currentAlertState == AlertState.Red) return;
        
        AlertState previousState = currentAlertState;
        currentAlertState = (AlertState)((int)currentAlertState + 1);
        timeInCurrentAlert = 0f;
        
        OnAlertStateChanged?.Invoke(previousState, currentAlertState);
        OnAlertStateAdvanced?.Invoke(currentAlertState);
        
        ApplyAlertStateEffects();
        
        GameLogger.Log(LogCategory.Gameplay, $"Alert level advanced to: {currentAlertState}", this);
    }
    
    private void DecayAlertLevel()
    {
        if (currentAlertState == AlertState.Calm) return;
        
        AlertState previousState = currentAlertState;
        currentAlertState = (AlertState)((int)currentAlertState - 1);
        timeInCurrentAlert = 0f;
        
        OnAlertStateChanged?.Invoke(previousState, currentAlertState);
        OnAlertDecayed?.Invoke();
        
        ApplyAlertStateEffects();
        
        GameLogger.Log(LogCategory.Gameplay, $"Alert level decayed to: {currentAlertState}", this);
    }
    
    private void ApplyAlertStateEffects()
    {
        AlertStateConfig config = GetConfigForState(currentAlertState);
        if (config == null) return;
        
        // Apply guard behavior modifications
        ApplyGuardModifications(config);
        
        // Handle spawning
        HandleAlertSpawning(config);
        
        // Handle gates
        HandleGateLocking(config);
        
        // Handle civilian behavior
        HandleCivilianBehavior(config);
        
        // Update guard alertness manager
        if (guardAlertnessManager != null)
        {
            switch (currentAlertState)
            {
                case AlertState.Yellow:
                    guardAlertnessManager.SetAlertnessLevel(GuardAlertnessLevel.Suspicious);
                    break;
                case AlertState.Orange:
                    guardAlertnessManager.SetAlertnessLevel(GuardAlertnessLevel.Alert);
                    break;
                case AlertState.Red:
                    guardAlertnessManager.SetAlertnessLevel(GuardAlertnessLevel.Panic);
                    break;
                default:
                    guardAlertnessManager.SetAlertnessLevel(GuardAlertnessLevel.Normal);
                    break;
            }
        }
    }
    
    private void ApplyGuardModifications(AlertStateConfig config)
    {
        GuardAI[] guards = FindObjectsOfType<GuardAI>();
        
        foreach (var guard in guards)
        {
            // Apply speed multiplier
            guard.SetSpeedMultiplier(config.guardSpeedMultiplier);
            
            // Apply detection range multiplier
            guard.SetDetectionRangeMultiplier(config.detectionRangeMultiplier);
            
            // Apply audio sensitivity
            guard.SetAudioSensitivityMultiplier(config.audioSensitivityMultiplier);
            
            // Make guards more aggressive at higher alert levels
            if (currentAlertState >= AlertState.Orange)
            {
                guard.SetAggressiveMode(true);
            }
        }
    }
    
    private void HandleAlertSpawning(AlertStateConfig config)
    {
        // Clear previous spawns if going to lower alert
        if (currentAlertState < AlertState.Orange)
        {
            ClearSpawnedUnits();
            return;
        }
        
        // Spawn units based on alert level
        switch (currentAlertState)
        {
            case AlertState.Orange:
                SpawnSearchDogs();
                SpawnMountedPatrols();
                break;
            case AlertState.Red:
                SpawnEliteGuards();
                break;
        }
    }
    
    private void SpawnSearchDogs()
    {
        if (searchDogPrefab == null || spawnPoints.Length == 0) return;
        
        for (int i = 0; i < dogsPerOrangeAlert; i++)
        {
            Transform spawnPoint = GetRandomSpawnPoint();
            GameObject dog = Instantiate(searchDogPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedAlertUnits.Add(dog);
            
            // Set dog to track last known player position
            // Assuming dog has a component that can track positions
            var tracker = dog.GetComponent<AISearchBehavior>();
            if (tracker != null)
            {
                tracker.SetSearchTarget(lastKnownPlayerPosition);
            }
        }
    }
    
    private void SpawnMountedPatrols()
    {
        if (mountedPatrolPrefab == null) return;
        
        // Spawn mounted patrols at city gates
        foreach (var gate in cityGates)
        {
            if (gate != null)
            {
                Vector3 spawnPos = gate.transform.position + gate.transform.forward * 5f;
                GameObject patrol = Instantiate(mountedPatrolPrefab, spawnPos, gate.transform.rotation);
                spawnedAlertUnits.Add(patrol);
            }
        }
    }
    
    private void SpawnEliteGuards()
    {
        if (eliteGuardPrefab == null || spawnPoints.Length == 0) return;
        
        for (int i = 0; i < eliteGuardsPerRedAlert; i++)
        {
            Transform spawnPoint = GetRandomSpawnPoint();
            GameObject elite = Instantiate(eliteGuardPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedAlertUnits.Add(elite);
            
            // Elite guards actively hunt the player
            var guard = elite.GetComponent<GuardAI>();
            if (guard != null)
            {
                guard.SetHuntingMode(true);
            }
        }
    }
    
    private void HandleGateLocking(AlertStateConfig config)
    {
        bool shouldLock = currentAlertState >= AlertState.Orange;
        
        foreach (var gate in cityGates)
        {
            if (gate != null)
            {
                var cityGateTrigger = gate.GetComponent<CityGateTrigger>();
                if (cityGateTrigger != null)
                {
                    cityGateTrigger.SetLocked(shouldLock);
                }
                
                // Visual indicator
                var gateVisual = gate.GetComponent<Renderer>();
                if (gateVisual != null)
                {
                    gateVisual.material.color = shouldLock ? Color.red : Color.white;
                }
            }
        }
    }
    
    private void HandleCivilianBehavior(AlertStateConfig config)
    {
        bool shouldFlee = currentAlertState >= AlertState.Orange;
        
        Citizen[] citizens = FindObjectsOfType<Citizen>();
        foreach (var citizen in citizens)
        {
            if (shouldFlee)
            {
                citizen.FleeToSafety();
            }
        }
    }
    
    private void ClearSpawnedUnits()
    {
        foreach (var unit in spawnedAlertUnits)
        {
            if (unit != null)
            {
                Destroy(unit);
            }
        }
        spawnedAlertUnits.Clear();
    }
    
    private AlertStateConfig GetConfigForState(AlertState state)
    {
        foreach (var config in alertConfigs)
        {
            if (config.state == state)
                return config;
        }
        return null;
    }
    
    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Length == 0) return transform;
        return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
    }
    
    public void ForceAlertState(AlertState newState)
    {
        AlertState previousState = currentAlertState;
        currentAlertState = newState;
        timeInCurrentAlert = 0f;
        
        OnAlertStateChanged?.Invoke(previousState, currentAlertState);
        ApplyAlertStateEffects();
    }
    
    public void ResetAlertSystem()
    {
        ForceAlertState(AlertState.Calm);
        lastKnownPlayerPosition = Vector3.zero;
        ClearSpawnedUnits();
    }
    
    // Properties
    public AlertState CurrentAlertState => currentAlertState;
    public Vector3 GetLastKnownPlayerPosition() => lastKnownPlayerPosition;
    public float TimeInCurrentAlert => timeInCurrentAlert;
    public float TimeUntilDecay => alertDecayTime - timeInCurrentAlert;
    
    public AlertStateConfig GetCurrentConfig()
    {
        return GetConfigForState(currentAlertState);
    }
}