using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DayNightLightingController : MonoBehaviour
{
    [Header("References")]
    public Light mainLight;
    public Material daySkybox;
    public Material nightSkybox;

    [Header("Day Settings")]
    public Color dayColor = new Color(1f, 0.95f, 0.8f);
    public float dayIntensity = 1.2f;
    public Color dayAmbient = new Color(0.7f, 0.7f, 0.7f);

    [Header("Night Settings")]
    public Color nightColor = new Color(0.2f, 0.3f, 0.6f);
    public float nightIntensity = 0.3f;
    public Color nightAmbient = new Color(0.05f, 0.08f, 0.15f);

    [Header("Transition")]
    public float transitionDuration = 3f;

    [Header("Gameplay Integration")]
    public Light[] streetLights;
    public Light[] buildingLights;
    public float streetLightIntensity = 0.8f;
    public float buildingLightIntensity = 0.5f;
    public bool useRandomFlickering = true;
    public float flickerChance = 0.02f;

    // Private variables
    private float baseDayIntensity;
    private float baseNightIntensity;
    private Color baseNightAmbient;
    private bool isNight = true;
    private Dictionary<Light, float> originalLightIntensities = new Dictionary<Light, float>();

    void Start()
    {
        // Store base values for difficulty scaling
        baseDayIntensity = dayIntensity;
        baseNightIntensity = nightIntensity;
        baseNightAmbient = nightAmbient;

        // Initialize street and building lights
        InitializeLights();

        GameManager gm = GameManager.instance;
        if (gm != null)
        {
            gm.OnSundown += TransitionToNight;
            gm.OnSunrise += TransitionToDay;
        }

        // Set initial state to night
        SetNightImmediate();

        Debug.Log("[DayNightLightingController] Initialized with " + (streetLights?.Length ?? 0) + " street lights and " + (buildingLights?.Length ?? 0) + " building lights");
    }

    void Update()
    {
        // Handle light flickering during night time
        if (isNight && useRandomFlickering)
        {
            HandleLightFlickering();
        }
    }

    void OnDestroy()
    {
        GameManager gm = GameManager.instance;
        if (gm != null)
        {
            gm.OnSundown -= TransitionToNight;
            gm.OnSunrise -= TransitionToDay;
        }
    }

    public void TransitionToNight()
    {
        isNight = true;
        StopAllCoroutines();
        StartCoroutine(LightingTransition(dayColor, nightColor, dayIntensity, nightIntensity, dayAmbient, nightAmbient, daySkybox, nightSkybox));
        StartCoroutine(TransitionStreetLights(true));
    }

    public void TransitionToDay()
    {
        isNight = false;
        StopAllCoroutines();
        StartCoroutine(LightingTransition(nightColor, dayColor, nightIntensity, dayIntensity, nightAmbient, dayAmbient, nightSkybox, daySkybox));
        StartCoroutine(TransitionStreetLights(false));
    }

    IEnumerator LightingTransition(Color fromColor, Color toColor, float fromIntensity, float toIntensity, Color fromAmbient, Color toAmbient, Material fromSkybox, Material toSkybox)
    {
        float t = 0f;
        // Set initial skybox
        if (fromSkybox != null)
            RenderSettings.skybox = fromSkybox;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            if (mainLight != null)
            {
                mainLight.color = Color.Lerp(fromColor, toColor, t);
                mainLight.intensity = Mathf.Lerp(fromIntensity, toIntensity, t);
            }
            RenderSettings.ambientLight = Color.Lerp(fromAmbient, toAmbient, t);
            yield return null;
        }
        if (mainLight != null)
        {
            mainLight.color = toColor;
            mainLight.intensity = toIntensity;
        }
        RenderSettings.ambientLight = toAmbient;
        // Set final skybox
        if (toSkybox != null)
            RenderSettings.skybox = toSkybox;
    }

    public void SetNightImmediate()
    {
        isNight = true;
        if (mainLight != null)
        {
            mainLight.color = nightColor;
            mainLight.intensity = nightIntensity;
        }
        RenderSettings.ambientLight = nightAmbient;
        if (nightSkybox != null)
            RenderSettings.skybox = nightSkybox;
        SetStreetLightsImmediate(true);
    }

    public void SetDayImmediate()
    {
        isNight = false;
        if (mainLight != null)
        {
            mainLight.color = dayColor;
            mainLight.intensity = dayIntensity;
        }
        RenderSettings.ambientLight = dayAmbient;
        if (daySkybox != null)
            RenderSettings.skybox = daySkybox;
        SetStreetLightsImmediate(false);
    }
    
    // Method for DifficultyProgression integration
    public void SetDifficultyLightingMultiplier(float multiplier)
    {
        // Apply multiplier to night lighting intensity
        // Higher difficulty = brighter nights (harder for vampire to hide)
        float adjustedNightIntensity = nightIntensity * multiplier;
        
        // Clamp to reasonable values
        adjustedNightIntensity = Mathf.Clamp(adjustedNightIntensity, 0.1f, 1.0f);
        
        // Apply immediately if we're currently in night mode
        if (mainLight != null && mainLight.intensity <= nightIntensity + 0.1f)
        {
            mainLight.intensity = adjustedNightIntensity;
        }
        
        // Update the night intensity for future transitions
        nightIntensity = adjustedNightIntensity;
        
        // Also adjust ambient lighting
        Color adjustedNightAmbient = nightAmbient * multiplier;
        adjustedNightAmbient.a = 1f; // Preserve alpha
        nightAmbient = adjustedNightAmbient;
        
        // Apply ambient lighting immediately if in night mode
        if (RenderSettings.ambientLight.r <= nightAmbient.r + 0.1f)
        {
            RenderSettings.ambientLight = adjustedNightAmbient;
        }
        
        Debug.Log($"[DayNightLightingController] Updated difficulty lighting multiplier to {multiplier:F2}. Night intensity: {adjustedNightIntensity:F2}");
    }

    void InitializeLights()
    {
        // Store original intensities for all lights
        if (streetLights != null)
        {
            foreach (Light light in streetLights)
            {
                if (light != null && !originalLightIntensities.ContainsKey(light))
                {
                    originalLightIntensities[light] = light.intensity;
                }
            }
        }

        if (buildingLights != null)
        {
            foreach (Light light in buildingLights)
            {
                if (light != null && !originalLightIntensities.ContainsKey(light))
                {
                    originalLightIntensities[light] = light.intensity;
                }
            }
        }
    }

    IEnumerator TransitionStreetLights(bool turnOn)
    {
        List<Light> allLights = new List<Light>();
        if (streetLights != null) allLights.AddRange(streetLights);
        if (buildingLights != null) allLights.AddRange(buildingLights);

        float transitionTime = transitionDuration * 0.8f; // Lights transition slightly faster than main lighting
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / transitionTime;

            foreach (Light light in allLights)
            {
                if (light == null) continue;

                float targetIntensity = turnOn ? GetTargetLightIntensity(light) : 0f;
                float startIntensity = turnOn ? 0f : GetOriginalIntensity(light);

                light.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                light.enabled = light.intensity > 0.01f;
            }

            yield return null;
        }

        // Ensure final state
        foreach (Light light in allLights)
        {
            if (light == null) continue;

            if (turnOn)
            {
                light.intensity = GetTargetLightIntensity(light);
                light.enabled = true;
            }
            else
            {
                light.intensity = 0f;
                light.enabled = false;
            }
        }
    }

    void SetStreetLightsImmediate(bool turnOn)
    {
        List<Light> allLights = new List<Light>();
        if (streetLights != null) allLights.AddRange(streetLights);
        if (buildingLights != null) allLights.AddRange(buildingLights);

        foreach (Light light in allLights)
        {
            if (light == null) continue;

            if (turnOn)
            {
                light.intensity = GetTargetLightIntensity(light);
                light.enabled = true;
            }
            else
            {
                light.intensity = 0f;
                light.enabled = false;
            }
        }
    }

    float GetTargetLightIntensity(Light light)
    {
        if (streetLights != null && System.Array.IndexOf(streetLights, light) >= 0)
        {
            return streetLightIntensity;
        }
        else if (buildingLights != null && System.Array.IndexOf(buildingLights, light) >= 0)
        {
            return buildingLightIntensity;
        }

        return GetOriginalIntensity(light);
    }

    float GetOriginalIntensity(Light light)
    {
        if (light == null) return 1f;
        return originalLightIntensities.ContainsKey(light) ? originalLightIntensities[light] : 1f;
    }

    void HandleLightFlickering()
    {
        if (streetLights == null) return;

        foreach (Light light in streetLights)
        {
            if (light == null || !light.enabled) continue;

            if (Random.value < flickerChance)
            {
                StartCoroutine(FlickerLight(light));
            }
        }
    }

    IEnumerator FlickerLight(Light light)
    {
        if (light == null) yield break;

        float originalIntensity = light.intensity;

        // Quick flicker
        light.intensity = 0f;
        yield return new WaitForSeconds(0.05f);

        light.intensity = originalIntensity * 0.3f;
        yield return new WaitForSeconds(0.02f);

        light.intensity = 0f;
        yield return new WaitForSeconds(0.03f);

        light.intensity = originalIntensity;
    }

    // Public methods for gameplay integration
    public float GetCurrentLightLevel()
    {
        if (mainLight == null) return 0f;
        return isNight ? (nightIntensity / baseNightIntensity) : (dayIntensity / baseDayIntensity);
    }

    public bool IsNight()
    {
        return isNight;
    }

    public void SetLightFlicker(bool enabled)
    {
        useRandomFlickering = enabled;
    }

    // Method for random events or player abilities to temporarily affect lighting
    public void TemporaryLightingEffect(float multiplier, float duration)
    {
        StartCoroutine(ApplyTemporaryEffect(multiplier, duration));
    }

    IEnumerator ApplyTemporaryEffect(float multiplier, float duration)
    {
        // Store current values
        float originalMainIntensity = mainLight != null ? mainLight.intensity : 0f;
        Color originalAmbient = RenderSettings.ambientLight;

        // Apply effect
        if (mainLight != null)
        {
            mainLight.intensity *= multiplier;
        }
        RenderSettings.ambientLight = originalAmbient * multiplier;

        yield return new WaitForSeconds(duration);

        // Restore original values
        if (mainLight != null)
        {
            mainLight.intensity = originalMainIntensity;
        }
        RenderSettings.ambientLight = originalAmbient;
    }
} 