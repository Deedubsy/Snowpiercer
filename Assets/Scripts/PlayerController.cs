using UnityEngine;
using UnityEngine.UI;  // Required if you're using a UI Slider

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float crouchSpeed = 3f;
    public float sprintSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 10f;     // Stamina per second
    public float sprintStaminaCost = 20f;      // Stamina cost per second while sprinting
    private float currentStamina;

    [Header("UI (Optional)")]
    // Drag your UI Slider here in the Inspector to visualize stamina.
    public Slider staminaSlider;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isCrouching;
    private float standingHeight = 2f;
    private float crouchingHeight = 1f;

    // Expose crouch state for other scripts.
    public bool IsCrouched
    {
        get { return isCrouching; }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.height = standingHeight;
        currentStamina = maxStamina;

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
        bool isSprinting = false;

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

        // Jumping.
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Update the stamina UI, if available.
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }
}
