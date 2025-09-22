using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseManager : MonoBehaviour
{
    private static NoiseManager instance;
    public static NoiseManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("NoiseManager");
                instance = go.AddComponent<NoiseManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("Debug Settings")]
    public bool showNoiseGizmos = true;
    public float gizmoDuration = 1f;

    private List<NoiseEvent> activeNoises = new List<NoiseEvent>();

    private class NoiseEvent
    {
        public Vector3 position;
        public float radius;
        public float intensity;
        public float createdTime;

        public NoiseEvent(Vector3 pos, float rad, float intens)
        {
            position = pos;
            radius = rad;
            intensity = intens;
            createdTime = Time.time;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void MakeNoise(Vector3 position, float radius, float intensity = 1f)
    {
        Instance.CreateNoise(position, radius, intensity);
    }

    private void CreateNoise(Vector3 position, float radius, float intensity)
    {
        // Create noise event for visualization
        NoiseEvent noiseEvent = new NoiseEvent(position, radius, intensity);
        activeNoises.Add(noiseEvent);

        // Alert all citizens within range
        Collider[] citizenColliders = Physics.OverlapSphere(position, radius);
        foreach (Collider col in citizenColliders)
        {
            Citizen citizen = col.GetComponent<Citizen>();
            if (citizen != null)
            {
                float distance = Vector3.Distance(position, citizen.transform.position);
                if (distance <= radius)
                {
                    // Intensity decreases with distance
                    float adjustedIntensity = intensity * (1f - distance / radius);
                    citizen.ReactToNoise(position, adjustedIntensity);
                }
            }

            // Also alert guards
            GuardAI guard = col.GetComponent<GuardAI>();
            if (guard != null)
            {
                float distance = Vector3.Distance(position, guard.transform.position);
                if (distance <= radius)
                {
                    // Guards are more alert to noise
                    float adjustedIntensity = intensity * (1f - distance / radius) * 1.5f;
                    guard.InvestigateNoise(position, adjustedIntensity);
                }
            }
        }

        // Clean up old noise events for visualization
        activeNoises.RemoveAll(n => Time.time - n.createdTime > gizmoDuration);
    }

    void OnDrawGizmos()
    {
        if (!showNoiseGizmos) return;

        foreach (NoiseEvent noise in activeNoises)
        {
            float age = Time.time - noise.createdTime;
            float alpha = 1f - (age / gizmoDuration);
            
            // Draw expanding circle to show noise
            Color noiseColor = new Color(1f, 0.5f, 0f, alpha * 0.3f);
            Gizmos.color = noiseColor;
            
            // Draw multiple circles for better visualization
            for (int i = 0; i < 3; i++)
            {
                float radiusMultiplier = 1f - (i * 0.3f);
                Gizmos.DrawWireSphere(noise.position, noise.radius * radiusMultiplier * (1f + age * 0.5f));
            }

            // Draw intensity indicator
            Color intensityColor = Color.Lerp(Color.yellow, Color.red, noise.intensity);
            intensityColor.a = alpha;
            Gizmos.color = intensityColor;
            Gizmos.DrawWireSphere(noise.position, 0.5f);
        }
    }
}