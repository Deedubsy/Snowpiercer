using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VampireAbilities : MonoBehaviour
{
    [Header("Blood Drinking Settings")]
    public float drinkRange = 2f;             // Maximum distance to drink blood from a citizen.
    public KeyCode drinkKey = KeyCode.E;      // Key to activate blood drinking.
    public float bloodAmountPerCitizen = 25f;   // Amount of blood gained per citizen.
    public float drinkCooldown = 1.0f;          // Cooldown time between drinks.

    private float drinkTimer = 0f;            // Timer to enforce cooldown.

    [Header("Temporary Upgrades")]
    public List<ActiveUpgrade> activeUpgrades = new List<ActiveUpgrade>();

    private VampireStats stats;

    public enum UpgradeType
    {
        NightVision,
        CloakOfShadows,
        SwiftHunter,
        GluttonousDrain,
        SilentSteps,
        HypnoticGaze,
        DoubleBlood,
        Shadowstep,      // Teleport short distances through shadows
        EnhancedSenses,  // See through walls briefly
        BloodFrenzy      // Faster drain and movement after feeding
    }

    [System.Serializable]
    public class ActiveUpgrade
    {
        public UpgradeType type;
        public float duration;
        public float timer;
        public bool isActive;
    }

    void Start()
    {
        stats = GetComponent<VampireStats>();
    }

    void Update()
    {
        drinkTimer += Time.deltaTime;
        UpdateUpgrades(Time.deltaTime);
        if (HasUpgrade(UpgradeType.HypnoticGaze))
        {
            TryHypnoticGaze();
        }

        // Check if the drink key is pressed and the cooldown has elapsed.
        if (Input.GetKeyDown(drinkKey) && drinkTimer >= drinkCooldown)
        {
            DrinkBlood();
            drinkTimer = 0f;
        }
    }

    void UpdateUpgrades(float deltaTime)
    {
        for (int i = activeUpgrades.Count - 1; i >= 0; i--)
        {
            var upgrade = activeUpgrades[i];
            if (upgrade.isActive)
            {
                upgrade.timer += deltaTime;
                if (upgrade.timer >= upgrade.duration)
                {
                    RemoveUpgrade(upgrade.type);
                    activeUpgrades.RemoveAt(i);
                }
            }
        }
    }

    void ApplyUpgrade(UpgradeType type, float duration)
    {
        // Prevent stacking the same upgrade
        foreach (var up in activeUpgrades)
        {
            if (up.type == type)
            {
                up.timer = 0f; // Refresh duration
                return;
            }
        }
        var upgrade = new ActiveUpgrade { type = type, duration = duration, timer = 0f, isActive = true };
        activeUpgrades.Add(upgrade);
        // Apply stat modifications
        switch (type)
        {
            case UpgradeType.NightVision:
                SetAllEnemiesHighlight(true);
                break;
            case UpgradeType.CloakOfShadows:
                if (stats != null) stats.spotDistance *= 0.3f;
                break;
            case UpgradeType.SwiftHunter:
                if (stats != null) { stats.sprintDuration *= 1.5f; stats.crouchSpeed *= 1.5f; }
                break;
            case UpgradeType.GluttonousDrain:
                if (stats != null) { stats.killDrainRange *= 1.5f; stats.bloodDrainSpeed *= 0.5f; }
                break;
            case UpgradeType.SilentSteps:
                // Implement sound effect elsewhere
                break;
            case UpgradeType.HypnoticGaze:
                // Implement effect elsewhere
                break;
            case UpgradeType.DoubleBlood:
                // Handled in DrinkBlood
                break;
            case UpgradeType.Shadowstep:
                // Enable shadowstep ability
                EnableShadowstep(true);
                break;
            case UpgradeType.EnhancedSenses:
                // Enable wall vision
                EnableWallVision(true);
                break;
            case UpgradeType.BloodFrenzy:
                // Increase movement and drain speed
                if (stats != null) 
                { 
                    stats.walkSpeed *= 1.5f; 
                    stats.crouchSpeed *= 1.5f;
                    stats.bloodDrainSpeed *= 0.5f;
                }
                break;
        }
        Debug.Log($"Upgrade applied: {type} for {duration} seconds");
    }

    void RemoveUpgrade(UpgradeType type)
    {
        // Revert stat modifications
        switch (type)
        {
            case UpgradeType.NightVision:
                SetAllEnemiesHighlight(false);
                break;
            case UpgradeType.CloakOfShadows:
                if (stats != null) stats.spotDistance /= 0.3f;
                break;
            case UpgradeType.SwiftHunter:
                if (stats != null) { stats.sprintDuration /= 1.5f; stats.crouchSpeed /= 1.5f; }
                break;
            case UpgradeType.GluttonousDrain:
                if (stats != null) { stats.killDrainRange /= 1.5f; stats.bloodDrainSpeed /= 0.5f; }
                break;
            case UpgradeType.Shadowstep:
                EnableShadowstep(false);
                break;
            case UpgradeType.EnhancedSenses:
                EnableWallVision(false);
                break;
            case UpgradeType.BloodFrenzy:
                if (stats != null) 
                { 
                    stats.walkSpeed /= 1.5f; 
                    stats.crouchSpeed /= 1.5f;
                    stats.bloodDrainSpeed /= 0.5f;
                }
                break;
            // Other upgrades: revert as needed
        }
        Debug.Log($"Upgrade expired: {type}");
    }

    void SetAllEnemiesHighlight(bool on)
    {
        // Use CitizenManager for better performance
        if (CitizenManager.Instance != null)
        {
            foreach (var citizen in CitizenManager.Instance.GetAllCitizens())
            {
                if (citizen != null)
                {
                    var h = citizen.GetComponent<Highlightable>();
                    if (h != null) h.SetHighlight(on);
                }
            }
        }
        
        // Use GuardAlertnessManager for guards
        if (GuardAlertnessManager.Instance != null)
        {
            var guards = GuardAlertnessManager.Instance.GetAllGuards();
            if (guards != null)
            {
                foreach (var guard in guards)
                {
                    if (guard != null)
                    {
                        var h = guard.GetComponent<Highlightable>();
                        if (h != null) h.SetHighlight(on);
                    }
                }
            }
        }
    }

    void DrinkBlood()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, stats != null ? stats.killDrainRange : drinkRange))
        {
            Citizen citizen = hit.collider.GetComponent<Citizen>();
            if (citizen != null && !citizen.isDrained)
            {
                citizen.Drain();
                float blood = citizen.bloodAmount;
                if (HasUpgrade(UpgradeType.DoubleBlood))
                {
                    blood *= 2f;
                    doubleBloodDrinksLeft--;
                    if (doubleBloodDrinksLeft <= 0)
                        RemoveUpgrade(UpgradeType.DoubleBlood);
                }
                VampireStats stats = GetComponent<VampireStats>();
                if (stats != null)
                {
                    stats.AddBlood(blood);
                }
                
                if (DynamicObjectiveSystem.Instance != null)
                {
                    DynamicObjectiveSystem.Instance.OnBloodCollected(blood);
                    Debug.Log($"Notified objective system: citizen drained, {blood} blood collected");
                }
                else
                {
                    Debug.LogWarning("DynamicObjectiveSystem not found - objective progress not updated");
                }
                
                Debug.Log($"Drank blood from {citizen.rarity}! Gained {blood} blood.");
                TryGrantRandomUpgrade(citizen.rarity);
            }
        }
    }

    int doubleBloodDrinksLeft = 0;
    bool HasUpgrade(UpgradeType type)
    {
        foreach (var up in activeUpgrades)
            if (up.type == type && up.isActive) return true;
        return false;
    }

    void TryGrantRandomUpgrade(CitizenRarity rarity)
    {
        // Rarer blood = higher chance for upgrade
        float chance = 0.05f + (int)rarity * 0.01f; // Peasant: 5%, Merchant: 20%, ... Royalty: 65%
        if (Random.value < chance)
        {
            UpgradeType upgrade = (UpgradeType)Random.Range(0, System.Enum.GetValues(typeof(UpgradeType)).Length);
            float duration = GetUpgradeDuration(upgrade);
            ApplyUpgrade(upgrade, duration);
            if (upgrade == UpgradeType.DoubleBlood)
                doubleBloodDrinksLeft = 3;
        }
    }

    float GetUpgradeDuration(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.NightVision: return 30f;
            case UpgradeType.CloakOfShadows: return 60f;
            case UpgradeType.SwiftHunter: return 90f;
            case UpgradeType.GluttonousDrain: return 20f;
            case UpgradeType.SilentSteps: return 60f;
            case UpgradeType.HypnoticGaze: return 10f;
            case UpgradeType.DoubleBlood: return 999f; // handled by drinks left
            case UpgradeType.Shadowstep: return 45f;
            case UpgradeType.EnhancedSenses: return 15f;
            case UpgradeType.BloodFrenzy: return 30f;
            default: return 30f;
        }
    }

    // Placeholder for additional vampire abilities.
    public void UpgradeStealth()
    {
        Debug.Log("Stealth upgraded!");
    }

    public void UpgradeSpeed()
    {
        Debug.Log("Speed upgraded!");
    }

    void TryHypnoticGaze()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam == null) return;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        RaycastHit hit;
        float maxRange = stats != null ? stats.spotDistance : 10f;
        if (Physics.Raycast(ray, out hit, maxRange))
        {
            Citizen citizen = hit.collider.GetComponent<Citizen>();
            if (citizen != null && !citizen.isHypnotized)
            {
                citizen.SetHypnotized(true);
                RemoveUpgrade(UpgradeType.HypnoticGaze);
                Debug.Log("Hypnotic Gaze: Citizen hypnotized!");
                return;
            }
            GuardAI guard = hit.collider.GetComponent<GuardAI>();
            if (guard != null && !guard.isHypnotized)
            {
                guard.SetHypnotized(true);
                RemoveUpgrade(UpgradeType.HypnoticGaze);
                Debug.Log("Hypnotic Gaze: Guard hypnotized!");
                return;
            }
        }
    }

    // New ability implementations
    private void EnableShadowstep(bool enable)
    {
        if (enable)
        {
            // Enable shadowstep controls
            StartCoroutine(ShadowstepHandler());
        }
    }

    private IEnumerator ShadowstepHandler()
    {
        while (HasUpgrade(UpgradeType.Shadowstep))
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                PerformShadowstep();
            }
            yield return null;
        }
    }

    private void PerformShadowstep()
    {
        // Find nearest shadow within range
        RaycastHit hit;
        Vector3 direction = transform.forward;
        float maxDistance = 10f;

        if (Physics.Raycast(transform.position, direction, out hit, maxDistance))
        {
            // Check if destination is in shadow
            Collider[] shadowColliders = Physics.OverlapSphere(hit.point, 1f);
            bool inShadow = false;
            
            foreach (var col in shadowColliders)
            {
                if (col.CompareTag("Shadow") || col.CompareTag("IndoorArea"))
                {
                    inShadow = true;
                    break;
                }
            }

            if (inShadow)
            {
                // Teleport effect
                GameObject teleportEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                teleportEffect.transform.position = transform.position;
                teleportEffect.transform.localScale = Vector3.one * 0.5f;
                Destroy(teleportEffect, 0.5f);

                // Move player
                transform.position = hit.point + Vector3.up * 0.5f;

                // Make noise at original position to confuse enemies
                NoiseManager.MakeNoise(teleportEffect.transform.position, 5f, 0.5f);
                
                GameLogger.Log(LogCategory.Gameplay, "Shadowstepped!", this);
            }
        }
    }

    private void EnableWallVision(bool enable)
    {
        // Find all walls and make them semi-transparent
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        
        foreach (var wall in walls)
        {
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (enable)
                {
                    // Store original material and make transparent
                    Material mat = renderer.material;
                    Color color = mat.color;
                    color.a = 0.3f;
                    mat.color = color;
                    
                    // Enable transparency rendering
                    mat.SetFloat("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                }
                else
                {
                    // Restore opacity
                    Material mat = renderer.material;
                    Color color = mat.color;
                    color.a = 1f;
                    mat.color = color;
                    
                    // Restore opaque rendering
                    mat.SetFloat("_Mode", 0);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 2000;
                }
            }
        }

        // Also highlight all NPCs through walls
        if (enable)
        {
            SetAllEnemiesHighlight(true);
        }
    }

    public bool HasActiveUpgrade(UpgradeType type)
    {
        return HasUpgrade(type);
    }

    public List<ActiveUpgrade> GetActiveUpgrades()
    {
        return new List<ActiveUpgrade>(activeUpgrades);
    }
}
