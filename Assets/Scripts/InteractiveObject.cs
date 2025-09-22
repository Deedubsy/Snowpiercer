using UnityEngine;

public class InteractiveObject : MonoBehaviour
{
    [Header("Interaction")]
    public string displayName = "Interact";
    public string promptText = "Press E to interact";
    public float interactionRange = 2f;
    public bool requiresCrouch = false;
    
    protected string interactionPrompt;
    
    public virtual void Start()
    {
        // Base implementation - can be overridden
    }

    public virtual void Interact(PlayerController player)
    {
        Debug.Log($"{player.name} interacted with {displayName}");
    }

    // Optionally, you can add OnFocus/OnUnfocus for UI prompts
    public virtual void OnFocus(PlayerController player) { }
    public virtual void OnUnfocus(PlayerController player) { }
} 