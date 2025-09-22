using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class EventUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject eventPanelPrefab;
    public Transform eventContainer;
    public Text noEventsText;
    
    [Header("Settings")]
    public float updateInterval = 1f;
    
    private RandomEventManager eventManager;
    private List<GameObject> activeEventPanels = new List<GameObject>();
    private float lastUpdate;
    
    void Start()
    {
        eventManager = FindObjectOfType<RandomEventManager>();
        if (noEventsText != null)
        {
            noEventsText.gameObject.SetActive(true);
        }
    }
    
    void Update()
    {
        if (eventManager == null || Time.time - lastUpdate < updateInterval) return;
        
        UpdateEventDisplay();
        lastUpdate = Time.time;
    }
    
    void UpdateEventDisplay()
    {
        List<ActiveEvent> activeEvents = eventManager.GetActiveEvents();
        
        // Clear existing panels
        ClearEventPanels();
        
        // Show/hide no events text
        if (noEventsText != null)
        {
            noEventsText.gameObject.SetActive(activeEvents.Count == 0);
        }
        
        // Create panels for active events
        foreach (ActiveEvent activeEvent in activeEvents)
        {
            CreateEventPanel(activeEvent);
        }
    }
    
    void CreateEventPanel(ActiveEvent activeEvent)
    {
        if (eventPanelPrefab == null || eventContainer == null) return;
        
        GameObject panel = Instantiate(eventPanelPrefab, eventContainer);
        activeEventPanels.Add(panel);
        
        // Set event name
        Text nameText = panel.transform.Find("EventName")?.GetComponent<Text>();
        if (nameText != null)
        {
            nameText.text = activeEvent.eventData.eventName;
        }
        
        // Set event description
        Text descText = panel.transform.Find("EventDescription")?.GetComponent<Text>();
        if (descText != null)
        {
            descText.text = activeEvent.eventData.description;
        }
        
        // Set remaining time
        Text timeText = panel.transform.Find("TimeRemaining")?.GetComponent<Text>();
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(activeEvent.remainingDuration / 60f);
            int seconds = Mathf.FloorToInt(activeEvent.remainingDuration % 60f);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        
        // Set progress bar if available
        Slider progressSlider = panel.transform.Find("ProgressBar")?.GetComponent<Slider>();
        if (progressSlider != null)
        {
            float progress = 1f - (activeEvent.remainingDuration / activeEvent.eventData.duration);
            progressSlider.value = progress;
        }
    }
    
    void ClearEventPanels()
    {
        foreach (GameObject panel in activeEventPanels)
        {
            if (panel != null)
            {
                Destroy(panel);
            }
        }
        activeEventPanels.Clear();
    }
    
    void OnDestroy()
    {
        ClearEventPanels();
    }
} 