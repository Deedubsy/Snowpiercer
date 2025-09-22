using UnityEngine;
using System.Collections.Generic;

public class GarlicTrap : InteractiveObject
{
    public float damagePerSecond = 10f;
    private HashSet<PlayerController> playersInZone = new HashSet<PlayerController>();
    public Renderer glowRenderer;
    public string emissionColorProperty = "_EmissionColor";
    public Color glowColor = Color.green;
    private Color originalEmission;

    void Start()
    {
        if (glowRenderer != null && glowRenderer.material.HasProperty(emissionColorProperty))
        {
            originalEmission = glowRenderer.material.GetColor(emissionColorProperty);
            SetGlow(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            playersInZone.Add(player);
            SetGlow(true);
            // Optionally, show a warning prompt
            promptText = "You feel weak...";
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            playersInZone.Remove(player);
            if (playersInZone.Count == 0)
                SetGlow(false);
            // Optionally, clear warning prompt
            promptText = "";
        }
    }

    void Update()
    {
        foreach (var player in playersInZone)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }

    void SetGlow(bool on)
    {
        if (glowRenderer != null && glowRenderer.material.HasProperty(emissionColorProperty))
        {
            if (on)
            {
                glowRenderer.material.EnableKeyword("_EMISSION");
                glowRenderer.material.SetColor(emissionColorProperty, glowColor);
            }
            else
            {
                glowRenderer.material.SetColor(emissionColorProperty, originalEmission);
            }
        }
    }
} 