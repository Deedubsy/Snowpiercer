using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

[RequireComponent(typeof(GameLogger))]
public class InGameDebugConsole : MonoBehaviour
{
    private static InGameDebugConsole instance;
    private Canvas consoleCanvas;
    private ScrollRect scrollRect;
    private Text logText;
    
    [Header("UI Settings")]
    public int maxLogMessages = 200;
    public float consoleHeight = 0.5f;

    [Header("Filtering")]
    public LogLevel activeLogLevel = LogLevel.Info;
    
    private readonly List<LogMessage> logMessages = new List<LogMessage>();
    private readonly Dictionary<LogLevel, string> colorTags = new Dictionary<LogLevel, string>
    {
        { LogLevel.Info, "#FFFFFF" },      // White
        { LogLevel.Warning, "#FFFF00" },   // Yellow
        { LogLevel.Error, "#FF0000" },     // Red
        { LogLevel.Critical, "#FF00FF" }   // Magenta
    };

    void OnEnable()
    {
        GameLogger.OnMessageLogged += HandleLogMessage;
    }

    void OnDisable()
    {
        GameLogger.OnMessageLogged -= HandleLogMessage;
    }

    private void HandleLogMessage(LogMessage message)
    {
        logMessages.Add(message);
        if (logMessages.Count > maxLogMessages)
        {
            logMessages.RemoveAt(0);
        }
        UpdateLogDisplay();
    }
    
    private void UpdateLogDisplay()
    {
        if (logText == null) return;

        var sb = new StringBuilder();
        foreach (var message in logMessages)
        {
            if (message.Level >= activeLogLevel)
            {
                string color = colorTags[message.Level];
                sb.AppendLine($"<color={color}>{message.ToString()}</color>");
            }
        }
        logText.text = sb.ToString();
        
        // Scroll to bottom
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }

    #region UI Creation
    private void CreateConsoleUI()
    {
        if (consoleCanvas != null) return;
        
        // --- Canvas ---
        GameObject canvasGo = new GameObject("InGameDebugConsoleCanvas");
        canvasGo.transform.SetParent(transform);
        consoleCanvas = canvasGo.AddComponent<Canvas>();
        consoleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        consoleCanvas.sortingOrder = 30000; // Render on top of everything
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        
        // --- Background Panel ---
        GameObject panelGo = CreateUIElement("Background", canvasGo.transform);
        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1 - consoleHeight);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.75f);

        // --- Scroll View ---
        GameObject scrollGo = CreateUIElement("LogScroll", panelGo.transform);
        scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollGo.GetComponent<RectTransform>().anchorMin = new Vector2(0.02f, 0.1f);
        scrollGo.GetComponent<RectTransform>().anchorMax = new Vector2(0.98f, 0.98f);
        scrollGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        scrollGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        scrollRect.horizontal = false;

        // --- Viewport ---
        GameObject viewportGo = CreateUIElement("Viewport", scrollGo.transform);
        viewportGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = viewportGo.GetComponent<RectTransform>();

        // --- Content ---
        GameObject contentGo = CreateUIElement("Content", viewportGo.transform);
        VerticalLayoutGroup layoutGroup = contentGo.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childForceExpandHeight = false;
        ContentSizeFitter sizeFitter = contentGo.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentGo.GetComponent<RectTransform>();
        
        // --- Log Text ---
        GameObject textGo = CreateUIElement("LogText", contentGo.transform);
        logText = textGo.AddComponent<Text>();
        logText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        logText.fontSize = 14;
        logText.supportRichText = true;
        
        // --- Filter Buttons ---
        CreateFilterButtons(panelGo.transform);
        
        consoleCanvas.gameObject.SetActive(false);
    }
    
    private void CreateFilterButtons(Transform parent)
    {
        GameObject buttonPanel = CreateUIElement("ButtonPanel", parent);
        HorizontalLayoutGroup layout = buttonPanel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 5;
        layout.padding = new RectOffset(5, 5, 2, 2);
        RectTransform rect = buttonPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0.1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        CreateFilterButton("Info", LogLevel.Info, buttonPanel.transform);
        CreateFilterButton("Warning", LogLevel.Warning, buttonPanel.transform);
        CreateFilterButton("Error", LogLevel.Error, buttonPanel.transform);
        CreateFilterButton("Critical", LogLevel.Critical, buttonPanel.transform);
    }

    private void CreateFilterButton(string text, LogLevel level, Transform parent)
    {
        GameObject buttonGo = CreateUIElement($"Button_{text}", parent);
        buttonGo.AddComponent<LayoutElement>();
        Image img = buttonGo.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f);
        Button button = buttonGo.AddComponent<Button>();
        button.onClick.AddListener(() => SetFilterLevel(level));

        GameObject textGo = CreateUIElement("Text", buttonGo.transform);
        Text buttonText = textGo.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent);
        go.transform.localScale = Vector3.one;
        return go;
    }
    #endregion

    #region Static Controls
    public static void ToggleVisibility()
    {
        if (instance == null)
        {
            if (GameLogger.Instance != null)
            {
                instance = GameLogger.Instance.gameObject.AddComponent<InGameDebugConsole>();
                instance.CreateConsoleUI();
            }
            else
            {
                Debug.LogError("Cannot create InGameDebugConsole. GameLogger instance not found.");
                return;
            }
        }
        
        if (instance.consoleCanvas != null)
        {
            instance.consoleCanvas.gameObject.SetActive(!instance.consoleCanvas.gameObject.activeSelf);
            if(instance.consoleCanvas.gameObject.activeSelf)
            {
                instance.UpdateLogDisplay();
            }
        }
    }
    
    public void SetFilterLevel(LogLevel level)
    {
        activeLogLevel = level;
        UpdateLogDisplay();
    }
    #endregion
} 