using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// SP-020: Physics Layer Validator
/// Validates collision layers and physics interactions for the GamePlay scene
/// </summary>
public class PhysicsLayerValidator : MonoBehaviour
{
    [Header("Layer Configuration")]
    [Tooltip("Layer numbers for each entity type")]
    public int playerLayer = 8;
    public int guardLayer = 9;
    public int citizenLayer = 10;
    public int interactiveLayer = 11;
    public int shadowLayer = 12;
    public int indoorAreaLayer = 13;

    [Header("Testing")]
    public bool validateOnStart = false;
    public bool enableVisualDebugging = false;

    [Header("Results")]
    [SerializeField] private bool validationPassed = false;
    [SerializeField] private string[] validationResults;

    void Start()
    {
        if (validateOnStart)
        {
            ValidatePhysicsSetup();
        }
    }

    [ContextMenu("Validate Physics Setup")]
    public void ValidatePhysicsSetup()
    {
        Debug.Log("=== SP-020: Physics Layer Validation ===");

        var results = new System.Collections.Generic.List<string>();

        // Test 1: Layer existence
        bool layersExist = ValidateLayerExistence(results);

        // Test 2: Collision matrix
        bool matrixValid = ValidateCollisionMatrix(results);

        // Test 3: Entity assignment
        bool entitiesAssigned = ValidateEntityLayers(results);

        // Test 4: Physics materials
        bool materialsValid = ValidatePhysicsMaterials(results);

        // Overall result
        validationPassed = layersExist && matrixValid && entitiesAssigned && materialsValid;
        validationResults = results.ToArray();

        Debug.Log("=== Physics Validation Results ===");
        foreach (string result in results)
        {
            if (result.StartsWith("✅"))
                Debug.Log(result);
            else if (result.StartsWith("❌"))
                Debug.LogError(result);
            else if (result.StartsWith("⚠️"))
                Debug.LogWarning(result);
        }

        if (validationPassed)
        {
            Debug.Log("🎉 SP-020 Physics validation PASSED!");
        }
        else
        {
            Debug.LogWarning("⚠️ SP-020 Physics validation needs attention");
        }
    }

    bool ValidateLayerExistence(System.Collections.Generic.List<string> results)
    {
        Debug.Log("--- Validating layer existence ---");

        bool allExist = true;

        // Check each required layer
        string[] requiredLayers = { "Player", "Guard", "Citizen", "Interactive", "Shadow", "IndoorArea" };
        int[] layerNumbers = { playerLayer, guardLayer, citizenLayer, interactiveLayer, shadowLayer, indoorAreaLayer };

        for (int i = 0; i < requiredLayers.Length; i++)
        {
            string layerName = LayerMask.LayerToName(layerNumbers[i]);
            if (string.IsNullOrEmpty(layerName))
            {
                results.Add($"❌ Layer {layerNumbers[i]} ({requiredLayers[i]}) not defined");
                allExist = false;
            }
            else if (layerName == requiredLayers[i])
            {
                results.Add($"✅ Layer {layerNumbers[i]}: {layerName} correctly defined");
            }
            else
            {
                results.Add($"⚠️ Layer {layerNumbers[i]} exists as '{layerName}' but expected '{requiredLayers[i]}'");
            }
        }

        return allExist;
    }

    bool ValidateCollisionMatrix(System.Collections.Generic.List<string> results)
    {
        Debug.Log("--- Validating collision matrix ---");

        bool matrixValid = true;

        // Define expected collision rules
        var collisionRules = new System.Collections.Generic.Dictionary<(int, int), bool>
        {
            // Player interactions
            { (playerLayer, guardLayer), true },        // Player should collide with guards
            { (playerLayer, citizenLayer), true },      // Player should collide with citizens
            { (playerLayer, interactiveLayer), true },  // Player should interact with objects
            { (playerLayer, shadowLayer), false },      // Player shouldn't collide with shadow triggers
            { (playerLayer, indoorAreaLayer), false },  // Player shouldn't collide with area triggers

            // Guard interactions
            { (guardLayer, citizenLayer), true },       // Guards should collide with citizens
            { (guardLayer, interactiveLayer), true },   // Guards should interact with objects
            { (guardLayer, shadowLayer), false },       // Guards shouldn't collide with shadow triggers
            { (guardLayer, indoorAreaLayer), false },   // Guards shouldn't collide with area triggers

            // Citizen interactions
            { (citizenLayer, interactiveLayer), true }, // Citizens should interact with objects
            { (citizenLayer, shadowLayer), false },     // Citizens shouldn't collide with shadow triggers
            { (citizenLayer, indoorAreaLayer), false }, // Citizens shouldn't collide with area triggers

            // Special layers
            { (shadowLayer, interactiveLayer), false }, // Shadow triggers independent
            { (shadowLayer, indoorAreaLayer), false },  // Shadow and indoor areas independent
            { (interactiveLayer, indoorAreaLayer), false } // Interactive objects and areas independent
        };

        foreach (var rule in collisionRules)
        {
            bool shouldCollide = rule.Value;
            bool actuallyCollides = !Physics.GetIgnoreLayerCollision(rule.Key.Item1, rule.Key.Item2);

            string layer1Name = LayerMask.LayerToName(rule.Key.Item1);
            string layer2Name = LayerMask.LayerToName(rule.Key.Item2);

            if (actuallyCollides == shouldCollide)
            {
                string status = shouldCollide ? "collide" : "ignore";
                results.Add($"✅ {layer1Name} ↔ {layer2Name}: correctly {status}");
            }
            else
            {
                string expected = shouldCollide ? "should collide" : "should ignore";
                string actual = actuallyCollides ? "currently collides" : "currently ignores";
                results.Add($"❌ {layer1Name} ↔ {layer2Name}: {expected} but {actual}");
                matrixValid = false;
            }
        }

        return matrixValid;
    }

    bool ValidateEntityLayers(System.Collections.Generic.List<string> results)
    {
        Debug.Log("--- Validating entity layer assignments ---");

        bool allAssigned = true;

        // Find entities in scene and check their layers
        var players = FindObjectsOfType<PlayerController>();
        var guards = FindObjectsOfType<GuardAI>();
        var citizens = FindObjectsOfType<Citizen>();

        // Validate player layers
        foreach (var player in players)
        {
            if (player.gameObject.layer == playerLayer)
            {
                results.Add($"✅ Player '{player.name}' on correct layer {playerLayer}");
            }
            else
            {
                results.Add($"❌ Player '{player.name}' on layer {player.gameObject.layer}, expected {playerLayer}");
                allAssigned = false;
            }
        }

        // Validate guard layers
        foreach (var guard in guards)
        {
            if (guard.gameObject.layer == guardLayer)
            {
                results.Add($"✅ Guard '{guard.name}' on correct layer {guardLayer}");
            }
            else
            {
                results.Add($"❌ Guard '{guard.name}' on layer {guard.gameObject.layer}, expected {guardLayer}");
                allAssigned = false;
            }
        }

        // Validate citizen layers
        foreach (var citizen in citizens)
        {
            if (citizen.gameObject.layer == citizenLayer)
            {
                results.Add($"✅ Citizen '{citizen.name}' on correct layer {citizenLayer}");
            }
            else
            {
                results.Add($"❌ Citizen '{citizen.name}' on layer {citizen.gameObject.layer}, expected {citizenLayer}");
                allAssigned = false;
            }
        }

        // Check for interactive objects
        var interactiveObjects = FindObjectsOfType<InteractiveObject>();
        foreach (var obj in interactiveObjects)
        {
            if (obj.gameObject.layer == interactiveLayer)
            {
                results.Add($"✅ Interactive '{obj.name}' on correct layer {interactiveLayer}");
            }
            else
            {
                results.Add($"❌ Interactive '{obj.name}' on layer {obj.gameObject.layer}, expected {interactiveLayer}");
                allAssigned = false;
            }
        }

        if (players.Length == 0 && guards.Length == 0 && citizens.Length == 0)
        {
            results.Add("⚠️ No entities found in scene - validation limited");
        }

        return allAssigned;
    }

    bool ValidatePhysicsMaterials(System.Collections.Generic.List<string> results)
    {
        Debug.Log("--- Validating physics materials ---");

        bool materialsValid = true;

        // Check for common physics materials
        var colliders = FindObjectsOfType<Collider>();
        int materialsFound = 0;
        int materialsTotal = colliders.Length;

        foreach (var collider in colliders)
        {
            if (collider.material != null)
            {
                materialsFound++;

                // Check for reasonable friction/bounce values
                PhysicsMaterial mat = collider.material;
                if (mat.dynamicFriction < 0 || mat.dynamicFriction > 1 ||
                    mat.staticFriction < 0 || mat.staticFriction > 1 ||
                    mat.bounciness < 0 || mat.bounciness > 1)
                {
                    results.Add($"⚠️ '{collider.name}' has invalid physics material values");
                    materialsValid = false;
                }
            }
        }

        float materialPercentage = materialsTotal > 0 ? (float)materialsFound / materialsTotal * 100f : 0f;
        results.Add($"📊 Physics materials: {materialsFound}/{materialsTotal} ({materialPercentage:F1}%) have materials assigned");

        if (materialPercentage < 50f)
        {
            results.Add("⚠️ Consider assigning physics materials to more colliders for realistic behavior");
        }
        else
        {
            results.Add("✅ Good physics material coverage");
        }

        return materialsValid;
    }

    [ContextMenu("Auto-Fix Layer Assignments")]
    public void AutoFixLayerAssignments()
    {
        Debug.Log("--- Auto-fixing layer assignments ---");

        int fixedCount = 0;

        // Fix players
        var players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            if (player.gameObject.layer != playerLayer)
            {
                player.gameObject.layer = playerLayer;
                fixedCount++;
                Debug.Log($"Fixed player '{player.name}' layer to {playerLayer}");
            }
        }

        // Fix guards
        var guards = FindObjectsOfType<GuardAI>();
        foreach (var guard in guards)
        {
            if (guard.gameObject.layer != guardLayer)
            {
                guard.gameObject.layer = guardLayer;
                fixedCount++;
                Debug.Log($"Fixed guard '{guard.name}' layer to {guardLayer}");
            }
        }

        // Fix citizens
        var citizens = FindObjectsOfType<Citizen>();
        foreach (var citizen in citizens)
        {
            if (citizen.gameObject.layer != citizenLayer)
            {
                citizen.gameObject.layer = citizenLayer;
                fixedCount++;
                Debug.Log($"Fixed citizen '{citizen.name}' layer to {citizenLayer}");
            }
        }

        // Fix interactive objects
        var interactiveObjects = FindObjectsOfType<InteractiveObject>();
        foreach (var obj in interactiveObjects)
        {
            if (obj.gameObject.layer != interactiveLayer)
            {
                obj.gameObject.layer = interactiveLayer;
                fixedCount++;
                Debug.Log($"Fixed interactive '{obj.name}' layer to {interactiveLayer}");
            }
        }

        Debug.Log($"✅ Auto-fix complete: {fixedCount} layer assignments corrected");

        // Re-run validation
        ValidatePhysicsSetup();
    }

    [ContextMenu("Show Collision Matrix")]
    public void ShowCollisionMatrix()
    {
        Debug.Log("=== Current Collision Matrix ===");

        string[] layerNames = { "Player", "Guard", "Citizen", "Interactive", "Shadow", "IndoorArea" };
        int[] layers = { playerLayer, guardLayer, citizenLayer, interactiveLayer, shadowLayer, indoorAreaLayer };

        for (int i = 0; i < layers.Length; i++)
        {
            for (int j = i; j < layers.Length; j++)
            {
                bool collides = !Physics.GetIgnoreLayerCollision(layers[i], layers[j]);
                string status = collides ? "COLLIDE" : "IGNORE";
                Debug.Log($"{layerNames[i]} ↔ {layerNames[j]}: {status}");
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Open Physics Settings")]
    public void OpenPhysicsSettings()
    {
        EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
        Debug.Log("📋 Navigate to Physics → Layer Collision Matrix to configure collision rules");
    }
#endif
}