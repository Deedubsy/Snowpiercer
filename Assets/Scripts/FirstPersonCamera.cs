using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public float mouseSensitivity = 100f;  // This value can later be loaded from your game settings.
    public Transform playerBody;           // Reference to the player's body for horizontal rotation.
    public float rotationSmoothing = 10f;    // Lower values produce smoother, slower transitions.

    private float xRotation = 0f;          // Vertical rotation (pitch)
    private Quaternion targetRotation;     // For smoothing the vertical rotation

    void Start()
    {
        // Lock the cursor for a true first-person experience.
        Cursor.lockState = CursorLockMode.Locked;

        // Reset the camera's local rotation so it doesn't start pointing at the ground.
        transform.localRotation = Quaternion.identity;
        xRotation = 0f;
        targetRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        // Get mouse movement inputs.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Adjust vertical rotation and clamp to prevent flipping.
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Set the target rotation for the camera (vertical only).
        targetRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Smoothly interpolate the camera's rotation towards the target.
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, rotationSmoothing * Time.deltaTime);

        // Rotate the player horizontally immediately.
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
