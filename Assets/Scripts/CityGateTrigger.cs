using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// SP-012: Enhanced City Gate Trigger System
/// Manages scene transitions between castle and town areas with save/load integration
/// </summary>
public class CityGateTrigger : MonoBehaviour
{
    [Header("Transition Settings")]
    public TransitionType transitionType = TransitionType.ReturnToCastle;
    public KeyCode interactionKey = KeyCode.F;
    public float transitionDelay = 1f;
    public bool requiresBloodQuota = false;

    [Header("Destination Configuration")]
    public string destinationScene = "";
    public Vector3 destinationPosition = Vector3.zero;
    public string destinationSpawnPointName = "";

    [Header("UI References")]
    public GameObject promptUI;
    public GameObject blockedUI;

    [Header("Audio/Visual")]
    public AudioSource transitionAudio;
    public ParticleSystem transitionEffect;

    [Header("Validation")]
    public bool validateBloodQuota = true;
    public bool validateDaylight = true;
    public bool allowEmergencyReturn = false;

    private bool playerInRange = false;
    private bool isLocked = false;
    private bool transitionInProgress = false;

    public enum TransitionType
    {
        ReturnToCastle,
        EnterTown,
        FastTravel,
        AreaTransition
    }

    void Start()
    {
        // Validate configuration
        ValidateConfiguration();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            UpdateUI();

            // Log for debugging
            Debug.Log($"Player entered {transitionType} trigger zone");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            UpdateUI();

            Debug.Log($"Player exited {transitionType} trigger zone");
        }
    }

    void Update()
    {
        if (playerInRange && !isLocked && !transitionInProgress && Input.GetKeyDown(interactionKey))
        {
            AttemptTransition();
        }
    }

    void UpdateUI()
    {
        // Hide all UI elements first
        if (promptUI != null) promptUI.SetActive(false);
        if (blockedUI != null) blockedUI.SetActive(false);

        if (!playerInRange) return;

        if (isLocked || !CanTransition())
        {
            if (blockedUI != null) blockedUI.SetActive(true);
        }
        else
        {
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    void AttemptTransition()
    {
        if (!CanTransition())
        {
            HandleBlockedTransition();
            return;
        }

        StartCoroutine(ExecuteTransition());
    }

    bool CanTransition()
    {
        if (isLocked) return false;

        // Check blood quota requirement
        if (requiresBloodQuota && validateBloodQuota)
        {
            if (VampireStats.instance != null && GameManager.instance != null)
            {
                float currentBlood = GameManager.instance.currentBlood;
                float dailyRequirement = GameManager.instance.dailyBloodGoal;

                if (currentBlood < dailyRequirement)
                {
                    if (!allowEmergencyReturn || transitionType != TransitionType.ReturnToCastle)
                    {
                        return false;
                    }
                }
            }
        }

        // Check daylight restrictions
        if (validateDaylight && transitionType == TransitionType.EnterTown)
        {
            if (GameManager.instance != null)
            {
                // Don't allow town entry when night time is low (approaching dawn)
                float remainingTime = GameManager.instance.currentTime;
                if (remainingTime < 300f) // Less than 5 minutes remaining
                {
                    return false;
                }
            }
        }

        return true;
    }

    void HandleBlockedTransition()
    {
        string reason = GetBlockedReason();
        Debug.LogWarning($"Transition blocked: {reason}");

        // Show appropriate feedback
        if (GameManager.instance != null)
        {
            GameManager.instance.ShowMessage($"Cannot transition: {reason}");
        }

        // Play blocked audio if available
        if (transitionAudio != null && transitionAudio.clip != null)
        {
            transitionAudio.pitch = 0.8f; // Lower pitch for blocked sound
            transitionAudio.Play();
        }
    }

    string GetBlockedReason()
    {
        if (isLocked) return "Gate is locked";

        if (requiresBloodQuota && validateBloodQuota && GameManager.instance != null)
        {
            float currentBlood = GameManager.instance.currentBlood;
            float dailyRequirement = GameManager.instance.dailyBloodGoal;

            if (currentBlood < dailyRequirement)
            {
                return $"Need {dailyRequirement - currentBlood:F0} more blood";
            }
        }

        if (validateDaylight && transitionType == TransitionType.EnterTown)
        {
            if (GameManager.instance != null)
            {
                float remainingTime = GameManager.instance.currentTime;
                if (remainingTime < 300f)
                {
                    return "Too close to sunrise";
                }
            }
        }

        return "Unknown restriction";
    }

    IEnumerator ExecuteTransition()
    {
        transitionInProgress = true;

        Debug.Log($"Starting {transitionType} transition");

        // Save current state before transition
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SaveGame();
        }

        // Play transition effects
        if (transitionEffect != null)
        {
            transitionEffect.Play();
        }

        if (transitionAudio != null && transitionAudio.clip != null)
        {
            transitionAudio.pitch = 1f;
            transitionAudio.Play();
        }

        // Hide UI during transition
        UpdateUI();

        // Wait for transition delay
        yield return new WaitForSeconds(transitionDelay);

        // Execute the transition based on type
        switch (transitionType)
        {
            case TransitionType.ReturnToCastle:
                ExecuteReturnToCastle();
                break;

            case TransitionType.EnterTown:
                ExecuteEnterTown();
                break;

            case TransitionType.FastTravel:
                ExecuteFastTravel();
                break;

            case TransitionType.AreaTransition:
                ExecuteAreaTransition();
                break;
        }

        transitionInProgress = false;
    }

    void ExecuteReturnToCastle()
    {
        Debug.Log("Executing return to castle");

        // Notify GameManager
        if (GameManager.instance != null)
        {
            GameManager.instance.ReturnToCastle();
        }

        // If we have a specific destination scene, load it
        if (!string.IsNullOrEmpty(destinationScene))
        {
            StartCoroutine(LoadSceneWithPosition());
        }
        else
        {
            // Default behavior - teleport within current scene
            TeleportToDestination();
        }
    }

    void ExecuteEnterTown()
    {
        Debug.Log("Executing enter town");

        // Notify GameManager
        if (GameManager.instance != null)
        {
            GameManager.instance.EnterTown();
        }

        // Load town scene or teleport
        if (!string.IsNullOrEmpty(destinationScene))
        {
            StartCoroutine(LoadSceneWithPosition());
        }
        else
        {
            TeleportToDestination();
        }
    }

    void ExecuteFastTravel()
    {
        Debug.Log("Executing fast travel");

        // Fast travel implementation
        if (!string.IsNullOrEmpty(destinationScene))
        {
            StartCoroutine(LoadSceneWithPosition());
        }
        else
        {
            TeleportToDestination();
        }
    }

    void ExecuteAreaTransition()
    {
        Debug.Log("Executing area transition");

        // Area transition implementation
        TeleportToDestination();
    }

    IEnumerator LoadSceneWithPosition()
    {
        Debug.Log($"Loading scene: {destinationScene}");

        // Store destination position for the new scene
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.SetPendingPlayerPosition(destinationPosition);
        }

        // Load the destination scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(destinationScene);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log($"Scene {destinationScene} loaded successfully");
    }

    void TeleportToDestination()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player not found for teleportation");
            return;
        }

        Vector3 targetPosition = destinationPosition;

        // Try to find spawn point by name if specified
        if (!string.IsNullOrEmpty(destinationSpawnPointName))
        {
            GameObject spawnPoint = GameObject.Find(destinationSpawnPointName);
            if (spawnPoint != null)
            {
                targetPosition = spawnPoint.transform.position;
            }
        }

        // Teleport player
        if (player.GetComponent<CharacterController>() != null)
        {
            player.GetComponent<CharacterController>().enabled = false;
            player.transform.position = targetPosition;
            player.GetComponent<CharacterController>().enabled = true;
        }
        else
        {
            player.transform.position = targetPosition;
        }

        Debug.Log($"Player teleported to {targetPosition}");
    }

    void ValidateConfiguration()
    {
        bool hasIssues = false;

        if (transitionType == TransitionType.FastTravel || transitionType == TransitionType.AreaTransition)
        {
            if (destinationPosition == Vector3.zero && string.IsNullOrEmpty(destinationSpawnPointName))
            {
                Debug.LogWarning($"CityGateTrigger '{name}': No destination configured");
                hasIssues = true;
            }
        }

        if (!hasIssues)
        {
            Debug.Log($"CityGateTrigger '{name}' configuration validated successfully");
        }
    }

    // Public methods for external control
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        UpdateUI();

        Debug.Log($"CityGateTrigger '{name}' lock state: {locked}");
    }

    public void SetBloodQuotaRequired(bool required)
    {
        requiresBloodQuota = required;
        UpdateUI();
    }

    public void ForceTransition()
    {
        if (!transitionInProgress)
        {
            StartCoroutine(ExecuteTransition());
        }
    }

    // Properties
    public bool IsLocked => isLocked;
    public bool IsPlayerInRange => playerInRange;
    public bool IsTransitionInProgress => transitionInProgress;
}
