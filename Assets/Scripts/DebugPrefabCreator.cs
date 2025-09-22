using UnityEditor;
using UnityEngine;

public class DebugPrefabCreator : MonoBehaviour
{
    [Header("Source Prefabs")]
    public GameObject guardPrefab;
    public GameObject citizenPrefab;

    [Header("Output Settings")]
    public string debugPrefabSuffix = "_Debug";
    public bool addDebugComponents = true;
    public bool setupMaterials = true;

    [ContextMenu("Create Debug Guard Prefab")]
    public void CreateDebugGuardPrefab()
    {
        if (guardPrefab == null)
        {
            Debug.LogError("[DebugPrefabCreator] No guard prefab assigned!");
            return;
        }

        CreateDebugPrefab(guardPrefab, "Guard", true);
    }

    [ContextMenu("Create Debug Citizen Prefab")]
    public void CreateDebugCitizenPrefab()
    {
        if (citizenPrefab == null)
        {
            Debug.LogError("[DebugPrefabCreator] No citizen prefab assigned!");
            return;
        }

        CreateDebugPrefab(citizenPrefab, "Citizen", false);
    }

    [ContextMenu("Create All Debug Prefabs")]
    public void CreateAllDebugPrefabs()
    {
        CreateDebugGuardPrefab();
        CreateDebugCitizenPrefab();
    }

    void CreateDebugPrefab(GameObject sourcePrefab, string entityType, bool isGuard)
    {
        // Instantiate the source prefab
        GameObject debugInstance = Instantiate(sourcePrefab);
        debugInstance.name = sourcePrefab.name + debugPrefabSuffix;

        try
        {
            // Add debug components
            if (addDebugComponents)
            {
                AddDebugComponents(debugInstance, isGuard);
            }

            // Setup materials for better visibility
            if (setupMaterials)
            {
                SetupDebugMaterials(debugInstance, isGuard);
            }

            // Configure for debug use
            ConfigureForDebug(debugInstance, isGuard);

            Debug.Log($"[DebugPrefabCreator] Created debug {entityType} prefab: {debugInstance.name}");

            // Select the created object for easy prefab creation
            Selection.activeGameObject = debugInstance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DebugPrefabCreator] Error creating debug prefab: {e.Message}");
            if (debugInstance != null)
                DestroyImmediate(debugInstance);
        }
    }

    void AddDebugComponents(GameObject debugObj, bool isGuard)
    {
        if (isGuard)
        {
            // Add Guard debug components
            if (debugObj.GetComponent<GuardAIDebugProvider>() == null)
                debugObj.AddComponent<GuardAIDebugProvider>();
        }
        else
        {
            // Add Citizen debug components
            if (debugObj.GetComponent<CitizenDebugProvider>() == null)
                debugObj.AddComponent<CitizenDebugProvider>();
        }

        // Add AIDebugUI to both
        if (debugObj.GetComponent<AIDebugUI>() == null)
        {
            AIDebugUI debugUI = debugObj.AddComponent<AIDebugUI>();
            debugUI.showDebugUI = true;
            debugUI.updateFrequency = 0.1f;
            debugUI.worldOffset = Vector3.up * 3f;
        }

        Debug.Log($"[DebugPrefabCreator] Added debug components to {debugObj.name}");
    }

    void SetupDebugMaterials(GameObject debugObj, bool isGuard)
    {
        // Create debug material
        Material debugMaterial = new Material(Shader.Find("Standard"));
        debugMaterial.name = isGuard ? "DebugGuardMaterial" : "DebugCitizenMaterial";

        // Set distinct colors for easy identification
        if (isGuard)
        {
            debugMaterial.color = new Color(1f, 0.3f, 0.3f, 0.8f); // Red tint
            debugMaterial.SetFloat("_Metallic", 0.2f);
            debugMaterial.SetFloat("_Smoothness", 0.1f);
        }
        else
        {
            debugMaterial.color = new Color(0.3f, 0.3f, 1f, 0.8f); // Blue tint
            debugMaterial.SetFloat("_Metallic", 0.1f);
            debugMaterial.SetFloat("_Smoothness", 0.3f);
        }

        // Apply to all renderers
        Renderer[] renderers = debugObj.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            // Skip if it's a UI element or special renderer
            if (renderer.gameObject.name.Contains("UI") ||
                renderer.gameObject.name.Contains("Canvas"))
                continue;

            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = debugMaterial;
            }
            renderer.materials = materials;
        }

        Debug.Log($"[DebugPrefabCreator] Applied debug materials to {debugObj.name}");
    }

    void ConfigureForDebug(GameObject debugObj, bool isGuard)
    {
        if (isGuard)
        {
            GuardAI guardAI = debugObj.GetComponent<GuardAI>();
            if (guardAI != null)
            {
                // Make detection more visible for testing
                guardAI.viewDistance = Mathf.Max(guardAI.viewDistance, 20f);
                guardAI.fieldOfView = Mathf.Max(guardAI.fieldOfView, 90f);
                guardAI.detectionTime = Mathf.Max(guardAI.detectionTime, 1f); // Slower for testing

                // Enable debug features
                guardAI.enablePeripheralVision = true;
                guardAI.enableGuardCommunication = true;
                guardAI.enablePredictiveChasing = true;

                Debug.Log($"[DebugPrefabCreator] Configured GuardAI for debug testing");
            }
        }
        else
        {
            Citizen citizen = debugObj.GetComponent<Citizen>();
            if (citizen != null)
            {
                // Make detection more visible for testing
                citizen.viewDistance = Mathf.Max(citizen.viewDistance, 15f);
                citizen.fieldOfView = Mathf.Max(citizen.fieldOfView, 75f);
                citizen.detectionTime = Mathf.Max(citizen.detectionTime, 1f); // Slower for testing

                // Enable debug features
                citizen.enablePeripheralVision = true;
                citizen.reactToNoises = true;
                citizen.reactToLights = true;

                // Set interesting personality for testing
                if (citizen.personality == CitizenPersonality.Normal)
                {
                    citizen.personality = CitizenPersonality.Curious;
                    citizen.curiosityLevel = 0.8f;
                    citizen.braveryLevel = 0.6f;
                    citizen.socialLevel = 0.7f;
                }

                Debug.Log($"[DebugPrefabCreator] Configured Citizen for debug testing");
            }
        }

        // Add name tag for easy identification
        GameObject nameTag = new GameObject("NameTag");
        nameTag.transform.SetParent(debugObj.transform);
        nameTag.transform.localPosition = Vector3.up * 2.5f;

        // Add 3D text for name
        TextMesh textMesh = nameTag.AddComponent<TextMesh>();
        textMesh.text = debugObj.name;
        textMesh.fontSize = 20;
        textMesh.color = isGuard ? Color.red : Color.blue;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;

        // Make text face camera
        nameTag.transform.rotation = Quaternion.LookRotation(Vector3.forward);
    }

    [ContextMenu("Setup Test Scene")]
    public void SetupTestScene()
    {
        // Create test scene controller if it doesn't exist
        AITestSceneController controller = FindObjectOfType<AITestSceneController>();
        if (controller == null)
        {
            GameObject controllerObj = new GameObject("AITestSceneController");
            controller = controllerObj.AddComponent<AITestSceneController>();

            // Assign the debug prefabs if they exist
            if (guardPrefab != null)
                controller.guardPrefab = guardPrefab;
            if (citizenPrefab != null)
                controller.citizenPrefab = citizenPrefab;

            Debug.Log("[DebugPrefabCreator] Created AITestSceneController");
        }

        // Select the controller
        Selection.activeGameObject = controller.gameObject;
    }
}