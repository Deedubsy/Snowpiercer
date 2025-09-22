using UnityEngine;
using UnityEngine.UI;  // Required if you're using a UI Slider

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float crouchSpeed = 3f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Noise Settings")]
    public float walkNoiseRadius = 5f;
    public float sprintNoiseRadius = 10f;
    public float jumpNoiseRadius = 15f;
    public float crouchNoiseRadius = 2f;
    public float landingNoiseMultiplier = 1.5f;
    public float noiseInterval = 0.5f;  // How often to make noise while moving
    private float lastNoiseTime;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f;     // Stamina per second
    public float sprintStaminaCost = 20f;      // Stamina cost per second while sprinting
    private float currentStamina;

    [Header("UI (Optional)")]
    // Drag your UI Slider here in the Inspector to visualize stamina.
    public Slider staminaSlider;

    [Header("Interaction Settings")]
    public float interactRange = 2.5f;
    public KeyCode interactKey = KeyCode.E;
    private InteractiveObject focusedObject;
    public GameObject interactPromptUI;
    public UnityEngine.UI.Text interactPromptText;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isCrouching;
    private bool isSprinting;
    private float standingHeight = 2f;
    private float crouchingHeight = 1f;
    private VampireStats stats;
    private PlayerHealth health;
    public PlayerHealth Health => health;
    private bool wasGrounded;
    private float lastJumpTime;

    [Header("Audio & Animation")]
    public AudioSource footstepSource;
    public Animator animator;

    // Expose crouch state for other scripts.
    public bool IsCrouched
    {
        get { return isCrouching; }
    }

    // Expose sprint state for other scripts.
    public bool IsSprinting()
    {
        return isSprinting;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.height = standingHeight;
        currentStamina = maxStamina;
        stats = GetComponent<VampireStats>();
        health = GetComponent<PlayerHealth>();

        // Setup the slider, if available.
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void Update()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Set base movement speed.
        float speed = walkSpeed;
        isSprinting = false;

        // Check for sprint input (Left Shift) and ensure the player isn't crouching and has stamina.
        if (Input.GetKey(KeyCode.LeftShift) && !isCrouching && currentStamina > 0f)
        {
            isSprinting = true;
            speed = sprintSpeed;
            // Deduct stamina over time.
            currentStamina -= sprintStaminaCost * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }
        else
        {
            // Regenerate stamina when not sprinting.
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }

        // Check for crouch input (crouching overrides sprinting).
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            controller.height = crouchingHeight;
        }
        if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            controller.height = standingHeight;
        }

        // If crouched, override speed.
        if (isCrouching)
            speed = crouchSpeed;

        // Get movement input.
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * speed * Time.deltaTime);

        // Generate noise based on movement
        if (move.magnitude > 0.1f && Time.time - lastNoiseTime > noiseInterval)
        {
            float noiseRadius = walkNoiseRadius;
            float noiseIntensity = 0.5f;

            if (isSprinting)
            {
                noiseRadius = sprintNoiseRadius;
                noiseIntensity = 1f;
            }
            else if (isCrouching)
            {
                noiseRadius = crouchNoiseRadius;
                noiseIntensity = 0.2f;
            }

            NoiseManager.MakeNoise(transform.position, noiseRadius, noiseIntensity);
            lastNoiseTime = Time.time;
        }

        // Jumping.
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
            // Make noise when jumping
            NoiseManager.MakeNoise(transform.position, jumpNoiseRadius, 1.2f);
        }

        // Check for landing
        if (!wasGrounded && isGrounded && Time.time - lastJumpTime > 0.1f)
        {
            // Make noise when landing
            float landingIntensity = Mathf.Clamp01(Mathf.Abs(velocity.y) / 10f);
            NoiseManager.MakeNoise(transform.position, jumpNoiseRadius * landingNoiseMultiplier, landingIntensity);
        }
        wasGrounded = isGrounded;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Update the stamina UI, if available.
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }

        HandleInteraction();
    }

    void HandleInteraction()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;
        InteractiveObject found = null;
        if (Physics.Raycast(ray, out hit, interactRange))
        {
            found = hit.collider.GetComponent<InteractiveObject>();
        }
        if (found != null)
        {
            if (focusedObject != found)
            {
                if (focusedObject != null) focusedObject.OnUnfocus(this);
                focusedObject = found;
                focusedObject.OnFocus(this);
                if (interactPromptUI != null && interactPromptText != null)
                {
                    interactPromptUI.SetActive(true);
                    interactPromptText.text = found.promptText;
                }
            }
            if (Input.GetKeyDown(interactKey))
            {
                focusedObject.Interact(this);
            }
        }
        else
        {
            if (focusedObject != null)
            {
                focusedObject.OnUnfocus(this);
                focusedObject = null;
            }
            if (interactPromptUI != null)
                interactPromptUI.SetActive(false);
        }
    }

    public float GetKillDrainRange()
    {
        return stats != null ? stats.killDrainRange : 2f;
    }

    // New methods for integration with new systems
    public bool IsCrouching()
    {
        return isCrouching;
    }

    public void SetDisguisedAnimations(bool disguised)
    {
        // Update animator parameters for disguised movement
        if (animator != null)
        {
            animator.SetBool("IsDisguised", disguised);
        }

        // Modify movement sound/particles if disguised
        if (disguised)
        {
            // Quieter footsteps when disguised
            if (footstepSource != null)
            {
                footstepSource.volume *= 0.5f;
            }
        }
        else
        {
            // Restore normal footstep volume
            if (footstepSource != null)
            {
                footstepSource.volume *= 2f; // Assuming it was halved
            }
        }
    }
}
