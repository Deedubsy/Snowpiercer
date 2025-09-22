using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AIDebugUI : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebugUI = true;
    public float updateFrequency = 0.1f;
    public Vector3 worldOffset = Vector3.up * 3f;

    [Header("UI Prefab References")]
    public GameObject debugPanelPrefab;
    public Canvas worldCanvas;

    [Header("UI Components")]
    public RectTransform debugPanel;
    public TextMeshProUGUI entityNameText;
    public TextMeshProUGUI currentStateText;
    public Slider detectionSlider;
    public TextMeshProUGUI detectionText;
    public Transform debugEntriesParent;
    public GameObject debugEntryPrefab;

    private IDebugProvider debugProvider;
    private Camera mainCamera;
    private float updateTimer = 0f;
    private List<GameObject> debugEntryObjects = new List<GameObject>();
    private AIDebugInfo currentDebugInfo = new AIDebugInfo();

    void Start()
    {
        debugProvider = GetComponent<IDebugProvider>();
        mainCamera = Camera.main;

        if (debugProvider == null)
        {
            Debug.LogWarning($"[AIDebugUI] No IDebugProvider found on {gameObject.name}");
            enabled = false;
            return;
        }

        SetupDebugUI();
    }

    void SetupDebugUI()
    {
        // Find or create world canvas
        if (worldCanvas == null)
        {
            worldCanvas = DebugUIManager.Instance?.GetWorldCanvas();
        }

        if (worldCanvas == null)
        {
            Debug.LogWarning("[AIDebugUI] No world canvas found. Creating temporary one.");
            CreateTemporaryCanvas();
        }

        // Get debug panel prefab from DebugUIManager if not assigned
        if (debugPanelPrefab == null && DebugUIManager.Instance != null)
        {
            debugPanelPrefab = DebugUIManager.Instance.GetDebugPanelPrefab();
        }

        // Create debug panel if not assigned
        if (debugPanel == null && debugPanelPrefab != null)
        {
            GameObject panelObj = Instantiate(debugPanelPrefab, worldCanvas.transform);
            debugPanel = panelObj.GetComponent<RectTransform>();

            // Get references to UI components
            entityNameText = debugPanel.Find("EntityName")?.GetComponent<TextMeshProUGUI>();
            currentStateText = debugPanel.Find("CurrentState")?.GetComponent<TextMeshProUGUI>();
            detectionSlider = debugPanel.Find("DetectionSlider")?.GetComponent<Slider>();
            detectionText = debugPanel.Find("DetectionText")?.GetComponent<TextMeshProUGUI>();
            debugEntriesParent = debugPanel.Find("DebugEntries");

            // Find debug entry prefab within the panel
            Transform entryPrefabTransform = debugPanel.Find("DebugEntryPrefab");
            if (entryPrefabTransform != null)
            {
                debugEntryPrefab = entryPrefabTransform.gameObject;
                debugEntryPrefab.SetActive(false);
            }
        }

        // Initialize UI
        if (debugPanel != null)
        {
            debugPanel.gameObject.SetActive(showDebugUI);
            UpdatePanelPosition();
        }
        
        // Register with DebugUIManager
        DebugUIManager.Instance?.RegisterDebugUI(this);
    }

    void CreateTemporaryCanvas()
    {
        GameObject canvasObj = new GameObject("TempDebugCanvas");
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = mainCamera;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    void Update()
    {
        if (!showDebugUI || debugProvider == null || debugPanel == null)
            return;

        UpdatePanelPosition();

        updateTimer += Time.deltaTime;
        if (updateTimer >= updateFrequency)
        {
            UpdateDebugInfo();
            updateTimer = 0f;
        }
    }

    void UpdatePanelPosition()
    {
        if (debugPanel == null || mainCamera == null)
            return;

        Vector3 worldPos = transform.position + worldOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // Keep panel on screen
        if (screenPos.z > 0)
        {
            debugPanel.position = screenPos;
            debugPanel.gameObject.SetActive(true);
        }
        else
        {
            debugPanel.gameObject.SetActive(false);
        }
    }

    void UpdateDebugInfo()
    {
        if (debugProvider == null) return;

        // Get fresh debug data
        currentDebugInfo.Clear();
        currentDebugInfo.entityName = debugProvider.GetEntityName();
        currentDebugInfo.currentState = debugProvider.GetCurrentState();
        currentDebugInfo.detectionProgress = debugProvider.GetDetectionProgress();
        currentDebugInfo.position = debugProvider.GetPosition();

        // Get additional debug data
        var debugData = debugProvider.GetDebugData();
        foreach (var kvp in debugData)
        {
            Color color = Color.white;

            // Color code certain types of data
            if (kvp.Key.ToLower().Contains("distance"))
                color = Color.cyan;
            else if (kvp.Key.ToLower().Contains("angle"))
                color = Color.yellow;
            else if (kvp.Key.ToLower().Contains("alert") || kvp.Key.ToLower().Contains("suspicion"))
                color = Color.magenta;
            else if (kvp.Key.ToLower().Contains("health"))
                color = Color.red;

            currentDebugInfo.AddEntry(kvp.Key, kvp.Value, color);
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        // Update basic info
        if (entityNameText != null)
            entityNameText.text = currentDebugInfo.entityName;

        if (currentStateText != null)
        {
            currentStateText.text = $"State: {currentDebugInfo.currentState}";

            // Color code states
            Color stateColor = GetStateColor(currentDebugInfo.currentState);
            currentStateText.color = stateColor;
        }

        // Update detection slider
        if (detectionSlider != null)
        {
            detectionSlider.value = currentDebugInfo.detectionProgress;

            // Color code detection progress
            Image fillImage = detectionSlider.fillRect?.GetComponent<Image>();
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(Color.green, Color.red, currentDebugInfo.detectionProgress);
            }
        }

        if (detectionText != null)
        {
            detectionText.text = $"Detection: {(currentDebugInfo.detectionProgress * 100f):F0}%";
        }

        // Update debug entries
        UpdateDebugEntries();
    }

    void UpdateDebugEntries()
    {
        if (debugEntriesParent == null || debugEntryPrefab == null)
            return;

        // Clear existing entries
        for (int i = debugEntryObjects.Count - 1; i >= 0; i--)
        {
            if (debugEntryObjects[i] != null)
                DestroyImmediate(debugEntryObjects[i]);
        }
        debugEntryObjects.Clear();

        // Create new entries
        foreach (var entry in currentDebugInfo.debugEntries)
        {
            GameObject entryObj = Instantiate(debugEntryPrefab, debugEntriesParent);
            entryObj.SetActive(true);

            // Set text content
            TextMeshProUGUI entryText = entryObj.GetComponent<TextMeshProUGUI>();
            if (entryText == null)
                entryText = entryObj.GetComponentInChildren<TextMeshProUGUI>();

            if (entryText != null)
            {
                entryText.text = $"{entry.key}: {entry.value}";
                entryText.color = entry.color;
            }

            debugEntryObjects.Add(entryObj);
        }
    }

    Color GetStateColor(string state)
    {
        switch (state.ToLower())
        {
            case "patrol": return Color.green;
            case "chase": return Color.red;
            case "attack": return Color.magenta;
            case "alert": return Color.yellow;
            case "search": return Color.blue;
            case "alerting": return Color.yellow;
            case "panicking": return Color.red;
            case "social": return Color.blue;
            default: return Color.white;
        }
    }

    public void SetShowDebugUI(bool show)
    {
        showDebugUI = show;
        if (debugPanel != null)
            debugPanel.gameObject.SetActive(show);
    }

    public void SetUpdateFrequency(float frequency)
    {
        updateFrequency = Mathf.Max(0.01f, frequency);
    }

    void OnDestroy()
    {
        // Unregister from DebugUIManager
        DebugUIManager.Instance?.UnregisterDebugUI(this);
        
        // Clean up debug entry objects
        for (int i = debugEntryObjects.Count - 1; i >= 0; i--)
        {
            if (debugEntryObjects[i] != null)
                DestroyImmediate(debugEntryObjects[i]);
        }
        debugEntryObjects.Clear();

        // Clean up debug panel
        if (debugPanel != null)
            DestroyImmediate(debugPanel.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // Draw connection line to debug panel
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + worldOffset);
        Gizmos.DrawWireSphere(transform.position + worldOffset, 0.5f);
    }
}