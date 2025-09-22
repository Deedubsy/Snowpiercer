using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The transform of the player body, used for horizontal (yaw) rotation.")]
    public Transform playerBody;

    [Header("Camera Settings")]
    [Tooltip("The sensitivity of the mouse look.")]
    public float mouseSensitivity = 2f;
    [Tooltip("How quickly the camera follows the mouse. Lower values are slower and smoother.")]
    public float smoothing = 10f;

    // The target rotation for the camera and player body
    private Quaternion playerTargetRot;
    private Quaternion cameraTargetRot;

    void Start()
    {
        // Lock and hide the cursor for a seamless first-person experience
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize the target rotations to the starting rotations
        playerTargetRot = playerBody.localRotation;
        cameraTargetRot = transform.localRotation;
    }

    void LateUpdate()
    {
        // Get mouse input and scale it by sensitivity
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Update the target rotations based on mouse input
        playerTargetRot *= Quaternion.Euler(0f, mouseX, 0f);
        cameraTargetRot *= Quaternion.Euler(-mouseY, 0f, 0f);

        // Clamp the vertical (pitch) rotation to prevent camera flipping
        cameraTargetRot = ClampPitch(cameraTargetRot);

        // Smoothly interpolate the player body and camera to their target rotations
        playerBody.localRotation = Quaternion.Slerp(playerBody.localRotation, playerTargetRot, smoothing * Time.deltaTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, cameraTargetRot, smoothing * Time.deltaTime);
    }

    /// <summary>
    /// Clamps the vertical rotation of a quaternion to a range of -90 to +90 degrees.
    /// </summary>
    private Quaternion ClampPitch(Quaternion q)
    {
        q.x /= q.w;
        q.y /= q.w;
        q.z /= q.w;
        q.w = 1.0f;

        float pitch = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);
        pitch = Mathf.Clamp(pitch, -90f, 90f);
        q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * pitch);

        return q;
    }
}
