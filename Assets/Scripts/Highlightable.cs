using UnityEngine;

public class Highlightable : MonoBehaviour
{
    public Renderer[] renderers;
    public Material highlightMaterial;
    private Material[] originalMaterials;
    private bool isHighlighted = false;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalMaterials[i] = renderers[i].material;
        }
    }

    public void SetHighlight(bool on)
    {
        if (isHighlighted == on) return;
        isHighlighted = on;
        if (renderers == null || renderers.Length == 0) return;
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material = on && highlightMaterial != null ? highlightMaterial : originalMaterials[i];
        }
    }
} 