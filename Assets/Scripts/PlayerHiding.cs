using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    // Indicates whether the player is hidden.
    public bool isHidden { get; private set; } = false;

    private PlayerController playerController;

    void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if the trigger is a designated hiding spot AND the player is crouched.
        if ((other.CompareTag("Shadow") || other.CompareTag("HidingSpot")) && playerController != null && playerController.IsCrouched)
        {
            isHidden = true;
            Debug.Log("Player is hidden.");
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Continuously ensure that the player remains hidden only while crouched.
        if ((other.CompareTag("Shadow") || other.CompareTag("HidingSpot")) && playerController != null)
        {
            if (!playerController.IsCrouched && isHidden)
            {
                isHidden = false;
                Debug.Log("Player is no longer hidden (not crouched).");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Shadow") || other.CompareTag("HidingSpot"))
        {
            isHidden = false;
            Debug.Log("Player left the hiding area.");
        }
    }
}
