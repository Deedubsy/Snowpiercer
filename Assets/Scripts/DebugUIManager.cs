using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DebugUIManager : MonoBehaviour
{
    public static DebugUIManager Instance { get; private set; }
    
    [Header("Canvas Setup")]
    public Canvas worldCanvas;
    public Camera debugCamera;
    public float canvasScale = 0.01f;
    
    [Header("Debug Panel Prefab")]
    public GameObject debugPanelPrefab;
    
    [Header("Global Controls")]
    public bool showAllDebugUI = true;
    public KeyCode toggleDebugKey = KeyCode.F1;
    public float globalUpdateFrequency = 0.1f;
    
    private List<AIDebugUI> activeDebugUIs = new List<AIDebugUI>();
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupWorldCanvas();
            CreateDebugPanelPrefab();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void SetupWorldCanvas()
    {
        if (worldCanvas == null)
        {
            // Create world space canvas
            GameObject canvasObj = new GameObject("DebugWorldCanvas");
            canvasObj.transform.SetParent(transform);
            
            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            worldCanvas.worldCamera = Camera.main;
            
            // Set up canvas scaler
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            
            // Add graphic raycaster for UI interactions
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // Set canvas scale
            canvasObj.transform.localScale = Vector3.one * canvasScale;
        }
        
        if (debugCamera == null)
        {
            debugCamera = Camera.main;
            if (worldCanvas != null)
                worldCanvas.worldCamera = debugCamera;
        }
    }
    
    void CreateDebugPanelPrefab()
    {
        if (debugPanelPrefab != null) return;
        
        // Create debug panel prefab programmatically
        GameObject panelObj = new GameObject("DebugPanel");
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(300, 200);
        
        // Add background
        Image bgImage = panelObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);
        
        // Add vertical layout group
        VerticalLayoutGroup layout = panelObj.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 5f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        
        // Add content size fitter
        ContentSizeFitter fitter = panelObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Entity Name
        CreateTextElement(panelObj.transform, "EntityName", "Entity Name", 16, FontStyles.Bold);
        
        // Current State
        CreateTextElement(panelObj.transform, "CurrentState", "State: Unknown", 14, FontStyles.Normal);
        
        // Detection Slider
        CreateDetectionSlider(panelObj.transform);
        
        // Detection Text
        CreateTextElement(panelObj.transform, "DetectionText", "Detection: 0%", 12, FontStyles.Normal);
        
        // Debug Entries Parent
        GameObject entriesParent = new GameObject("DebugEntries");
        entriesParent.transform.SetParent(panelObj.transform);
        RectTransform entriesRect = entriesParent.AddComponent<RectTransform>();
        entriesRect.anchorMin = Vector2.zero;
        entriesRect.anchorMax = Vector2.one;
        entriesRect.sizeDelta = Vector2.zero;
        entriesRect.anchoredPosition = Vector2.zero;
        
        VerticalLayoutGroup entriesLayout = entriesParent.AddComponent<VerticalLayoutGroup>();
        entriesLayout.childAlignment = TextAnchor.UpperLeft;
        entriesLayout.childControlWidth = true;
        entriesLayout.childControlHeight = false;
        entriesLayout.childForceExpandWidth = true;
        entriesLayout.spacing = 2f;
        
        // Debug Entry Prefab
        CreateTextElement(panelObj.transform, "DebugEntryPrefab", "Key: Value", 10, FontStyles.Normal);
        
        debugPanelPrefab = panelObj;
        
        Debug.Log("[DebugUIManager] Created debug panel prefab");
    }
    
    TextMeshProUGUI CreateTextElement(Transform parent, string name, string text, int fontSize, FontStyles style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(0, fontSize + 4);
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = style;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Left;
        
        // Add layout element
        LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = fontSize + 4;
        
        return textComponent;
    }
    
    void CreateDetectionSlider(Transform parent)
    {
        GameObject sliderObj = new GameObject("DetectionSlider");
        sliderObj.transform.SetParent(parent);
        
        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0, 20);
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        fillAreaRect.anchoredPosition = Vector2.zero;
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.green;
        
        // Assign to slider
        slider.fillRect = fillRect;
        
        // Add layout element
        LayoutElement layoutElement = sliderObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 20;
    }
    
    void Update()
    {
        // Handle global debug toggle
        if (Input.GetKeyDown(toggleDebugKey))
        {
            ToggleAllDebugUI();
        }
        
        // Update camera reference if needed
        if (debugCamera == null && Camera.main != null)
        {
            debugCamera = Camera.main;
            if (worldCanvas != null)
                worldCanvas.worldCamera = debugCamera;
        }
    }
    
    public void RegisterDebugUI(AIDebugUI debugUI)
    {
        if (!activeDebugUIs.Contains(debugUI))
        {
            activeDebugUIs.Add(debugUI);
            debugUI.SetShowDebugUI(showAllDebugUI);
            debugUI.SetUpdateFrequency(globalUpdateFrequency);
        }
    }
    
    public void UnregisterDebugUI(AIDebugUI debugUI)
    {
        activeDebugUIs.Remove(debugUI);
    }
    
    public void ToggleAllDebugUI()
    {
        showAllDebugUI = !showAllDebugUI;
        
        foreach (var debugUI in activeDebugUIs)
        {
            if (debugUI != null)
                debugUI.SetShowDebugUI(showAllDebugUI);
        }
        
        Debug.Log($"[DebugUIManager] Debug UI toggled: {(showAllDebugUI ? "ON" : "OFF")}");
    }
    
    public void SetGlobalUpdateFrequency(float frequency)
    {
        globalUpdateFrequency = Mathf.Max(0.01f, frequency);
        
        foreach (var debugUI in activeDebugUIs)
        {
            if (debugUI != null)
                debugUI.SetUpdateFrequency(globalUpdateFrequency);
        }
    }
    
    public Canvas GetWorldCanvas()
    {
        return worldCanvas;
    }
    
    public GameObject GetDebugPanelPrefab()
    {
        return debugPanelPrefab;
    }
    
    void OnGUI()
    {
        // Show simple instructions
        GUI.Box(new Rect(10, Screen.height - 60, 300, 50), 
            $"Press {toggleDebugKey} to toggle AI Debug UI\n" +
            $"Debug UI: {(showAllDebugUI ? "ON" : "OFF")} | Active UIs: {activeDebugUIs.Count}");
    }
}