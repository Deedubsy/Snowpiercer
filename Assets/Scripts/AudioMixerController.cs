using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class AudioEffect
{
    public string effectName;
    public AudioMixerGroup mixerGroup;
    public bool enabled = true;
    [Range(0f, 1f)]
    public float intensity = 0.5f;
    public float fadeInTime = 1f;
    public float fadeOutTime = 1f;
    public bool useCurve = false;
    public AnimationCurve intensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
}

[System.Serializable]
public class DynamicMix
{
    public string parameterName;
    public float currentValue = 0f;
    public float targetValue = 0f;
    public float transitionSpeed = 1f;
    public bool useSmoothing = true;
    public AnimationCurve transitionCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
}

public class AudioMixerController : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Audio Effects")]
    public List<AudioEffect> audioEffects = new List<AudioEffect>();

    [Header("Dynamic Mixing")]
    public List<DynamicMix> dynamicMixes = new List<DynamicMix>();

    [Header("Day/Night Audio")]
    public bool enableDayNightAudio = true;
    public float dayNightBlend = 0f; // 0 = day, 1 = night
    public float dayNightTransitionSpeed = 0.5f;
    public AudioEffect dayAudioEffect;
    public AudioEffect nightAudioEffect;

    [Header("Player State Audio")]
    public bool enablePlayerStateAudio = true;
    public float playerHealthBlend = 1f; // 1 = full health, 0 = low health
    public float playerStealthBlend = 0f; // 0 = visible, 1 = hidden
    public AudioEffect lowHealthEffect;
    public AudioEffect stealthEffect;

    [Header("Environmental Audio")]
    public bool enableEnvironmentalAudio = true;
    public float indoorBlend = 0f; // 0 = outdoor, 1 = indoor
    public float weatherBlend = 0f; // 0 = clear, 1 = storm
    public AudioEffect indoorEffect;
    public AudioEffect weatherEffect;

    [Header("Combat Audio")]
    public bool enableCombatAudio = false;
    public float combatIntensity = 0f; // 0 = peaceful, 1 = intense combat
    public AudioEffect combatEffect;

    [Header("Settings")]
    public bool autoUpdate = true;
    public float updateInterval = 0.1f;
    public bool useRealTimeProcessing = true;

    [Header("Debug")]
    public bool debugMode = false;
    public bool showAudioParameters = false;

    // Private variables
    private Dictionary<string, AudioEffect> effectDict = new Dictionary<string, AudioEffect>();
    private Dictionary<string, DynamicMix> mixDict = new Dictionary<string, DynamicMix>();
    private float lastUpdateTime = 0f;
    private Coroutine effectCoroutine;

    void Start()
    {
        InitializeMixerController();
    }

    void InitializeMixerController()
    {
        if (audioMixer == null)
        {
            Debug.LogError("AudioMixer not assigned to AudioMixerController!");
            return;
        }

        // Initialize effect dictionary
        foreach (var effect in audioEffects)
        {
            if (!string.IsNullOrEmpty(effect.effectName))
            {
                effectDict[effect.effectName] = effect;
            }
        }

        // Initialize mix dictionary
        foreach (var mix in dynamicMixes)
        {
            if (!string.IsNullOrEmpty(mix.parameterName))
            {
                mixDict[mix.parameterName] = mix;
            }
        }

        // Set initial values
        UpdateAllAudioEffects();
        UpdateAllDynamicMixes();

        if (debugMode)
        {
            Debug.Log("AudioMixerController initialized successfully");
        }
    }

    void Update()
    {
        if (autoUpdate && Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateAudioSystem();
            lastUpdateTime = Time.time;
        }
    }

    void UpdateAudioSystem()
    {
        if (enableDayNightAudio)
        {
            UpdateDayNightAudio();
        }

        if (enablePlayerStateAudio)
        {
            UpdatePlayerStateAudio();
        }

        if (enableEnvironmentalAudio)
        {
            UpdateEnvironmentalAudio();
        }

        if (enableCombatAudio)
        {
            UpdateCombatAudio();
        }

        UpdateAllDynamicMixes();
    }

    void UpdateDayNightAudio()
    {
        if (dayAudioEffect != null && nightAudioEffect != null)
        {
            // Blend between day and night effects
            float dayIntensity = 1f - dayNightBlend;
            float nightIntensity = dayNightBlend;

            SetEffectIntensity(dayAudioEffect.effectName, dayIntensity);
            SetEffectIntensity(nightAudioEffect.effectName, nightIntensity);

            // Update mixer parameters
            if (audioMixer != null)
            {
                audioMixer.SetFloat("DayNightBlend", dayNightBlend);
                audioMixer.SetFloat("DayIntensity", dayIntensity);
                audioMixer.SetFloat("NightIntensity", nightIntensity);
            }
        }
    }

    void UpdatePlayerStateAudio()
    {
        if (lowHealthEffect != null)
        {
            float lowHealthIntensity = 1f - playerHealthBlend;
            SetEffectIntensity(lowHealthEffect.effectName, lowHealthIntensity);
        }

        if (stealthEffect != null)
        {
            SetEffectIntensity(stealthEffect.effectName, playerStealthBlend);
        }

        // Update mixer parameters
        if (audioMixer != null)
        {
            audioMixer.SetFloat("PlayerHealthBlend", playerHealthBlend);
            audioMixer.SetFloat("PlayerStealthBlend", playerStealthBlend);
        }
    }

    void UpdateEnvironmentalAudio()
    {
        if (indoorEffect != null)
        {
            SetEffectIntensity(indoorEffect.effectName, indoorBlend);
        }

        if (weatherEffect != null)
        {
            SetEffectIntensity(weatherEffect.effectName, weatherBlend);
        }

        // Update mixer parameters
        if (audioMixer != null)
        {
            audioMixer.SetFloat("IndoorBlend", indoorBlend);
            audioMixer.SetFloat("WeatherBlend", weatherBlend);
        }
    }

    void UpdateCombatAudio()
    {
        if (combatEffect != null)
        {
            SetEffectIntensity(combatEffect.effectName, combatIntensity);
        }

        // Update mixer parameters
        if (audioMixer != null)
        {
            audioMixer.SetFloat("CombatIntensity", combatIntensity);
        }
    }

    void UpdateAllDynamicMixes()
    {
        foreach (var mix in dynamicMixes)
        {
            if (mix.useSmoothing)
            {
                // Smooth transition to target value
                float delta = mix.targetValue - mix.currentValue;
                mix.currentValue += delta * mix.transitionSpeed * Time.deltaTime;

                // Apply transition curve if specified
                if (mix.transitionCurve != null)
                {
                    float curveValue = mix.transitionCurve.Evaluate(mix.currentValue);
                    SetMixerParameter(mix.parameterName, curveValue);
                }
                else
                {
                    SetMixerParameter(mix.parameterName, mix.currentValue);
                }
            }
            else
            {
                // Direct value setting
                mix.currentValue = mix.targetValue;
                SetMixerParameter(mix.parameterName, mix.currentValue);
            }
        }
    }

    void UpdateAllAudioEffects()
    {
        foreach (var effect in audioEffects)
        {
            if (effect.enabled)
            {
                SetEffectIntensity(effect.effectName, effect.intensity);
            }
        }
    }

    // Public methods for controlling audio effects
    public void SetEffectIntensity(string effectName, float intensity)
    {
        if (!effectDict.ContainsKey(effectName))
        {
            Debug.LogWarning($"Audio effect '{effectName}' not found!");
            return;
        }

        AudioEffect effect = effectDict[effectName];
        effect.intensity = Mathf.Clamp01(intensity);

        // Apply to mixer
        if (audioMixer != null)
        {
            string parameterName = $"{effectName}Intensity";
            float finalIntensity = effect.useCurve ? effect.intensityCurve.Evaluate(effect.intensity) : effect.intensity;
            audioMixer.SetFloat(parameterName, finalIntensity);
        }

        if (debugMode)
        {
            Debug.Log($"Set effect '{effectName}' intensity to {intensity}");
        }
    }

    public void EnableEffect(string effectName, bool enable)
    {
        if (!effectDict.ContainsKey(effectName))
        {
            Debug.LogWarning($"Audio effect '{effectName}' not found!");
            return;
        }

        AudioEffect effect = effectDict[effectName];
        effect.enabled = enable;

        if (enable)
        {
            SetEffectIntensity(effectName, effect.intensity);
        }
        else
        {
            SetEffectIntensity(effectName, 0f);
        }
    }

    public void FadeEffect(string effectName, float targetIntensity, float duration)
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine = StartCoroutine(FadeEffectCoroutine(effectName, targetIntensity, duration));
    }

    System.Collections.IEnumerator FadeEffectCoroutine(string effectName, float targetIntensity, float duration)
    {
        if (!effectDict.ContainsKey(effectName)) yield break;

        AudioEffect effect = effectDict[effectName];
        float startIntensity = effect.intensity;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float currentIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
            SetEffectIntensity(effectName, currentIntensity);

            yield return null;
        }

        SetEffectIntensity(effectName, targetIntensity);
    }

    // Public methods for controlling dynamic mixes
    public void SetDynamicMixTarget(string parameterName, float targetValue)
    {
        if (!mixDict.ContainsKey(parameterName))
        {
            Debug.LogWarning($"Dynamic mix parameter '{parameterName}' not found!");
            return;
        }

        DynamicMix mix = mixDict[parameterName];
        mix.targetValue = Mathf.Clamp01(targetValue);

        if (debugMode)
        {
            Debug.Log($"Set dynamic mix '{parameterName}' target to {targetValue}");
        }
    }

    public void SetDynamicMixValue(string parameterName, float value)
    {
        if (!mixDict.ContainsKey(parameterName))
        {
            Debug.LogWarning($"Dynamic mix parameter '{parameterName}' not found!");
            return;
        }

        DynamicMix mix = mixDict[parameterName];
        mix.currentValue = Mathf.Clamp01(value);
        mix.targetValue = mix.currentValue;

        SetMixerParameter(parameterName, mix.currentValue);
    }

    // Day/Night audio control
    public void SetDayNightBlend(float blend)
    {
        dayNightBlend = Mathf.Clamp01(blend);
    }

    public void FadeDayNightBlend(float targetBlend, float duration)
    {
        StartCoroutine(FadeDayNightBlendCoroutine(targetBlend, duration));
    }

    System.Collections.IEnumerator FadeDayNightBlendCoroutine(float targetBlend, float duration)
    {
        float startBlend = dayNightBlend;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            dayNightBlend = Mathf.Lerp(startBlend, targetBlend, t);

            yield return null;
        }

        dayNightBlend = targetBlend;
    }

    // Player state audio control
    public void SetPlayerHealthBlend(float blend)
    {
        playerHealthBlend = Mathf.Clamp01(blend);
    }

    public void SetPlayerStealthBlend(float blend)
    {
        playerStealthBlend = Mathf.Clamp01(blend);
    }

    // Environmental audio control
    public void SetIndoorBlend(float blend)
    {
        indoorBlend = Mathf.Clamp01(blend);
    }

    public void SetWeatherBlend(float blend)
    {
        weatherBlend = Mathf.Clamp01(blend);
    }

    // Combat audio control
    public void SetCombatIntensity(float intensity)
    {
        combatIntensity = Mathf.Clamp01(intensity);
    }

    // Utility methods
    void SetMixerParameter(string parameterName, float value)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat(parameterName, value);
        }
    }

    public float GetMixerParameter(string parameterName)
    {
        if (audioMixer != null)
        {
            float value;
            if (audioMixer.GetFloat(parameterName, out value))
            {
                return value;
            }
        }
        return 0f;
    }

    public void ResetAllEffects()
    {
        foreach (var effect in audioEffects)
        {
            SetEffectIntensity(effect.effectName, 0f);
        }
    }

    public void ResetAllDynamicMixes()
    {
        foreach (var mix in dynamicMixes)
        {
            SetDynamicMixValue(mix.parameterName, 0f);
        }
    }

    // Debug methods
    [ContextMenu("Log Audio Parameters")]
    public void LogAudioParameters()
    {
        Debug.Log("=== Audio Mixer Parameters ===");
        Debug.Log($"Day/Night Blend: {dayNightBlend}");
        Debug.Log($"Player Health Blend: {playerHealthBlend}");
        Debug.Log($"Player Stealth Blend: {playerStealthBlend}");
        Debug.Log($"Indoor Blend: {indoorBlend}");
        Debug.Log($"Weather Blend: {weatherBlend}");
        Debug.Log($"Combat Intensity: {combatIntensity}");

        foreach (var effect in audioEffects)
        {
            Debug.Log($"Effect {effect.effectName}: {effect.intensity} (Enabled: {effect.enabled})");
        }

        foreach (var mix in dynamicMixes)
        {
            Debug.Log($"Dynamic Mix {mix.parameterName}: {mix.currentValue} -> {mix.targetValue}");
        }
        Debug.Log("==============================");
    }

    [ContextMenu("Reset All Audio")]
    public void ResetAllAudioFromContext()
    {
        ResetAllEffects();
        ResetAllDynamicMixes();
    }

    [ContextMenu("Test Day/Night Transition")]
    public void TestDayNightTransition()
    {
        FadeDayNightBlend(1f - dayNightBlend, 3f);
    }

    void OnDrawGizmos()
    {
        if (showAudioParameters)
        {
            // Visual representation of audio parameters
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);

            // Day/Night indicator
            Gizmos.color = Color.Lerp(Color.yellow, Color.black, dayNightBlend);
            Gizmos.DrawSphere(transform.position + Vector3.up * 3f, 0.3f);
        }
    }
}