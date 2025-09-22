using UnityEngine;

/// <summary>
/// Random Event System Setup Guide
/// 
/// This system adds dynamic, unpredictable events to your vampire game that affect
/// citizen behavior, guard alertness, and world conditions.
/// 
/// SETUP STEPS:
/// 
/// 1. Create RandomEventManager GameObject:
///    - Create an empty GameObject named "RandomEventManager"
///    - Add the RandomEventManager script to it
///    - Add an AudioSource component for event audio
/// 
/// 2. Create Example Events:
///    - Add the ExampleRandomEvents script to any GameObject
///    - Right-click the script in Inspector and select "Create Example Events"
///    - This will create 6 example events in Assets/Scripts/Events/
/// 
/// 3. Configure RandomEventManager:
///    - Drag the created event assets into the "Available Events" list
///    - Set "Max Active Events" (recommended: 2-3)
///    - Enable "Enable Random Events" and "Debug Mode" for testing
/// 
/// 4. Add to GameManager:
///    - Drag the RandomEventManager to the GameManager's "Random Event Manager" field
///    - Or it will auto-find it at runtime
/// 
/// 5. Create Event UI (Optional):
///    - Create a UI Canvas with an EventUI script
///    - Create an event panel prefab with Text components:
///      * "EventName" - Shows event title
///      * "EventDescription" - Shows event description  
///      * "TimeRemaining" - Shows countdown timer
///      * "ProgressBar" - Shows event progress (optional)
///    - Assign the prefab and container to EventUI
/// 
/// 6. Test the System:
///    - Play the game and watch for events to trigger
///    - Check console for debug messages
///    - Events will affect citizen movement, guard behavior, and world conditions
/// 
/// EVENT TYPES INCLUDED:
/// - Curfew: Citizens go inside, guards become more alert
/// - Festival: More citizens outside, increased activity
/// - Storm: Reduced visibility and movement speed
/// - Vampire Hunter: Special NPC spawns, high guard alertness
/// - Market Day: More merchants, normal guard behavior
/// - Guard Shift Change: Temporary patrol disruption
/// 
/// CUSTOMIZATION:
/// - Modify event timing, effects, and triggers in the ScriptableObject assets
/// - Add new event types by creating new RandomEvent assets
/// - Implement special effects in RandomEventManager (vampire hunter, storm, etc.)
/// - Add audio clips and visual effects to events
/// </summary>
public class RandomEventSetupGuide : MonoBehaviour
{
    [Header("Setup Status")]
    [SerializeField] private bool randomEventManagerFound = false;
    [SerializeField] private bool gameManagerFound = false;
    [SerializeField] private int availableEventsCount = 0;
    
    void Start()
    {
        CheckSetupStatus();
    }
    
    void CheckSetupStatus()
    {
        // Check if RandomEventManager exists
        RandomEventManager eventManager = FindObjectOfType<RandomEventManager>();
        randomEventManagerFound = eventManager != null;
        
        if (randomEventManagerFound)
        {
            availableEventsCount = eventManager.availableEvents.Count;
        }
        
        // Check if GameManager exists
        gameManagerFound = GameManager.instance != null;
        
        // Log setup status
        if (!randomEventManagerFound)
        {
            Debug.LogWarning("RandomEventManager not found! Please create one and add it to the scene.");
        }
        else if (availableEventsCount == 0)
        {
            Debug.LogWarning("No events configured in RandomEventManager! Use ExampleRandomEvents to create some.");
        }
        else
        {
            Debug.Log($"Random Event System ready! {availableEventsCount} events available.");
        }
    }
    
    [ContextMenu("Check Setup Status")]
    public void CheckSetup()
    {
        CheckSetupStatus();
    }
} 