using UnityEngine;
using System.Collections;

public class DisguiseSystem : MonoBehaviour
{
    [Header("Disguise Visual Settings")]
    [SerializeField] private GameObject hoodedCloakModel;
    [SerializeField] private GameObject normalPlayerModel;
    [SerializeField] private Material cloakMaterial;
    [SerializeField] private ParticleSystem disguiseParticles;
    
    [Header("Disguise Stations")]
    [SerializeField] private float disguiseStationRange = 3f;
    [SerializeField] private LayerMask disguiseStationLayer;
    
    [Header("Visual Effects")]
    [SerializeField] private float disguiseTransitionTime = 1f;
    [SerializeField] private AnimationCurve transitionCurve;
    [SerializeField] private AudioClip disguiseEquipSound;
    [SerializeField] private AudioClip disguiseRemoveSound;
    
    private VampireStats vampireStats;
    private PlayerController playerController;
    private AudioSource audioSource;
    private bool isTransitioning = false;
    
    // UI Feedback
    private string currentDisguiseStatus = "No Disguise";
    
    private void Start()
    {
        vampireStats = GetComponent<VampireStats>();
        if (vampireStats == null)
        {
            vampireStats = VampireStats.instance;
        }
        
        playerController = GetComponent<PlayerController>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Subscribe to disguise state changes
        if (vampireStats != null)
        {
            vampireStats.OnDisguiseStateChanged += OnDisguiseStateChanged;
        }
        
        // Initialize visual state
        UpdateDisguiseVisuals(false);
    }
    
    private void Update()
    {
        // Check for disguise station interaction
        if (Input.GetKeyDown(KeyCode.G))
        {
            TryUseDisguiseStation();
        }
        
        // Update UI status
        UpdateDisguiseStatus();
    }
    
    private void TryUseDisguiseStation()
    {
        if (isTransitioning || vampireStats == null) return;
        
        // Check if near a disguise station
        Collider[] nearbyStations = Physics.OverlapSphere(transform.position, disguiseStationRange, disguiseStationLayer);
        
        foreach (var station in nearbyStations)
        {
            DisguiseStation disguiseStation = station.GetComponent<DisguiseStation>();
            if (disguiseStation != null && disguiseStation.CanUse())
            {
                if (vampireStats.IsDisguised)
                {
                    StartCoroutine(RemoveDisguiseAnimation());
                }
                else
                {
                    StartCoroutine(ApplyDisguiseAnimation(disguiseStation));
                }
                break;
            }
        }
    }
    
    private IEnumerator ApplyDisguiseAnimation(DisguiseStation station)
    {
        isTransitioning = true;
        
        // Play sound
        if (audioSource != null && disguiseEquipSound != null)
        {
            audioSource.PlayOneShot(disguiseEquipSound);
        }
        
        // Play particles
        if (disguiseParticles != null)
        {
            disguiseParticles.Play();
        }
        
        // Animate transition
        float elapsed = 0f;
        while (elapsed < disguiseTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / disguiseTransitionTime);
            
            // Could add visual effects here like fade or scale
            if (cloakMaterial != null)
            {
                cloakMaterial.SetFloat("_Alpha", t);
            }
            
            yield return null;
        }
        
        // Apply disguise
        vampireStats.ApplyDisguise(station.GetDisguiseDuration());
        station.UseStation();
        
        UpdateDisguiseVisuals(true);
        
        isTransitioning = false;
        
        GameLogger.Log(LogCategory.Gameplay, "Disguise applied!", this);
    }
    
    private IEnumerator RemoveDisguiseAnimation()
    {
        isTransitioning = true;
        
        // Play sound
        if (audioSource != null && disguiseRemoveSound != null)
        {
            audioSource.PlayOneShot(disguiseRemoveSound);
        }
        
        // Animate transition
        float elapsed = 0f;
        while (elapsed < disguiseTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(1f - (elapsed / disguiseTransitionTime));
            
            if (cloakMaterial != null)
            {
                cloakMaterial.SetFloat("_Alpha", t);
            }
            
            yield return null;
        }
        
        // Remove disguise
        vampireStats.RemoveDisguise();
        UpdateDisguiseVisuals(false);
        
        isTransitioning = false;
        
        GameLogger.Log(LogCategory.Gameplay, "Disguise removed!", this);
    }
    
    private void OnDisguiseStateChanged(bool isDisguised)
    {
        if (!isTransitioning)
        {
            UpdateDisguiseVisuals(isDisguised);
        }
    }
    
    private void UpdateDisguiseVisuals(bool isDisguised)
    {
        if (hoodedCloakModel != null)
            hoodedCloakModel.SetActive(isDisguised);
            
        if (normalPlayerModel != null)
            normalPlayerModel.SetActive(!isDisguised);
            
        // Update player movement animations if needed
        if (playerController != null)
        {
            playerController.SetDisguisedAnimations(isDisguised);
        }
    }
    
    private void UpdateDisguiseStatus()
    {
        if (vampireStats == null) return;
        
        if (vampireStats.IsDisguised)
        {
            float timeRemaining = vampireStats.DisguiseTimeRemaining;
            currentDisguiseStatus = $"Disguised ({timeRemaining:F0}s)";
            
            // Warning when disguise is about to expire
            if (timeRemaining < 30f && timeRemaining > 0)
            {
                currentDisguiseStatus += " - EXPIRING SOON!";
            }
        }
        else
        {
            currentDisguiseStatus = "No Disguise";
        }
    }
    
    public string GetDisguiseStatus()
    {
        return currentDisguiseStatus;
    }
    
    private void OnDestroy()
    {
        if (vampireStats != null)
        {
            vampireStats.OnDisguiseStateChanged -= OnDisguiseStateChanged;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, disguiseStationRange);
    }
}

// Disguise Station component
public class DisguiseStation : InteractiveObject
{
    [Header("Station Settings")]
    [SerializeField] private float disguiseDuration = 300f; // 5 minutes
    [SerializeField] private int maxUses = 3;
    [SerializeField] private float cooldownTime = 60f;
    [SerializeField] private bool infiniteUses = false;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject[] clothingItems;
    [SerializeField] private Light stationLight;
    [SerializeField] private Color availableColor = Color.green;
    [SerializeField] private Color unavailableColor = Color.red;
    
    private int currentUses = 0;
    private float lastUseTime = -999f;
    
    public override void Start()
    {
        base.Start();
        interactionPrompt = "Change Clothes (G)";
        requiresCrouch = false;
        interactionRange = 3f;
        
        UpdateVisuals();
    }
    
    public override void Interact(PlayerController player)
    {
        // Handled by DisguiseSystem
    }
    
    public bool CanUse()
    {
        if (infiniteUses) return true;
        
        bool hasUsesLeft = currentUses < maxUses;
        bool cooldownPassed = Time.time - lastUseTime >= cooldownTime;
        
        return hasUsesLeft && cooldownPassed;
    }
    
    public void UseStation()
    {
        if (!infiniteUses)
        {
            currentUses++;
            lastUseTime = Time.time;
        }
        
        UpdateVisuals();
        
        // Hide some clothing items to show they've been taken
        if (clothingItems != null && clothingItems.Length > 0 && currentUses <= clothingItems.Length)
        {
            clothingItems[currentUses - 1].SetActive(false);
        }
    }
    
    private void UpdateVisuals()
    {
        bool canUse = CanUse();
        
        if (stationLight != null)
        {
            stationLight.color = canUse ? availableColor : unavailableColor;
        }
        
        // Update interaction prompt
        if (!canUse)
        {
            if (currentUses >= maxUses)
            {
                interactionPrompt = "No Clothes Left";
            }
            else
            {
                float cooldownRemaining = cooldownTime - (Time.time - lastUseTime);
                interactionPrompt = $"Cooldown ({cooldownRemaining:F0}s)";
            }
        }
        else
        {
            interactionPrompt = $"Change Clothes (G) [{maxUses - currentUses} left]";
        }
    }
    
    public float GetDisguiseDuration()
    {
        return disguiseDuration;
    }
    
    public void RefillStation()
    {
        currentUses = 0;
        lastUseTime = -999f;
        
        // Restore all clothing items
        if (clothingItems != null)
        {
            foreach (var item in clothingItems)
            {
                if (item != null)
                    item.SetActive(true);
            }
        }
        
        UpdateVisuals();
    }
}