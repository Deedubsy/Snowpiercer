using UnityEngine;

public class CityGateTrigger : MonoBehaviour
{
    public KeyCode returnKey = KeyCode.F; // Key to return to the castle.
    public GameObject returnPromptUI;       // UI element to prompt the player.

    private bool playerInRange = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (returnPromptUI != null)
                returnPromptUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (returnPromptUI != null)
                returnPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(returnKey))
        {
            // Notify GameManager that the player has returned to the castle.
            if (GameManager.instance != null)
                GameManager.instance.ReturnToCastle();
        }
    }
}
