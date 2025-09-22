using System.Collections;
using UnityEngine;

public class BellTower : InteractiveObject
{
    [Header("Bell Tower Settings")]
    [SerializeField] private float bellRadius = 100f; // How far the bell sound travels
    [SerializeField] private int maxTolls = 3; // Maximum times bell can be rung per night
    [SerializeField] private float tollCooldown = 30f; // Cooldown between tolls
    [SerializeField] private AudioClip bellSound;

    [Header("Sabotage Settings")]
    [SerializeField] private bool isSabotaged = false;
    [SerializeField] private float sabotageTime = 5f; // Time required to sabotage
    [SerializeField] private GameObject ropeVisual; // Visual representation of bell rope
    [SerializeField] private GameObject cutRopeVisual; // Visual for cut rope

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem bellParticles;
    [SerializeField] private Light bellLight;
    [SerializeField] private float lightIntensityOnRing = 10f;
    [SerializeField] private AnimationCurve lightFadeCurve;

    [Header("Interaction Settings")]
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private bool requiresCrouch = false;
    
    private int currentTolls = 0;
    private float lastTollTime = -999f;
    private bool isBeingSabotaged = false;
    private float sabotageProgress = 0f;
    private GlobalAlertSystem alertSystem;
    private AudioSource audioSource;
    private string interactionPrompt = "Ring Bell (E)";

    void Start()
    {
        // Set up interaction properties
        displayName = "Bell Tower";
        promptText = isSabotaged ? "Bell Sabotaged" : "Ring Bell (E)";
        interactionPrompt = promptText;

        alertSystem = GlobalAlertSystem.Instance;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Initialize visuals
        if (cutRopeVisual != null) cutRopeVisual.SetActive(false);
        if (bellLight != null) bellLight.intensity = 0f;
    }

    public override void Interact(PlayerController player)
    {
        if (isSabotaged)
        {
            GameLogger.Log(LogCategory.Gameplay, "Cannot ring sabotaged bell", this);
            return;
        }

        // Check cooldown
        if (Time.time - lastTollTime < tollCooldown)
        {
            GameLogger.Log(LogCategory.Gameplay, $"Bell on cooldown. Wait {tollCooldown - (Time.time - lastTollTime):F1} seconds", this);
            return;
        }

        RingBell(null);
    }

    // Legacy method for NPC interaction
    public void Interact()
    {
        Interact(null);
    }

    public void RingBell(SuspicionMeter ringer)
    {
        if (isSabotaged || currentTolls >= maxTolls) return;

        currentTolls++;
        lastTollTime = Time.time;

        // Play bell sound
        if (audioSource != null && bellSound != null)
        {
            audioSource.PlayOneShot(bellSound);
        }

        // Visual effects
        if (bellParticles != null) bellParticles.Play();
        StartCoroutine(AnimateBellLight());

        // Alert the global system
        if (alertSystem != null)
        {
            alertSystem.AdvanceAlertLevel();
        }

        // Alert all NPCs in radius
        AlertNPCsInRadius();

        // Make noise that travels far
        if (NoiseManager.Instance != null)
        {
            NoiseManager.MakeNoise(transform.position, bellRadius, 1.0f);
        }

        string ringerName = ringer != null ? ringer.gameObject.name : "Player";
        GameLogger.Log(LogCategory.Gameplay, $"Bell rung by {ringerName}! Toll {currentTolls}/{maxTolls}", this);

        // Update interaction prompt
        if (currentTolls >= maxTolls)
        {
            interactionPrompt = "Bell Exhausted";
            promptText = "Bell Exhausted";
        }
    }

    private void AlertNPCsInRadius()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, bellRadius);

        foreach (var collider in colliders)
        {
            // Alert citizens
            Citizen citizen = collider.GetComponent<Citizen>();
            if (citizen != null)
            {
                // Use the correct ReactToNoise signature for citizens
                citizen.ReactToNoise(transform.position, 1.0f);

                // Make citizens flee or hide
                if (alertSystem != null && alertSystem.CurrentAlertState >= GlobalAlertSystem.AlertState.Orange)
                {
                    citizen.FleeToSafety();
                }
            }

            // Alert guards
            GuardAI guard = collider.GetComponent<GuardAI>();
            if (guard != null)
            {
                // Use the correct InvestigateNoise method for guards
                guard.InvestigateNoise(transform.position, 1.0f);

                // Guards converge on last known player position
                if (alertSystem != null)
                {
                    Vector3 lastKnownPos = alertSystem.GetLastKnownPlayerPosition();
                    if (lastKnownPos != Vector3.zero)
                    {
                        guard.SetOverrideDestination(lastKnownPos);
                    }
                }
            }
        }
    }

    private IEnumerator AnimateBellLight()
    {
        if (bellLight == null) yield break;

        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Check if lightFadeCurve is assigned before using it
            if (lightFadeCurve != null)
            {
                bellLight.intensity = lightFadeCurve.Evaluate(t) * lightIntensityOnRing;
            }
            else
            {
                // Simple fade out if no curve is assigned
                bellLight.intensity = (1f - t) * lightIntensityOnRing;
            }
            yield return null;
        }

        bellLight.intensity = 0f;
    }

    // Sabotage functionality
    public void StartSabotage()
    {
        if (isSabotaged || isBeingSabotaged) return;

        VampireStats vampireStats = VampireStats.instance;
        if (vampireStats == null || !vampireStats.CanSabotage())
        {
            GameLogger.Log(LogCategory.Gameplay, "Cannot sabotage - need tools and skill", this);
            return;
        }

        isBeingSabotaged = true;
        sabotageProgress = 0f;
        StartCoroutine(SabotageProcess());
    }

    private IEnumerator SabotageProcess()
    {
        VampireStats vampireStats = VampireStats.instance;
        float actualSabotageTime = sabotageTime / vampireStats.SabotageSpeed;

        GameLogger.Log(LogCategory.Gameplay, "Starting bell sabotage...", this);

        while (sabotageProgress < actualSabotageTime)
        {
            // Check if player is still in range
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || Vector3.Distance(transform.position, player.transform.position) > interactionRange)
            {
                CancelSabotage();
                yield break;
            }

            sabotageProgress += Time.deltaTime;

            // Update UI or show progress
            float progress = sabotageProgress / actualSabotageTime;
            ShowSabotageProgress(progress);

            yield return null;
        }

        // Complete sabotage
        CompleteSabotage();
    }

    private void ShowSabotageProgress(float progress)
    {
        // This could update a UI element or visual indicator
        // For now, just update the interaction prompt
        interactionPrompt = $"Sabotaging... {(progress * 100):F0}%";
        promptText = interactionPrompt;
    }

    private void CancelSabotage()
    {
        isBeingSabotaged = false;
        sabotageProgress = 0f;
        interactionPrompt = "Ring Bell (E) / Sabotage (Hold F)";
        promptText = interactionPrompt;
        GameLogger.Log(LogCategory.Gameplay, "Sabotage cancelled", this);
    }

    private void CompleteSabotage()
    {
        isSabotaged = true;
        isBeingSabotaged = false;

        // Use a sabotage tool
        VampireStats.instance.UseSabotageTool();

        // Update visuals
        if (ropeVisual != null) ropeVisual.SetActive(false);
        if (cutRopeVisual != null) cutRopeVisual.SetActive(true);

        interactionPrompt = "Bell Sabotaged";
        promptText = interactionPrompt;

        GameLogger.Log(LogCategory.Gameplay, "Bell successfully sabotaged!", this);

        // Award achievement or progression
        AchievementSystem achievementSystem = FindObjectOfType<AchievementSystem>();
        if (achievementSystem != null)
        {
            achievementSystem.UnlockAchievement("SABOTEUR");
        }
    }

    public void RepairBell()
    {
        isSabotaged = false;
        currentTolls = 0;

        if (ropeVisual != null) ropeVisual.SetActive(true);
        if (cutRopeVisual != null) cutRopeVisual.SetActive(false);

        interactionPrompt = "Ring Bell (E) / Sabotage (Hold F)";
        promptText = interactionPrompt;
    }

    // Called at dawn to reset for next night
    public void ResetForNewNight()
    {
        currentTolls = 0;
        lastTollTime = -999f;

        if (!isSabotaged)
        {
            interactionPrompt = "Ring Bell (E) / Sabotage (Hold F)";
            promptText = interactionPrompt;
        }
    }

    // Properties
    public bool IsSabotaged => isSabotaged;
    public int TollsRemaining => maxTolls - currentTolls;
    public bool CanRing => !isSabotaged && currentTolls < maxTolls && Time.time - lastTollTime >= tollCooldown;

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, bellRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}