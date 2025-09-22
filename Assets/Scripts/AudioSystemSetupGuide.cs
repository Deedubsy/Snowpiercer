using UnityEngine;
using UnityEngine.Audio;

/*
 * COMPREHENSIVE AUDIO SYSTEM SETUP GUIDE
 * =====================================
 * 
 * This system provides complete audio management for the vampire game, including:
 * 
 * 1. AudioManager - Core audio system with music, SFX, ambient sounds, and audio pooling
 * 2. AudioTrigger - Environmental audio triggers with conditions and events
 * 3. AudioMixerController - Dynamic audio mixing and effects
 * 
 * SETUP INSTRUCTIONS:
 * ===================
 * 
 * STEP 1: Create AudioManager GameObject
 * --------------------------------------
 * 1. Create an empty GameObject in your scene
 * 2. Name it "AudioManager"
 * 3. Add the AudioManager component to it
 * 4. Configure audio sources and mixer
 * 5. Add music tracks, sound effects, and ambient sounds
 * 6. Set up audio pools for performance
 * 
 * STEP 2: Set up AudioMixerController
 * -----------------------------------
 * 1. Create an empty GameObject in your scene
 * 2. Name it "AudioMixerController"
 * 3. Add the AudioMixerController component to it
 * 4. Assign your AudioMixer asset
 * 5. Configure audio effects and dynamic mixing
 * 6. Set up day/night, player state, and environmental audio
 * 
 * STEP 3: Configure Audio Triggers
 * --------------------------------
 * 1. Add AudioTrigger component to objects that should trigger sounds
 * 2. Configure trigger conditions (enter/exit, proximity, time, etc.)
 * 3. Set up spatial audio settings
 * 4. Configure debug visualization
 * 
 * STEP 4: Set up Audio Mixer
 * ---------------------------
 * 1. Create an AudioMixer asset in your project
 * 2. Set up mixer groups: Master, Music, SFX, Ambient, UI, Player
 * 3. Configure volume controls and effects
 * 4. Set up dynamic parameters for day/night, health, stealth, etc.
 * 
 * STEP 5: Configure Audio Assets
 * ------------------------------
 * 1. Import and organize audio files
 * 2. Set up AudioClipData for sound effects
 * 3. Configure MusicTrack for background music
 * 4. Set up AmbientSound for environmental audio
 * 5. Configure audio pools for frequently used sounds
 * 
 * USAGE EXAMPLES:
 * ===============
 * 
 * Playing Music:
 * --------------
 * AudioManager.Instance.PlayMusic("NightTheme", true);
 * AudioManager.Instance.StopMusic(3f);
 * 
 * Playing Sound Effects:
 * ---------------------
 * AudioManager.Instance.PlaySoundEffect("Footstep_Stone", position);
 * AudioManager.Instance.PlayPlayerSound("Player_Attack");
 * AudioManager.Instance.PlayUISound("UI_ButtonClick");
 * 
 * Playing Ambient Sounds:
 * ----------------------
 * AudioManager.Instance.PlayAmbientSound("Wind", position);
 * AudioManager.Instance.StopAmbientSound("Wind", 2f);
 * 
 * Dynamic Audio Control:
 * ---------------------
 * AudioMixerController mixer = FindObjectOfType<AudioMixerController>();
 * mixer.SetDayNightBlend(0.8f); // 80% night
 * mixer.SetPlayerHealthBlend(0.3f); // 30% health
 * mixer.SetPlayerStealthBlend(0.9f); // 90% stealth
 * 
 * Audio Triggers:
 * ---------------
 * // Trigger will automatically play sounds based on conditions
 * // Configure in inspector with trigger conditions
 * 
 * FEATURES:
 * ---------
 * 
 * Audio Management:
 * - Music system with crossfading and looping
 * - Sound effect system with spatial audio
 * - Ambient sound system with fade in/out
 * - Audio pooling for performance
 * - Volume control for all audio types
 * 
 * Dynamic Audio:
 * - Day/night audio transitions
 * - Player health-based audio effects
 * - Stealth-based audio modifications
 * - Environmental audio (indoor/outdoor)
 * - Combat intensity audio
 * 
 * Audio Triggers:
 * - Multiple trigger types (enter/exit, proximity, time)
 * - Conditional triggering (player type, health, stealth)
 * - Spatial audio with distance falloff
 * - Event system for custom actions
 * 
 * Performance Optimization:
 * - Audio source pooling
 * - Automatic cleanup of finished sounds
 * - Configurable pool sizes
 * - Memory-efficient audio management
 * 
 * INTEGRATION POINTS:
 * -------------------
 * 
 * Existing Systems:
 * - GameManager: Day/night cycle integration
 * - PlayerHealth: Health-based audio effects
 * - ShadowTrigger: Stealth-based audio
 * - RandomEventManager: Event-based audio
 * - SaveSystem: Audio settings persistence
 * 
 * Future Systems:
 * - Weather system: Rain, wind, storm audio
 * - Combat system: Battle music and effects
 * - NPC interactions: Voice lines and reactions
 * - Environmental storytelling: Ambient narrative audio
 * 
 * SETUP REQUIREMENTS:
 * -------------------
 * 
 * Audio Assets:
 * - Music tracks for different times and moods
 * - Sound effects for player actions and interactions
 * - Ambient sounds for environmental immersion
 * - UI sounds for interface feedback
 * - Voice lines for characters (future)
 * 
 * Audio Mixer:
 * - Master volume control
 * - Individual group controls (Music, SFX, Ambient, UI)
 * - Dynamic parameters for real-time mixing
 * - Audio effects (reverb, echo, filters)
 * 
 * Configuration:
 * - Pool sizes for different audio types
 * - Trigger conditions and ranges
 * - Volume levels and fade times
 * - Spatial audio settings
 * 
 * DEBUGGING:
 * ==========
 * 
 * Common Issues:
 * - "Audio clip not found": Check clip names and assignments
 * - "Pool empty": Increase pool sizes or check object return
 * - "No spatial audio": Check spatial blend and distance settings
 * - "Volume issues": Check mixer group assignments and volume levels
 * 
 * Debug Tools:
 * - Enable debugMode for detailed logging
 * - Use context menu options for testing
 * - Monitor audio statistics in runtime
 * - Visualize trigger areas with gizmos
 * 
 * PERFORMANCE CONSIDERATIONS:
 * ---------------------------
 * 
 * Audio Pooling:
 * - Configure appropriate pool sizes for your game
 * - Monitor pool usage and adjust as needed
 * - Use audio pooling for frequently played sounds
 * 
 * Spatial Audio:
 * - Limit the number of simultaneous spatial audio sources
 * - Use appropriate max distances for audio sources
 * - Consider culling distant audio sources
 * 
 * Memory Management:
 * - Compress audio files appropriately
 * - Use streaming for large music files
 * - Monitor audio memory usage
 * 
 * TROUBLESHOOTING:
 * ================
 * 
 * Audio Not Playing:
 * - Check AudioManager is in scene
 * - Verify audio clips are assigned
 * - Check volume levels and mixer settings
 * - Ensure audio sources are not muted
 * 
 * Performance Issues:
 * - Reduce pool sizes if memory is limited
 * - Limit simultaneous audio sources
 * - Use audio compression
 * - Monitor audio statistics
 * 
 * Integration Issues:
 * - Check component references
 * - Verify event connections
 * - Test audio triggers manually
 * - Check debug logs for errors
 */

public class AudioSystemSetupGuide : MonoBehaviour
{
    [Header("Setup Verification")]
    public bool verifyAudioManager = true;
    public bool verifyAudioMixerController = true;
    public bool verifyAudioTriggers = true;
    public bool verifyAudioMixer = true;

    [Header("Auto Setup")]
    public bool autoCreateMissingComponents = true;
    public bool autoConfigureAudioSystem = true;
    public bool createSampleAudioAssets = true;

    [Header("Sample Configuration")]
    public bool createSampleMusicTracks = true;
    public bool createSampleSoundEffects = true;
    public bool createSampleAmbientSounds = true;
    public bool createSampleAudioTriggers = true;

    void Start()
    {
        if (verifyAudioManager)
        {
            VerifyAudioManagerSetup();
        }

        if (verifyAudioMixerController)
        {
            VerifyAudioMixerControllerSetup();
        }

        if (verifyAudioTriggers)
        {
            VerifyAudioTriggersSetup();
        }

        if (verifyAudioMixer)
        {
            VerifyAudioMixerSetup();
        }

        if (autoCreateMissingComponents)
        {
            CreateMissingComponents();
        }

        if (autoConfigureAudioSystem)
        {
            AutoConfigureAudioSystem();
        }

        if (createSampleAudioAssets)
        {
            CreateSampleAudioAssets();
        }
    }

    void VerifyAudioManagerSetup()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogWarning("AudioManager not found in scene! Creating one...");
            if (autoCreateMissingComponents)
            {
                CreateAudioManager();
            }
        }
        else
        {
            Debug.Log("AudioManager found and ready.");
        }
    }

    void VerifyAudioMixerControllerSetup()
    {
        AudioMixerController mixerController = FindObjectOfType<AudioMixerController>();
        if (mixerController == null)
        {
            Debug.LogWarning("AudioMixerController not found in scene! Creating one...");
            if (autoCreateMissingComponents)
            {
                CreateAudioMixerController();
            }
        }
        else
        {
            Debug.Log("AudioMixerController found and ready.");
        }
    }

    void VerifyAudioTriggersSetup()
    {
        AudioTrigger[] audioTriggers = FindObjectsOfType<AudioTrigger>();
        if (audioTriggers.Length == 0)
        {
            Debug.LogWarning("No AudioTriggers found in scene!");
            if (createSampleAudioTriggers)
            {
                CreateSampleAudioTriggers();
            }
        }
        else
        {
            Debug.Log($"Found {audioTriggers.Length} AudioTriggers in scene.");
        }
    }

    void VerifyAudioMixerSetup()
    {
        AudioMixer[] audioMixers = FindObjectsOfType<AudioMixer>();
        if (audioMixers.Length == 0)
        {
            Debug.LogWarning("No AudioMixer found in scene! Please create an AudioMixer asset.");
        }
        else
        {
            Debug.Log($"Found {audioMixers.Length} AudioMixer(s) in scene.");
        }
    }

    void CreateMissingComponents()
    {
        if (FindObjectOfType<AudioManager>() == null)
        {
            CreateAudioManager();
        }

        if (FindObjectOfType<AudioMixerController>() == null)
        {
            CreateAudioMixerController();
        }
    }

    void CreateAudioManager()
    {
        GameObject audioManagerGO = new GameObject("AudioManager");
        AudioManager audioManager = audioManagerGO.AddComponent<AudioManager>();

        // Set default configuration
        audioManager.debugMode = false;
        audioManager.useAudioPooling = true;
        audioManager.enableDynamicAudio = true;

        Debug.Log("Created AudioManager GameObject");
    }

    void CreateAudioMixerController()
    {
        GameObject mixerControllerGO = new GameObject("AudioMixerController");
        AudioMixerController mixerController = mixerControllerGO.AddComponent<AudioMixerController>();

        // Set default configuration
        mixerController.debugMode = false;
        mixerController.autoUpdate = true;
        mixerController.enableDayNightAudio = true;
        mixerController.enablePlayerStateAudio = true;
        mixerController.enableEnvironmentalAudio = true;

        Debug.Log("Created AudioMixerController GameObject");
    }

    void AutoConfigureAudioSystem()
    {
        // This would auto-configure the audio system based on scene analysis
        // For now, just log that manual configuration is needed
        Debug.Log("Auto-configure audio system: Manual configuration required. See setup guide above.");
    }

    void CreateSampleAudioAssets()
    {
        if (createSampleMusicTracks)
        {
            CreateSampleMusicTracks();
        }

        if (createSampleSoundEffects)
        {
            CreateSampleSoundEffects();
        }

        if (createSampleAmbientSounds)
        {
            CreateSampleAmbientSounds();
        }
    }

    void CreateSampleMusicTracks()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager == null) return;

        // Add sample music tracks
        MusicTrack dayTrack = new MusicTrack
        {
            trackName = "DayTheme",
            volume = 0.8f,
            fadeInTime = 3f,
            fadeOutTime = 3f,
            crossfade = true
        };

        MusicTrack nightTrack = new MusicTrack
        {
            trackName = "NightTheme",
            volume = 0.7f,
            fadeInTime = 4f,
            fadeOutTime = 4f,
            crossfade = true
        };

        audioManager.musicTracks.Add(dayTrack);
        audioManager.musicTracks.Add(nightTrack);

        Debug.Log("Created sample music tracks");
    }

    void CreateSampleSoundEffects()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager == null) return;

        // Add sample sound effects
        AudioClipData footstep = new AudioClipData
        {
            clipName = "Footstep_Stone",
            volume = 0.6f,
            spatialBlend = 1f,
            maxDistance = 15f
        };

        AudioClipData attack = new AudioClipData
        {
            clipName = "Player_Attack",
            volume = 0.8f,
            spatialBlend = 0f
        };

        AudioClipData uiClick = new AudioClipData
        {
            clipName = "UI_ButtonClick",
            volume = 0.5f,
            spatialBlend = 0f
        };

        audioManager.soundEffects.Add(footstep);
        audioManager.soundEffects.Add(attack);
        audioManager.soundEffects.Add(uiClick);

        Debug.Log("Created sample sound effects");
    }

    void CreateSampleAmbientSounds()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager == null) return;

        // Add sample ambient sounds
        AmbientSound wind = new AmbientSound
        {
            ambientName = "Wind",
            volume = 0.4f,
            fadeInTime = 5f,
            fadeOutTime = 5f,
            radius = 25f
        };

        AmbientSound crickets = new AmbientSound
        {
            ambientName = "Crickets",
            volume = 0.3f,
            fadeInTime = 3f,
            fadeOutTime = 3f,
            radius = 20f
        };

        audioManager.ambientSounds.Add(wind);
        audioManager.ambientSounds.Add(crickets);

        Debug.Log("Created sample ambient sounds");
    }

    void CreateSampleAudioTriggers()
    {
        // Create sample audio triggers in the scene
        CreateSampleTrigger("DoorTrigger", "Door_Creak", Vector3.zero);
        CreateSampleTrigger("WaterTrigger", "Water_Splash", new Vector3(5f, 0f, 5f));
        CreateSampleTrigger("WindTrigger", "Wind_Gust", new Vector3(-5f, 0f, -5f));
    }

    void CreateSampleTrigger(string triggerName, string soundName, Vector3 position)
    {
        GameObject triggerGO = new GameObject(triggerName);
        triggerGO.transform.position = position;

        AudioTrigger audioTrigger = triggerGO.AddComponent<AudioTrigger>();

        AudioTriggerCondition condition = new AudioTriggerCondition
        {
            triggerType = AudioTriggerCondition.TriggerType.OnEnter,
            soundName = soundName,
            playOnce = false,
            requirePlayer = true
        };

        audioTrigger.triggerConditions.Add(condition);
        audioTrigger.useCollider = true;
        audioTrigger.debugMode = false;

        // Add collider
        SphereCollider collider = triggerGO.AddComponent<SphereCollider>();
        collider.radius = 3f;
        collider.isTrigger = true;

        Debug.Log($"Created sample audio trigger: {triggerName}");
    }

    // Debug methods
    [ContextMenu("Verify All Audio Systems")]
    public void VerifyAllSystems()
    {
        VerifyAudioManagerSetup();
        VerifyAudioMixerControllerSetup();
        VerifyAudioTriggersSetup();
        VerifyAudioMixerSetup();

        Debug.Log("=== Audio System Verification Complete ===");
    }

    [ContextMenu("Create All Missing Components")]
    public void CreateAllMissingComponentsFromContext()
    {
        CreateMissingComponents();
        Debug.Log("=== Created all missing audio components ===");
    }

    [ContextMenu("Create Sample Audio Assets")]
    public void CreateSampleAudioAssetsFromContext()
    {
        CreateSampleAudioAssets();
        Debug.Log("=== Created sample audio assets ===");
    }

    [ContextMenu("Log Audio System Status")]
    public void LogAudioSystemStatus()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        AudioMixerController mixerController = FindObjectOfType<AudioMixerController>();
        AudioTrigger[] audioTriggers = FindObjectsOfType<AudioTrigger>();

        Debug.Log("=== Audio System Status ===");
        Debug.Log($"AudioManager: {(audioManager != null ? "Ready" : "Missing")}");
        Debug.Log($"AudioMixerController: {(mixerController != null ? "Ready" : "Missing")}");
        Debug.Log($"AudioTriggers: {audioTriggers.Length}");
        Debug.Log($"Music Tracks: {(audioManager != null ? audioManager.musicTracks.Count : 0)}");
        Debug.Log($"Sound Effects: {(audioManager != null ? audioManager.soundEffects.Count : 0)}");
        Debug.Log($"Ambient Sounds: {(audioManager != null ? audioManager.ambientSounds.Count : 0)}");
        Debug.Log("===========================");
    }

    [ContextMenu("Test Audio System")]
    public void TestAudioSystem()
    {
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if (audioManager != null)
        {
            // Test music
            audioManager.PlayMusic("DayTheme");

            // Test sound effect
            audioManager.PlaySoundEffect("UI_ButtonClick");

            // Test ambient sound
            audioManager.PlayAmbientSound("Wind", transform.position);

            Debug.Log("Audio system test completed");
        }
        else
        {
            Debug.LogWarning("AudioManager not found for testing");
        }
    }
}