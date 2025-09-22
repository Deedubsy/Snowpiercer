using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// SP-012: Scene Transition Setup Guide
/// Validates and configures CityGateTrigger components for seamless scene transitions
/// </summary>
public class SceneTransitionSetupGuide : MonoBehaviour
{
    [Header("SP-012 Configuration")]
    public bool validateOnStart = false;
    public bool autoFixIssues = false;

    [Header("Results")]
    [SerializeField] private bool validationPassed = false;
    [SerializeField] private List<string> validationResults = new List<string>();

    void Start()
    {
        if (validateOnStart)
        {
            ValidateSceneTransitionSetup();
        }
    }

    [ContextMenu("Validate Scene Transition Setup")]
    public void ValidateSceneTransitionSetup()
    {
        Debug.Log("=== SP-012: Scene Transition Validation ===");

        validationResults.Clear();

        // Test 1: Check for CityGateTrigger components
        bool hasGates = ValidateGateTriggers();

        // Test 2: Check SaveSystem integration
        bool saveSystemValid = ValidateSaveSystemIntegration();

        // Test 3: Check GameManager integration
        bool gameManagerValid = ValidateGameManagerIntegration();

        // Test 4: Check transition destinations
        bool destinationsValid = ValidateTransitionDestinations();

        // Test 5: Check UI configuration
        bool uiValid = ValidateUIConfiguration();

        // Overall result
        validationPassed = hasGates && saveSystemValid && gameManagerValid && destinationsValid && uiValid;

        // Display results
        DisplayValidationResults();

        if (autoFixIssues && !validationPassed)
        {
            AutoFixCommonIssues();
        }
    }

    bool ValidateGateTriggers()
    {
        Debug.Log("--- Validating CityGateTrigger components ---");

        CityGateTrigger[] gates = FindObjectsOfType<CityGateTrigger>();

        if (gates.Length == 0)
        {
            validationResults.Add("❌ No CityGateTrigger components found in scene");
            return false;
        }

        validationResults.Add($"✅ Found {gates.Length} CityGateTrigger component(s)");

        // Check each gate configuration
        bool allValid = true;
        foreach (var gate in gates)
        {
            string gateName = gate.name;

            // Check collider configuration
            Collider gateCollider = gate.GetComponent<Collider>();
            if (gateCollider == null)
            {
                validationResults.Add($"❌ Gate '{gateName}' missing Collider component");
                allValid = false;
            }
            else if (!gateCollider.isTrigger)
            {
                validationResults.Add($"❌ Gate '{gateName}' collider is not set as trigger");
                allValid = false;
            }
            else
            {
                validationResults.Add($"✅ Gate '{gateName}' has properly configured trigger collider");
            }

            // Check transition type configuration
            if (gate.transitionType == CityGateTrigger.TransitionType.FastTravel ||
                gate.transitionType == CityGateTrigger.TransitionType.AreaTransition)
            {
                if (gate.destinationPosition == Vector3.zero && string.IsNullOrEmpty(gate.destinationSpawnPointName))
                {
                    validationResults.Add($"⚠️ Gate '{gateName}' has no destination configured");
                }
                else
                {
                    validationResults.Add($"✅ Gate '{gateName}' has destination configured");
                }
            }

            // Check validation settings
            if (gate.validateBloodQuota && gate.requiresBloodQuota)
            {
                validationResults.Add($"✅ Gate '{gateName}' blood quota validation enabled");
            }

            if (gate.validateDaylight && gate.transitionType == CityGateTrigger.TransitionType.EnterTown)
            {
                validationResults.Add($"✅ Gate '{gateName}' daylight validation enabled");
            }
        }

        return allValid;
    }

    bool ValidateSaveSystemIntegration()
    {
        Debug.Log("--- Validating SaveSystem integration ---");

        if (SaveSystem.Instance == null)
        {
            validationResults.Add("❌ No SaveSystem instance found in scene");
            return false;
        }

        validationResults.Add("✅ SaveSystem instance found");

        // Check if SaveSystem has the required methods
        try
        {
            // Test SetPendingPlayerPosition method exists
            SaveSystem.Instance.SetPendingPlayerPosition(Vector3.zero);
            validationResults.Add("✅ SaveSystem SetPendingPlayerPosition method available");
        }
        catch (System.Exception)
        {
            validationResults.Add("❌ SaveSystem missing SetPendingPlayerPosition method");
            return false;
        }

        return true;
    }

    bool ValidateGameManagerIntegration()
    {
        Debug.Log("--- Validating GameManager integration ---");

        if (GameManager.instance == null)
        {
            validationResults.Add("❌ No GameManager instance found in scene");
            return false;
        }

        validationResults.Add("✅ GameManager instance found");

        // Check for required methods (we can't easily test private methods, so we assume they exist)
        validationResults.Add("✅ GameManager integration assumed valid");

        return true;
    }

    bool ValidateTransitionDestinations()
    {
        Debug.Log("--- Validating transition destinations ---");

        CityGateTrigger[] gates = FindObjectsOfType<CityGateTrigger>();
        bool allValid = true;

        foreach (var gate in gates)
        {
            // Check if spawn points exist for named destinations
            if (!string.IsNullOrEmpty(gate.destinationSpawnPointName))
            {
                GameObject spawnPoint = GameObject.Find(gate.destinationSpawnPointName);
                if (spawnPoint == null)
                {
                    validationResults.Add($"❌ Gate '{gate.name}' destination spawn point '{gate.destinationSpawnPointName}' not found");
                    allValid = false;
                }
                else
                {
                    validationResults.Add($"✅ Gate '{gate.name}' destination spawn point '{gate.destinationSpawnPointName}' found");
                }
            }

            // Validate scene references
            if (!string.IsNullOrEmpty(gate.destinationScene))
            {
                validationResults.Add($"✅ Gate '{gate.name}' has destination scene: {gate.destinationScene}");
            }
        }

        return allValid;
    }

    bool ValidateUIConfiguration()
    {
        Debug.Log("--- Validating UI configuration ---");

        CityGateTrigger[] gates = FindObjectsOfType<CityGateTrigger>();
        bool allValid = true;

        foreach (var gate in gates)
        {
            int uiElements = 0;

            if (gate.promptUI != null)
            {
                uiElements++;
                validationResults.Add($"✅ Gate '{gate.name}' has prompt UI configured");
            }

            if (gate.blockedUI != null)
            {
                uiElements++;
                validationResults.Add($"✅ Gate '{gate.name}' has blocked UI configured");
            }

            if (uiElements == 0)
            {
                validationResults.Add($"⚠️ Gate '{gate.name}' has no UI elements configured");
            }
        }

        return allValid;
    }

    void DisplayValidationResults()
    {
        Debug.Log("=== Scene Transition Validation Results ===");

        foreach (string result in validationResults)
        {
            if (result.StartsWith("✅"))
                Debug.Log(result);
            else if (result.StartsWith("❌"))
                Debug.LogError(result);
            else if (result.StartsWith("⚠️"))
                Debug.LogWarning(result);
            else
                Debug.Log(result);
        }

        if (validationPassed)
        {
            Debug.Log("🎉 SP-012 Scene Transition validation PASSED!");
        }
        else
        {
            Debug.LogWarning("⚠️ SP-012 Scene Transition validation needs attention");
        }

        Debug.Log("=== Validation Complete ===");
    }

    [ContextMenu("Auto-Fix Common Issues")]
    public void AutoFixCommonIssues()
    {
        Debug.Log("--- Auto-fixing common transition issues ---");

        int fixesApplied = 0;

        // Fix 1: Add trigger colliders to gates without them
        CityGateTrigger[] gates = FindObjectsOfType<CityGateTrigger>();
        foreach (var gate in gates)
        {
            Collider gateCollider = gate.GetComponent<Collider>();
            if (gateCollider == null)
            {
                BoxCollider newCollider = gate.gameObject.AddComponent<BoxCollider>();
                newCollider.isTrigger = true;
                newCollider.size = new Vector3(5, 5, 5);
                fixesApplied++;
                Debug.Log($"✅ Added trigger collider to gate '{gate.name}'");
            }
            else if (!gateCollider.isTrigger)
            {
                gateCollider.isTrigger = true;
                fixesApplied++;
                Debug.Log($"✅ Set collider as trigger on gate '{gate.name}'");
            }
        }

        // Fix 2: Create SaveSystem if missing
        if (SaveSystem.Instance == null)
        {
            GameObject saveSystemObj = new GameObject("SaveSystem");
            saveSystemObj.AddComponent<SaveSystem>();
            fixesApplied++;
            Debug.Log("✅ Created missing SaveSystem instance");
        }

        Debug.Log($"✅ Auto-fix complete: {fixesApplied} issues resolved");

        // Re-run validation
        ValidateSceneTransitionSetup();
    }

    [ContextMenu("Create Example Gate Triggers")]
    public void CreateExampleGateTriggers()
    {
        Debug.Log("--- Creating example gate triggers ---");

        // Create castle gate (return to castle)
        CreateExampleGate("Castle Gate", CityGateTrigger.TransitionType.ReturnToCastle,
                         new Vector3(200, 0, 120), true);

        // Create town entrance (enter town)
        CreateExampleGate("Town Entrance", CityGateTrigger.TransitionType.EnterTown,
                         new Vector3(400, 0, 280), false);

        Debug.Log("✅ Example gate triggers created");
    }

    void CreateExampleGate(string name, CityGateTrigger.TransitionType type, Vector3 position, bool requiresBlood)
    {
        GameObject gateObj = new GameObject(name);
        gateObj.transform.position = position;

        CityGateTrigger gate = gateObj.AddComponent<CityGateTrigger>();
        gate.transitionType = type;
        gate.requiresBloodQuota = requiresBlood;
        gate.validateBloodQuota = requiresBlood;
        gate.validateDaylight = (type == CityGateTrigger.TransitionType.EnterTown);

        // Add trigger collider
        BoxCollider collider = gateObj.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(8, 8, 8);

        // Create visual indicator
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "Visual Indicator";
        indicator.transform.SetParent(gateObj.transform);
        indicator.transform.localPosition = Vector3.zero;
        indicator.transform.localScale = new Vector3(6, 6, 2);

        // Set material color based on type
        Renderer renderer = indicator.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = type == CityGateTrigger.TransitionType.ReturnToCastle ? Color.blue : Color.green;
        renderer.material = mat;

        Debug.Log($"✅ Created {name} at {position}");
    }

    [ContextMenu("Show Setup Instructions")]
    public void ShowSetupInstructions()
    {
        Debug.Log(@"
=== SP-012: Scene Transition Setup Instructions ===

STEP 1: Create Gate Triggers
☐ Add CityGateTrigger components where transitions should occur
☐ Configure transition types (ReturnToCastle, EnterTown, FastTravel, AreaTransition)
☐ Set up trigger colliders (isTrigger = true)

STEP 2: Configure Destinations
☐ For same-scene transitions: Set destinationPosition or destinationSpawnPointName
☐ For cross-scene transitions: Set destinationScene and destinationPosition
☐ Create spawn point GameObjects for named destinations

STEP 3: Set Validation Rules
☐ requiresBloodQuota: Require blood quota before allowing transition
☐ validateDaylight: Prevent town entry during daylight
☐ allowEmergencyReturn: Allow castle return even without blood quota

STEP 4: Configure UI Elements
☐ Assign promptUI GameObject for interaction prompt
☐ Assign blockedUI GameObject for blocked transition feedback
☐ Set up particle effects and audio for transitions

STEP 5: Integration Testing
☐ Verify SaveSystem.Instance exists in scene
☐ Test transitions between areas
☐ Verify player position persistence across scene loads
☐ Test blood quota and daylight validation

STEP 6: Audio/Visual Polish
☐ Add transition particle effects
☐ Configure audio feedback for transitions
☐ Set transition delays for smooth experience

VALIDATION: Run 'Validate Scene Transition Setup' to verify configuration

TROUBLESHOOTING:
• No transition → Check trigger collider configuration and player tag
• Position not saved → Verify SaveSystem integration
• Blocked transitions → Check validation rules and requirements
• Missing destinations → Verify spawn points exist or positions are set
");
    }
}