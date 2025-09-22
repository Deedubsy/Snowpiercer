using UnityEngine;
using System.Collections.Generic;

public class HolySymbolTrap : InteractiveObject
{
    public float damagePerSecond = 15f;
    public Renderer glowRenderer;
    public string emissionColorProperty = "_EmissionColor";
    public Color glowColor = Color.yellow;
    private Color originalEmission;
    private HashSet<PlayerController> playersInZone = new HashSet<PlayerController>();

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
            promptText = "You feel a holy force!";
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