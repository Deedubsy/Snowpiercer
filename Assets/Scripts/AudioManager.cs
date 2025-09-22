using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class AudioClipData
{
    public string clipName;
    public AudioClip clip;
    public AudioMixerGroup mixerGroup;
    [Range(0f, 1f)]
    public float volume = 1f;
    [Range(0.1f, 3f)]
    public float pitch = 1f;
    public bool loop = false;
    public bool spatial = false;
    [Range(0f, 5f)]
    public float spatialBlend = 1f;
    [Range(0f, 25f)]
    public float maxDistance = 10f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
}

[System.Serializable]
public class MusicTrack
{
    public string trackName;
    public AudioClip clip;
    public AudioMixerGroup mixerGroup;
    [Range(0f, 1f)]
    public float volume = 1f;
    public bool loop = true;
    public float fadeInTime = 2f;
    public float fadeOutTime = 2f;
    public bool crossfade = true;
}

[System.Serializable]
public class AmbientSound
{
    public string ambientName;
    public AudioClip clip;
    public AudioMixerGroup mixerGroup;
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public bool loop = true;
    public float fadeInTime = 3f;
    public float fadeOutTime = 3f;
    public Vector3 position;
    public float radius = 20f;
    public bool followPlayer = false;
}

[System.Serializable]
public class AudioPool
{
    public string poolName;
    public AudioClipData clipData;
    public int initialPoolSize = 5;
    public int maxPoolSize = 20;
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource ambientSource;
    public AudioSource uiSource;
    public AudioSource playerSource;
    
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;
    
    [Header("Music System")]
    public List<MusicTrack> musicTracks = new List<MusicTrack>();
    public MusicTrack currentMusicTrack;
    public MusicTrack previousMusicTrack;
    
    [Header("Sound Effects")]
    public List<AudioClipData> soundEffects = new List<AudioClipData>();
    
    [Header("Ambient Sounds")]
    public List<AmbientSound> ambientSounds = new List<AmbientSound>();
    public List<AudioSource> activeAmbientSources = new List<AudioSource>();
    
    [Header("Audio Pools")]
    public List<AudioPool> audioPools = new List<AudioPool>();
    private Dictionary<string, Queue<AudioSource>> audioSourcePools = new Dictionary<string, Queue<AudioSource>>();
    
    [Header("Dynamic Audio")]
    public bool enableDynamicAudio = true;
    public float dayNightBlend = 0f; // 0 = day, 1 = night
    public float playerHealthBlend = 1f; // 1 = full health, 0 = low health
    public float playerStealthBlend = 0f; // 0 = visible, 1 = hidden
    
    [Header("Audio Settings")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float musicVolume = 0.8f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float ambientVolume = 0.6f;
    [Range(0f, 1f)]
    public float uiVolume = 1f;
    
    [Header("Advanced Settings")]
    public bool useAudioPooling = true;
    public bool enableSpatialAudio = true;
    public bool enableReverb = true;
    public bool enableEcho = false;
    public float reverbLevel = 0.5f;
    public float echoLevel = 0.3f;
    
    [Header("Debug")]
    public bool debugMode = false;
    public bool showAudioSources = false;

    // Private variables
    private Dictionary<string, AudioClipData> soundEffectDict = new Dictionary<string, AudioClipData>();
    private Dictionary<string, MusicTrack> musicTrackDict = new Dictionary<string, MusicTrack>();
    private Dictionary<string, AmbientSound> ambientSoundDict = new Dictionary<string, AmbientSound>();
    private Coroutine musicFadeCoroutine;
    private Coroutine ambientFadeCoroutine;
    private Transform playerTransform;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            GameLogger.LogWarning(LogCategory.Audio, "Another instance of AudioManager was found and destroyed.", this);
            Destroy(gameObject);
        }
    }

    void InitializeAudioManager()
    {
        // Create audio sources if they don't exist
        CreateAudioSources();
        
        // Initialize dictionaries
        InitializeDictionaries();
        
        // Initialize audio pools
        if (useAudioPooling)
        {
            InitializeAudioPools();
        }
        
        // Set initial volumes
        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);
        SetAmbientVolume(ambientVolume);
        SetUIVolume(uiVolume);
        
        if (debugMode)
        {
            GameLogger.Log(LogCategory.Audio, "AudioManager initialized successfully", this);
        }
    }

    void CreateAudioSources()
    {
        if (musicSource == null)
        {
            GameObject musicGO = new GameObject("MusicSource");
            musicGO.transform.SetParent(transform);
            musicSource = musicGO.AddComponent<AudioSource>();
            musicSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("Music")[0];
            musicSource.loop = true;
        }

        if (ambientSource == null)
        {
            GameObject ambientGO = new GameObject("AmbientSource");
            ambientGO.transform.SetParent(transform);
            ambientSource = ambientGO.AddComponent<AudioSource>();
            ambientSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("Ambient")[0];
            ambientSource.loop = true;
        }

        if (uiSource == null)
        {
            GameObject uiGO = new GameObject("UISource");
            uiGO.transform.SetParent(transform);
            uiSource = uiGO.AddComponent<AudioSource>();
            uiSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("UI")[0];
        }

        if (playerSource == null)
        {
            GameObject playerGO = new GameObject("PlayerSource");
            playerGO.transform.SetParent(transform);
            playerSource = playerGO.AddComponent<AudioSource>();
            playerSource.outputAudioMixerGroup = audioMixer?.FindMatchingGroups("Player")[0];
            playerSource.spatialBlend = 0f; // 2D for player sounds
        }
    }

    void InitializeDictionaries()
    {
        // Initialize sound effects dictionary
        foreach (var sfx in soundEffects)
        {
            if (!string.IsNullOrEmpty(sfx.clipName) && sfx.clip != null)
            {
                soundEffectDict[sfx.clipName] = sfx;
            }
        }

        // Initialize music tracks dictionary
        foreach (var track in musicTracks)
        {
            if (!string.IsNullOrEmpty(track.trackName) && track.clip != null)
            {
                musicTrackDict[track.trackName] = track;
            }
        }

        // Initialize ambient sounds dictionary
        foreach (var ambient in ambientSounds)
        {
            if (!string.IsNullOrEmpty(ambient.ambientName) && ambient.clip != null)
            {
                ambientSoundDict[ambient.ambientName] = ambient;
            }
        }
    }

    void InitializeAudioPools()
    {
        foreach (var pool in audioPools)
        {
            if (string.IsNullOrEmpty(pool.poolName) || pool.clipData.clip == null) continue;

            Queue<AudioSource> sourcePool = new Queue<AudioSource>();
            
            for (int i = 0; i < pool.initialPoolSize; i++)
            {
                AudioSource source = CreatePooledAudioSource(pool.clipData);
                sourcePool.Enqueue(source);
            }

            audioSourcePools[pool.poolName] = sourcePool;
        }
    }

    AudioSource CreatePooledAudioSource(AudioClipData clipData)
    {
        GameObject sourceGO = new GameObject($"PooledAudio_{clipData.clipName}");
        sourceGO.transform.SetParent(transform);
        
        AudioSource source = sourceGO.AddComponent<AudioSource>();
        source.clip = clipData.clip;
        source.volume = clipData.volume;
        source.pitch = clipData.pitch;
        source.loop = clipData.loop;
        source.spatialBlend = clipData.spatialBlend;
        source.maxDistance = clipData.maxDistance;
        source.rolloffMode = clipData.rolloffMode;
        source.outputAudioMixerGroup = clipData.mixerGroup;
        
        return source;
    }

    // Music System
    public void PlayMusic(string trackName, bool crossfade = true)
    {
        if (!musicTrackDict.ContainsKey(trackName))
        {
            GameLogger.LogWarning(LogCategory.Audio, $"Music track '{trackName}' not found!", this);
            return;
        }

        MusicTrack newTrack = musicTrackDict[trackName];
        
        if (currentMusicTrack == newTrack && musicSource.isPlaying)
        {
            return; // Already playing this track
        }

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(FadeMusic(newTrack, crossfade));
    }

    public void StopMusic(float fadeOutTime = 2f)
    {
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(FadeOutMusic(fadeOutTime));
    }

    IEnumerator FadeMusic(MusicTrack newTrack, bool crossfade)
    {
        previousMusicTrack = currentMusicTrack;
        currentMusicTrack = newTrack;

        if (crossfade && musicSource.isPlaying)
        {
            // Crossfade
            float fadeTime = Mathf.Max(newTrack.fadeInTime, previousMusicTrack?.fadeOutTime ?? 2f);
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;
                
                musicSource.volume = Mathf.Lerp(musicVolume, 0f, t);
                yield return null;
            }
        }

        // Set new track
        musicSource.clip = newTrack.clip;
        musicSource.volume = 0f;
        musicSource.outputAudioMixerGroup = newTrack.mixerGroup;
        musicSource.loop = newTrack.loop;
        musicSource.Play();

        // Fade in
        float fadeInTime = newTrack.fadeInTime;
        float elapsed2 = 0f;

        while (elapsed2 < fadeInTime)
        {
            elapsed2 += Time.deltaTime;
            float t = elapsed2 / fadeInTime;
            
            musicSource.volume = Mathf.Lerp(0f, newTrack.volume * musicVolume, t);
            yield return null;
        }

        musicSource.volume = newTrack.volume * musicVolume;
    }

    IEnumerator FadeOutMusic(float fadeOutTime)
    {
        float startVolume = musicSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutTime;
            
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        musicSource.Stop();
        currentMusicTrack = null;
    }

    // Sound Effects System
    public AudioSource PlaySoundEffect(string effectName, Vector3 position = default)
    {
        if (!soundEffectDict.ContainsKey(effectName))
        {
            GameLogger.LogWarning(LogCategory.Audio, $"Sound effect '{effectName}' not found!", this);
            return null;
        }

        AudioClipData clipData = soundEffectDict[effectName];
        
        if (useAudioPooling && audioSourcePools.ContainsKey(effectName))
        {
            return PlayPooledSoundEffect(effectName, position);
        }
        else
        {
            return PlayDirectSoundEffect(clipData, position);
        }
    }

    AudioSource PlayPooledSoundEffect(string effectName, Vector3 position)
    {
        Queue<AudioSource> pool = audioSourcePools[effectName];
        AudioSource source = null;

        if (pool.Count > 0)
        {
            source = pool.Dequeue();
        }
        else
        {
            // Create new source if pool is empty
            AudioClipData clipData = soundEffectDict[effectName];
            source = CreatePooledAudioSource(clipData);
        }

        source.transform.position = position;
        source.gameObject.SetActive(true);
        source.Play();

        // Return to pool when finished
        StartCoroutine(ReturnToPool(source, effectName, source.clip.length));

        return source;
    }

    AudioSource PlayDirectSoundEffect(AudioClipData clipData, Vector3 position)
    {
        GameObject sourceGO = new GameObject($"SFX_{clipData.clipName}");
        sourceGO.transform.position = position;
        
        AudioSource source = sourceGO.AddComponent<AudioSource>();
        source.clip = clipData.clip;
        source.volume = clipData.volume * sfxVolume;
        source.pitch = clipData.pitch;
        source.loop = clipData.loop;
        source.spatialBlend = clipData.spatialBlend;
        source.maxDistance = clipData.maxDistance;
        source.rolloffMode = clipData.rolloffMode;
        source.outputAudioMixerGroup = clipData.mixerGroup;
        
        source.Play();

        if (!clipData.loop)
        {
            Destroy(sourceGO, clipData.clip.length + 0.1f);
        }

        return source;
    }

    IEnumerator ReturnToPool(AudioSource source, string poolName, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (source != null && audioSourcePools.ContainsKey(poolName))
        {
            source.Stop();
            source.gameObject.SetActive(false);
            audioSourcePools[poolName].Enqueue(source);
        }
    }

    // Ambient Sounds System
    public void PlayAmbientSound(string ambientName, Vector3 position = default)
    {
        if (!ambientSoundDict.ContainsKey(ambientName))
        {
            GameLogger.LogWarning(LogCategory.Audio, $"Ambient sound '{ambientName}' not found!", this);
            return;
        }

        AmbientSound ambient = ambientSoundDict[ambientName];
        
        GameObject sourceGO = new GameObject($"Ambient_{ambientName}");
        sourceGO.transform.position = position;
        
        AudioSource source = sourceGO.AddComponent<AudioSource>();
        source.clip = ambient.clip;
        source.volume = 0f; // Start at 0 for fade in
        source.pitch = 1f;
        source.loop = ambient.loop;
        source.spatialBlend = 1f; // 3D ambient sounds
        source.maxDistance = ambient.radius;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.outputAudioMixerGroup = ambient.mixerGroup;
        
        source.Play();
        activeAmbientSources.Add(source);

        // Fade in
        StartCoroutine(FadeAmbientSound(source, 0f, ambient.volume * ambientVolume, ambient.fadeInTime));
    }

    public void StopAmbientSound(string ambientName, float fadeOutTime = 3f)
    {
        for (int i = activeAmbientSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeAmbientSources[i];
            if (source != null && source.name.Contains(ambientName))
            {
                StartCoroutine(FadeOutAmbientSound(source, fadeOutTime));
                activeAmbientSources.RemoveAt(i);
            }
        }
    }

    IEnumerator FadeAmbientSound(AudioSource source, float fromVolume, float toVolume, float duration)
    {
        float elapsed = 0f;
        float startVolume = fromVolume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            source.volume = Mathf.Lerp(startVolume, toVolume, t);
            yield return null;
        }

        source.volume = toVolume;
    }

    IEnumerator FadeOutAmbientSound(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        if (source != null)
        {
            Destroy(source.gameObject);
        }
    }

    // Player Audio System
    public void PlayPlayerSound(string effectName)
    {
        PlaySoundEffect(effectName, playerSource.transform.position);
    }

    public void PlayPlayerFootstep()
    {
        // Random footstep sound based on surface
        string[] footstepSounds = { "Footstep_Stone", "Footstep_Wood", "Footstep_Grass" };
        string randomFootstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
        PlayPlayerSound(randomFootstep);
    }

    public void PlayPlayerAttack()
    {
        PlayPlayerSound("Player_Attack");
    }

    public void PlayPlayerDamage()
    {
        PlayPlayerSound("Player_Damage");
    }

    public void PlayPlayerDeath()
    {
        PlayPlayerSound("Player_Death");
    }

    // UI Audio System
    public void PlayUISound(string effectName)
    {
        if (!soundEffectDict.ContainsKey(effectName))
        {
            GameLogger.LogWarning(LogCategory.Audio, $"UI sound effect '{effectName}' not found!", this);
            return;
        }

        AudioClipData clipData = soundEffectDict[effectName];
        
        uiSource.PlayOneShot(clipData.clip, clipData.volume * uiVolume);
    }

    public void PlayUIButtonClick()
    {
        PlayUISound("UI_ButtonClick");
    }

    public void PlayUIHover()
    {
        PlayUISound("UI_Hover");
    }

    // Volume Control System
    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(volume) * 20f);
        }
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (musicSource != null && currentMusicTrack != null)
        {
            musicSource.volume = currentMusicTrack.volume * volume;
        }
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20f);
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (audioMixer != null)
        {
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20f);
        }
    }

    public void SetAmbientVolume(float volume)
    {
        ambientVolume = volume;
        foreach (var source in activeAmbientSources)
        {
            if (source != null)
            {
                source.volume = volume;
            }
        }
        if (audioMixer != null)
        {
            audioMixer.SetFloat("AmbientVolume", Mathf.Log10(volume) * 20f);
        }
    }

    public void SetUIVolume(float volume)
    {
        uiVolume = volume;
        if (audioMixer != null)
        {
            audioMixer.SetFloat("UIVolume", Mathf.Log10(volume) * 20f);
        }
    }

    // Dynamic Audio System
    public void UpdateDynamicAudio()
    {
        if (!enableDynamicAudio) return;

        // Update day/night blend
        if (audioMixer != null)
        {
            audioMixer.SetFloat("DayNightBlend", dayNightBlend);
        }

        // Update player health blend
        if (audioMixer != null)
        {
            audioMixer.SetFloat("PlayerHealthBlend", playerHealthBlend);
        }

        // Update stealth blend
        if (audioMixer != null)
        {
            audioMixer.SetFloat("StealthBlend", playerStealthBlend);
        }
    }

    public void SetDayNightBlend(float blend)
    {
        dayNightBlend = Mathf.Clamp01(blend);
        UpdateDynamicAudio();
    }

    public void SetPlayerHealthBlend(float blend)
    {
        playerHealthBlend = Mathf.Clamp01(blend);
        UpdateDynamicAudio();
    }

    public void SetPlayerStealthBlend(float blend)
    {
        playerStealthBlend = Mathf.Clamp01(blend);
        UpdateDynamicAudio();
    }

    // Utility Methods
    public void StopAllSounds()
    {
        if (musicSource != null) musicSource.Stop();
        if (ambientSource != null) ambientSource.Stop();
        if (uiSource != null) uiSource.Stop();
        if (playerSource != null) playerSource.Stop();

        foreach (var source in activeAmbientSources)
        {
            if (source != null)
            {
                source.Stop();
            }
        }
    }

    public void PauseAllSounds()
    {
        if (musicSource != null) musicSource.Pause();
        if (ambientSource != null) ambientSource.Pause();
        if (uiSource != null) uiSource.Pause();
        if (playerSource != null) playerSource.Pause();

        foreach (var source in activeAmbientSources)
        {
            if (source != null)
            {
                source.Pause();
            }
        }
    }

    public void ResumeAllSounds()
    {
        if (musicSource != null) musicSource.UnPause();
        if (ambientSource != null) ambientSource.UnPause();
        if (uiSource != null) uiSource.UnPause();
        if (playerSource != null) playerSource.UnPause();

        foreach (var source in activeAmbientSources)
        {
            if (source != null)
            {
                source.UnPause();
            }
        }
    }

    // Debug Methods
    [ContextMenu("Log Audio Statistics")]
    public void LogAudioStatistics()
    {
        GameLogger.Log(LogCategory.Audio, "=== Audio Manager Statistics ===", this);
        GameLogger.Log(LogCategory.Audio, $"Current Music: {(currentMusicTrack != null ? currentMusicTrack.trackName : "None")}", this);
        GameLogger.Log(LogCategory.Audio, $"Active Ambient Sources: {activeAmbientSources.Count}", this);
        GameLogger.Log(LogCategory.Audio, $"Audio Pools: {audioSourcePools.Count}", this);
        GameLogger.Log(LogCategory.Audio, $"Sound Effects: {soundEffectDict.Count}", this);
        GameLogger.Log(LogCategory.Audio, $"Music Tracks: {musicTrackDict.Count}", this);
        GameLogger.Log(LogCategory.Audio, $"Ambient Sounds: {ambientSoundDict.Count}", this);
        GameLogger.Log(LogCategory.Audio, "================================", this);
    }

    [ContextMenu("Stop All Sounds")]
    public void StopAllSoundsFromContext()
    {
        StopAllSounds();
    }

    void Update()
    {
        // Update ambient sounds that follow player
        if (playerTransform != null)
        {
            foreach (var ambient in ambientSounds)
            {
                if (ambient.followPlayer)
                {
                    // Find corresponding audio source and update position
                    foreach (var source in activeAmbientSources)
                    {
                        if (source != null && source.name.Contains(ambient.ambientName))
                        {
                            source.transform.position = playerTransform.position;
                            break;
                        }
                    }
                }
            }
        }
    }

    void OnDestroy()
    {
        // Clean up all audio sources
        StopAllSounds();
        foreach (var source in activeAmbientSources)
        {
            if (source != null)
            {
                Destroy(source.gameObject);
            }
        }
    }
} 