using UnityEngine;

[System.Serializable]
public class DetectionSettings
{
    [Header("Vision Settings")]
    public float fieldOfView = 90f;
    public float viewDistance = 35f;
    public float detectionTime = 0.5f;
    public float closeRangeDetectionTime = 0.1f;
    public float closeRangeDistance = 6f;
    
    [Header("Peripheral Vision")]
    public bool enablePeripheralVision = true;
    public float peripheralVisionAngle = 120f;
    public float peripheralDetectionTime = 1.0f;
    
    [Header("Layers")]
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;
}

public class DetectionSystem : MonoBehaviour, ISpatialEntity
{
    public DetectionSettings settings = new DetectionSettings();
    
    [Header("Detection State")]
    public float detectionProgress = 0f;
    public bool isDetecting = false;
    
    private Transform player;
    private VampireStats playerStats;
    private float detectionTimer = 0f;
    private Vector3 lastPlayerPosition;
    private float playerMovementSpeed;
    
    public Vector3 Position => transform.position;
    public Transform Transform => transform;
    
    // Events
    public System.Action<Transform> OnPlayerDetected;
    public System.Action OnPlayerLost;
    public System.Action<float> OnDetectionProgressChanged;
    
    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<VampireStats>();
        }
        
        // Register with spatial grid
        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.RegisterEntity(this);
        }
    }
    
    void OnDestroy()
    {
        // Unregister from spatial grid
        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.UnregisterEntity(this);
        }
    }
    
    void Update()
    {
        // Update spatial grid position
        if (SpatialGrid.Instance != null)
        {
            SpatialGrid.Instance.UpdateEntity(this);
        }
    }
    
    public bool PerformDetection()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float effectiveSpotDistance = playerStats != null ? playerStats.spotDistance : settings.viewDistance;
        
        // Calculate player movement speed
        if (lastPlayerPosition != Vector3.zero)
        {
            playerMovementSpeed = (player.position - lastPlayerPosition).magnitude / Time.deltaTime;
        }
        lastPlayerPosition = player.position;
        
        // Check if player is within view distance and within the cone
        bool inDirectView = distanceToPlayer < effectiveSpotDistance && angle < settings.fieldOfView * 0.5f;
        bool inPeripheralView = false;
        
        // Check peripheral vision if enabled
        if (settings.enablePeripheralVision && distanceToPlayer < effectiveSpotDistance && 
            angle < settings.peripheralVisionAngle * 0.5f)
        {
            inPeripheralView = true;
        }
        
        // Check for line of sight
        bool hasLineOfSight = !Physics.Raycast(transform.position + Vector3.up * 1.5f, directionToPlayer, distanceToPlayer, settings.obstacleLayer);
        
        if ((inDirectView || inPeripheralView) && hasLineOfSight)
        {
            // Determine detection time based on conditions
            float effectiveDetectionTime = CalculateDetectionTime(distanceToPlayer, inDirectView, inPeripheralView, effectiveSpotDistance);
            
            detectionTimer += Time.deltaTime;
            detectionProgress = Mathf.Clamp01(detectionTimer / effectiveDetectionTime);
            isDetecting = true;
            
            OnDetectionProgressChanged?.Invoke(detectionProgress);
            
            if (detectionTimer >= effectiveDetectionTime)
            {
                detectionTimer = 0f;
                OnPlayerDetected?.Invoke(player);
                return true;
            }
        }
        else
        {
            // Reset detection when player is not visible
            if (isDetecting)
            {
                detectionTimer = 0f;
                detectionProgress = 0f;
                isDetecting = false;
                OnDetectionProgressChanged?.Invoke(0f);
                OnPlayerLost?.Invoke();
            }
        }
        
        return false;
    }
    
    private float CalculateDetectionTime(float distanceToPlayer, bool inDirectView, bool inPeripheralView, float effectiveSpotDistance)
    {
        float effectiveDetectionTime = settings.detectionTime;
        
        // Close range detection is much faster
        if (distanceToPlayer <= settings.closeRangeDistance)
        {
            effectiveDetectionTime = settings.closeRangeDetectionTime;
        }
        // Peripheral vision is slower
        else if (inPeripheralView && !inDirectView)
        {
            effectiveDetectionTime = settings.peripheralDetectionTime;
        }
        else
        {
            // Distance-based scaling
            float distanceRatio = distanceToPlayer / effectiveSpotDistance;
            effectiveDetectionTime = settings.detectionTime * (0.5f + distanceRatio * 0.5f);
        }
        
        // Movement affects detection (moving targets are easier to spot)
        if (playerMovementSpeed > 5f) // Running
        {
            effectiveDetectionTime *= 0.4f;
        }
        else if (playerMovementSpeed > 2f) // Walking
        {
            effectiveDetectionTime *= 0.7f;
        }
        
        // Lighting affects detection
        float lightingModifier = GetLightingModifier();
        effectiveDetectionTime /= lightingModifier;
        
        return effectiveDetectionTime;
    }
    
    private float GetLightingModifier()
    {
        // Check for nearby lights
        Collider[] colliders = Physics.OverlapSphere(transform.position, 10f, -1);
        float totalIntensity = 0f;
        int lightCount = 0;
        
        foreach (var collider in colliders)
        {
            Light light = collider.GetComponent<Light>();
            if (light != null && light.enabled)
            {
                float distance = Vector3.Distance(transform.position, light.transform.position);
                if (distance < light.range)
                {
                    totalIntensity += light.intensity * (1f - distance / light.range);
                    lightCount++;
                }
            }
        }
        
        if (lightCount == 0)
        {
            return 0.3f; // Dark areas make detection harder
        }
        
        return Mathf.Clamp(0.3f + totalIntensity * 0.7f, 0.3f, 2f);
    }
    
    public bool CanSeePlayer()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float angle = Vector3.Angle(transform.forward, directionToPlayer);
        float effectiveSpotDistance = playerStats != null ? playerStats.spotDistance : settings.viewDistance;
        
        // Use half of the full FOV for the cone check
        if (distanceToPlayer <= effectiveSpotDistance && angle <= settings.fieldOfView * 0.5f)
        {
            // Check for obstacles blocking view
            if (!Physics.Raycast(transform.position, directionToPlayer, distanceToPlayer, settings.obstacleLayer))
            {
                return true;
            }
        }
        
        return false;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection cone
        Gizmos.color = isDetecting ? Color.red : Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        
        // Draw main view cone
        Gizmos.DrawWireCube(Vector3.forward * settings.viewDistance * 0.5f, 
            new Vector3(settings.viewDistance * Mathf.Tan(settings.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2f, 
                       2f, 
                       settings.viewDistance));
        
        // Draw peripheral vision if enabled
        if (settings.enablePeripheralVision)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(Vector3.forward * settings.viewDistance * 0.5f, 
                new Vector3(settings.viewDistance * Mathf.Tan(settings.peripheralVisionAngle * 0.5f * Mathf.Deg2Rad) * 2f, 
                           1f, 
                           settings.viewDistance));
        }
        
        Gizmos.matrix = Matrix4x4.identity;
        
        // Draw detection progress
        if (isDetecting)
        {
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, detectionProgress);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, detectionProgress * 2f);
        }
    }
}