using UnityEngine;
using System.Collections.Generic;

public class WardSystem : MonoBehaviour
{
    public static WardSystem Instance { get; private set; }
    
    [Header("Ward Configuration")]
    [SerializeField] private List<Ward> wards = new List<Ward>();
    [SerializeField] private float wardTransitionTime = 2f;
    
    private Ward currentWard;
    private Ward previousWard;
    
    public event System.Action<Ward, Ward> OnWardChanged;
    
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
        // Find all wards in the scene
        Ward[] foundWards = FindObjectsOfType<Ward>();
        wards.AddRange(foundWards);
        
        // Determine initial ward based on player position
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            UpdateCurrentWard(player.transform.position);
        }
    }
    
    public void UpdateCurrentWard(Vector3 position)
    {
        Ward newWard = GetWardAtPosition(position);
        
        if (newWard != currentWard)
        {
            previousWard = currentWard;
            currentWard = newWard;
            
            OnWardChanged?.Invoke(previousWard, currentWard);
            
            if (currentWard != null)
            {
                GameLogger.Log(LogCategory.Gameplay, $"Entered ward: {currentWard.WardName}", this);
                currentWard.OnPlayerEnter();
            }
            
            if (previousWard != null)
            {
                previousWard.OnPlayerExit();
            }
        }
    }
    
    private Ward GetWardAtPosition(Vector3 position)
    {
        foreach (var ward in wards)
        {
            if (ward.IsPositionInWard(position))
            {
                return ward;
            }
        }
        return null;
    }
    
    public Ward GetCurrentWard()
    {
        return currentWard;
    }
    
    public List<Ward> GetAllWards()
    {
        return new List<Ward>(wards);
    }
    
    public Ward GetWardByName(string name)
    {
        foreach (var ward in wards)
        {
            if (ward.WardName == name)
                return ward;
        }
        return null;
    }
}

[System.Serializable]
public class Ward : MonoBehaviour
{
    [Header("Ward Identity")]
    [SerializeField] private string wardName = "District";
    [SerializeField] private Color wardColor = Color.white;
    
    [Header("Ward Boundaries")]
    [SerializeField] private Collider wardBoundary;
    [SerializeField] private List<WardGate> gates = new List<WardGate>();
    
    [Header("Ward Properties")]
    [SerializeField] private int securityLevel = 1; // 1-5, affects guard density
    [SerializeField] private bool hasChurch = false;
    [SerializeField] private bool hasBellTower = true;
    [SerializeField] private bool isResidential = true;
    
    [Header("Ward NPCs")]
    [SerializeField] private int maxCitizens = 20;
    [SerializeField] private int maxGuards = 5;
    [SerializeField] private CitizenRarity dominantCitizenType = CitizenRarity.Peasant;
    
    [Header("Special Features")]
    [SerializeField] private Transform[] hidingSpots;
    [SerializeField] private Transform[] vantagePoints;
    [SerializeField] private GameObject[] wardLights;
    
    private List<Citizen> wardCitizens = new List<Citizen>();
    private List<GuardAI> wardGuards = new List<GuardAI>();
    private BellTower wardBellTower;
    
    private void Start()
    {
        // Set up ward boundary if not assigned
        if (wardBoundary == null)
        {
            wardBoundary = GetComponent<Collider>();
        }
        
        // Find bell tower in ward
        wardBellTower = GetComponentInChildren<BellTower>();
        
        // Find all gates
        WardGate[] foundGates = GetComponentsInChildren<WardGate>();
        gates.AddRange(foundGates);
        
        // Register NPCs in ward
        RegisterNPCs();
    }
    
    private void RegisterNPCs()
    {
        // Find all citizens in ward bounds
        Citizen[] allCitizens = FindObjectsOfType<Citizen>();
        foreach (var citizen in allCitizens)
        {
            if (IsPositionInWard(citizen.transform.position))
            {
                wardCitizens.Add(citizen);
                citizen.SetHomeWard(this);
            }
        }
        
        // Find all guards in ward bounds
        GuardAI[] allGuards = FindObjectsOfType<GuardAI>();
        foreach (var guard in allGuards)
        {
            if (IsPositionInWard(guard.transform.position))
            {
                wardGuards.Add(guard);
                guard.SetPatrolWard(this);
            }
        }
    }
    
    public bool IsPositionInWard(Vector3 position)
    {
        if (wardBoundary != null)
        {
            return wardBoundary.bounds.Contains(position);
        }
        return false;
    }
    
    public void OnPlayerEnter()
    {
        // Adjust lighting based on time and security
        AdjustWardLighting(true);
        
        // Alert guards if security is high
        if (securityLevel >= 3)
        {
            foreach (var guard in wardGuards)
            {
                guard.IncreaseAlertness();
            }
        }
    }
    
    public void OnPlayerExit()
    {
        AdjustWardLighting(false);
    }
    
    private void AdjustWardLighting(bool playerPresent)
    {
        if (wardLights == null) return;
        
        foreach (var light in wardLights)
        {
            Light lightComponent = light.GetComponent<Light>();
            if (lightComponent != null)
            {
                // Dim lights when player is present for stealth
                float targetIntensity = playerPresent ? 0.5f : 1f;
                lightComponent.intensity = targetIntensity;
            }
        }
    }
    
    public void LockDownWard()
    {
        // Lock all gates
        foreach (var gate in gates)
        {
            gate.SetLocked(true);
        }
        
        // Alert all guards
        foreach (var guard in wardGuards)
        {
            guard.SetAggressiveMode(true);
        }
        
        // Citizens flee indoors
        foreach (var citizen in wardCitizens)
        {
            citizen.FleeToSafety();
        }
        
        GameLogger.Log(LogCategory.Gameplay, $"Ward {wardName} is now on lockdown!", this);
    }
    
    public void ReleaseWardLockdown()
    {
        foreach (var gate in gates)
        {
            gate.SetLocked(false);
        }
        
        GameLogger.Log(LogCategory.Gameplay, $"Ward {wardName} lockdown lifted", this);
    }
    
    // Properties
    public string WardName => wardName;
    public Color WardColor => wardColor;
    public int SecurityLevel => securityLevel;
    public bool HasChurch => hasChurch;
    public bool HasBellTower => hasBellTower;
    public List<WardGate> Gates => new List<WardGate>(gates);
    public List<Transform> HidingSpots => new List<Transform>(hidingSpots);
    public BellTower BellTower => wardBellTower;
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = wardColor;
        
        if (wardBoundary != null)
        {
            Gizmos.DrawWireCube(wardBoundary.bounds.center, wardBoundary.bounds.size);
        }
        
        // Draw hiding spots
        Gizmos.color = Color.blue;
        foreach (var spot in hidingSpots)
        {
            if (spot != null)
                Gizmos.DrawWireSphere(spot.position, 1f);
        }
        
        // Draw vantage points
        Gizmos.color = Color.yellow;
        foreach (var point in vantagePoints)
        {
            if (point != null)
                Gizmos.DrawWireCube(point.position, Vector3.one);
        }
    }
}

public class WardGate : InteractiveObject
{
    [Header("Gate Settings")]
    [SerializeField] private Ward connectedWard;
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private bool canBeLockpicked = true;
    [SerializeField] private bool canBeBribed = true;
    [SerializeField] private int bribeCost = 50;
    [SerializeField] private float lockpickTime = 5f;
    
    [Header("Gate State")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private bool isOpen = false;
    
    [Header("Visual Elements")]
    [SerializeField] private GameObject gateDoor;
    [SerializeField] private Light gateLight;
    [SerializeField] private Color lockedColor = Color.red;
    [SerializeField] private Color unlockedColor = Color.green;
    
    private bool isBeingLockpicked = false;
    private float lockpickProgress = 0f;
    
    public override void Start()
    {
        base.Start();
        UpdateGateVisuals();
        UpdateInteractionPrompt();
    }
    
    public override void Interact(PlayerController player)
    {
        if (!isLocked)
        {
            ToggleGate();
        }
        else
        {
            ShowLockedOptions();
        }
    }
    
    private void ToggleGate()
    {
        isOpen = !isOpen;
        
        if (gateDoor != null)
        {
            // Animate gate opening/closing
            Vector3 targetRotation = isOpen ? new Vector3(0, 90, 0) : Vector3.zero;
            gateDoor.transform.rotation = Quaternion.Euler(targetRotation);
        }
        
        UpdateInteractionPrompt();
    }
    
    private void ShowLockedOptions()
    {
        string options = "Gate Locked - ";
        
        if (canBeLockpicked)
            options += "Hold F to Lockpick / ";
        if (canBeBribed)
            options += $"Press B to Bribe ({bribeCost} blood) / ";
        
        interactionPrompt = options;
    }
    
    public void StartLockpicking()
    {
        if (!canBeLockpicked || !isLocked) return;
        
        VampireStats vampireStats = VampireStats.instance;
        if (vampireStats == null) return;
        
        isBeingLockpicked = true;
        lockpickProgress = 0f;
        StartCoroutine(LockpickProcess());
    }
    
    private System.Collections.IEnumerator LockpickProcess()
    {
        VampireStats vampireStats = VampireStats.instance;
        float actualLockpickTime = lockpickTime / vampireStats.SabotageSpeed;
        
        while (lockpickProgress < actualLockpickTime)
        {
            // Check if player is still in range
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || Vector3.Distance(transform.position, player.transform.position) > interactionRange)
            {
                CancelLockpicking();
                yield break;
            }
            
            lockpickProgress += Time.deltaTime;
            
            // Update UI
            float progress = lockpickProgress / actualLockpickTime;
            interactionPrompt = $"Lockpicking... {(progress * 100):F0}%";
            
            // Make small noise that might alert nearby guards
            if (Random.value < 0.01f) // 1% chance per frame
            {
                NoiseManager.MakeNoise(transform.position, 5f, 0.3f);
            }
            
            yield return null;
        }
        
        CompleteLockpicking();
    }
    
    private void CancelLockpicking()
    {
        isBeingLockpicked = false;
        lockpickProgress = 0f;
        UpdateInteractionPrompt();
    }
    
    private void CompleteLockpicking()
    {
        isLocked = false;
        isBeingLockpicked = false;
        UpdateGateVisuals();
        UpdateInteractionPrompt();
        
        GameLogger.Log(LogCategory.Gameplay, "Gate successfully lockpicked!", this);
    }
    
    public void TryBribe()
    {
        if (!canBeBribed || !isLocked) return;
        
        VampireStats vampireStats = VampireStats.instance;
        if (vampireStats == null || vampireStats.totalBlood < bribeCost) return;
        
        // Find nearby guard
        GuardAI nearbyGuard = null;
        Collider[] colliders = Physics.OverlapSphere(transform.position, 10f);
        
        foreach (var col in colliders)
        {
            GuardAI guard = col.GetComponent<GuardAI>();
            if (guard != null)
            {
                nearbyGuard = guard;
                break;
            }
        }
        
        if (nearbyGuard != null)
        {
            // Deduct blood as bribe
            vampireStats.totalBlood -= bribeCost;
            
            // Guard opens gate
            isLocked = false;
            nearbyGuard.SetBribed(true);
            
            UpdateGateVisuals();
            UpdateInteractionPrompt();
            
            GameLogger.Log(LogCategory.Gameplay, $"Bribed guard for {bribeCost} blood!", this);
        }
    }
    
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (locked) isOpen = false;
        
        UpdateGateVisuals();
        UpdateInteractionPrompt();
    }
    
    private void UpdateGateVisuals()
    {
        if (gateLight != null)
        {
            gateLight.color = isLocked ? lockedColor : unlockedColor;
        }
    }
    
    private void UpdateInteractionPrompt()
    {
        if (isLocked)
        {
            ShowLockedOptions();
        }
        else
        {
            interactionPrompt = isOpen ? "Close Gate (E)" : "Open Gate (E)";
        }
    }
    
    public bool IsLocked => isLocked;
    public bool IsOpen => isOpen;
    public Ward ConnectedWard => connectedWard;
}