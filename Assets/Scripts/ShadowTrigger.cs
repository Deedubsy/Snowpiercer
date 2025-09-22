using UnityEngine;
using UnityEngine.Events;

public class ShadowTrigger : MonoBehaviour
{
    [Header("Shadow Trigger Settings")]
    public bool isActive = true;
    public bool requiresCrouching = true;
    public bool isPermanentShadow = false;
    
    [Header("Shadow Properties")]
    public float shadowIntensity = 1f;
    public Color shadowColor = Color.black;
    public bool affectsPlayerVisibility = true;
    public bool affectsEnemyDetection = true;
    
    [Header("Trigger Events")]
    public UnityEvent onPlayerEnterShadow;
    public UnityEvent onPlayerExitShadow;
    public UnityEvent onPlayerStayInShadow;
    
    [Header("Visual Feedback")]
    public bool showDebugInfo = false;
    public GameObject visualIndicator;
    public Material shadowMaterial;
    
    [Header("Dynamic Shadow Settings")]
    public bool isDynamicShadow = false;
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 1f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // Private variables
    private bool playerInShadow = false;
    private float currentShadowIntensity = 0f;
    private Collider triggerCollider;
    private Renderer shadowRenderer;
    private Material originalMaterial;
    
    // Events
    public delegate void ShadowEvent(bool inShadow);
    public static event ShadowEvent OnShadowStateChanged;
    
    void Start()
    {
        // Get or add trigger collider
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
            triggerCollider.isTrigger = true;
        }
        
        // Setup visual indicator
        if (visualIndicator != null)
        {
            shadowRenderer = visualIndicator.GetComponent<Renderer>();
            if (shadowRenderer != null && shadowMaterial != null)
            {
                originalMaterial = shadowRenderer.material;
                shadowRenderer.material = shadowMaterial;
            }
        }
        
        // Set initial shadow intensity
        if (isPermanentShadow)
        {
            currentShadowIntensity = shadowIntensity;
        }
    }
    
    void Update()
    {
        if (!isActive) return;
        
        // Handle dynamic shadow fading
        if (isDynamicShadow && playerInShadow)
        {
            if (currentShadowIntensity < shadowIntensity)
            {
                currentShadowIntensity += Time.deltaTime / fadeInDuration;
                currentShadowIntensity = Mathf.Clamp01(currentShadowIntensity);
            }
        }
        else if (isDynamicShadow && !playerInShadow && !isPermanentShadow)
        {
            if (currentShadowIntensity > 0)
            {
                currentShadowIntensity -= Time.deltaTime / fadeOutDuration;
                currentShadowIntensity = Mathf.Clamp01(currentShadowIntensity);
            }
        }
        
        // Update visual feedback
        UpdateVisualFeedback();
        
        // Trigger stay event
        if (playerInShadow)
        {
            onPlayerStayInShadow?.Invoke();
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Player"))
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            
            // Check if crouching is required
            if (requiresCrouching && playerController != null && !playerController.IsCrouched)
            {
                return;
            }
            
            playerInShadow = true;
            currentShadowIntensity = isDynamicShadow ? 0f : shadowIntensity;
            
            // Trigger events
            onPlayerEnterShadow?.Invoke();
            OnShadowStateChanged?.Invoke(true);
            
            if (showDebugInfo)
            {
                Debug.Log($"Player entered shadow: {gameObject.name}");
            }
        }
    }
    
    void OnTriggerStay(Collider other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Player"))
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            
            // Check if player is still crouched (if required)
            if (requiresCrouching && playerController != null && !playerController.IsCrouched)
            {
                if (playerInShadow)
                {
                    playerInShadow = false;
                    onPlayerExitShadow?.Invoke();
                    OnShadowStateChanged?.Invoke(false);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"Player left shadow (no longer crouched): {gameObject.name}");
                    }
                }
                return;
            }
            
            // Ensure player is marked as in shadow
            if (!playerInShadow)
            {
                playerInShadow = true;
                onPlayerEnterShadow?.Invoke();
                OnShadowStateChanged?.Invoke(true);
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Player"))
        {
            playerInShadow = false;
            
            // Trigger events
            onPlayerExitShadow?.Invoke();
            OnShadowStateChanged?.Invoke(false);
            
            if (showDebugInfo)
            {
                Debug.Log($"Player exited shadow: {gameObject.name}");
            }
        }
    }
    
    void UpdateVisualFeedback()
    {
        if (shadowRenderer != null && shadowMaterial != null)
        {
            // Update material properties based on shadow intensity
            Color currentColor = Color.Lerp(Color.clear, shadowColor, currentShadowIntensity);
            shadowMaterial.SetColor("_Color", currentColor);
            shadowMaterial.SetFloat("_Alpha", currentShadowIntensity);
        }
    }
    
    // Public methods for external control
    public void ActivateShadow()
    {
        isActive = true;
    }
    
    public void DeactivateShadow()
    {
        isActive = false;
        if (playerInShadow)
        {
            playerInShadow = false;
            onPlayerExitShadow?.Invoke();
            OnShadowStateChanged?.Invoke(false);
        }
    }
    
    public void SetShadowIntensity(float intensity)
    {
        shadowIntensity = Mathf.Clamp01(intensity);
        if (!isDynamicShadow)
        {
            currentShadowIntensity = shadowIntensity;
        }
    }
    
    public bool IsPlayerInShadow()
    {
        return playerInShadow;
    }
    
    public float GetCurrentShadowIntensity()
    {
        return currentShadowIntensity;
    }
    
    // Editor visualization
    void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        Gizmos.color = playerInShadow ? Color.red : Color.gray;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        if (triggerCollider is BoxCollider boxCollider)
        {
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else if (triggerCollider is SphereCollider sphereCollider)
        {
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
        }
        else if (triggerCollider is CapsuleCollider capsuleCollider)
        {
            Gizmos.DrawWireCube(capsuleCollider.center, new Vector3(capsuleCollider.radius * 2, capsuleCollider.height, capsuleCollider.radius * 2));
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        
        if (triggerCollider is BoxCollider boxCollider)
        {
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else if (triggerCollider is SphereCollider sphereCollider)
        {
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
        }
        else if (triggerCollider is CapsuleCollider capsuleCollider)
        {
            Gizmos.DrawWireCube(capsuleCollider.center, new Vector3(capsuleCollider.radius * 2, capsuleCollider.height, capsuleCollider.radius * 2));
        }
    }
} 