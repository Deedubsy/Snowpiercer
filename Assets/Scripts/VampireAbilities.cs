using UnityEngine;

public class VampireAbilities : MonoBehaviour
{
    [Header("Blood Drinking Settings")]
    public float drinkRange = 2f;             // Maximum distance to drink blood from a citizen.
    public KeyCode drinkKey = KeyCode.E;      // Key to activate blood drinking.
    public float bloodAmountPerCitizen = 25f;   // Amount of blood gained per citizen.
    public float drinkCooldown = 1.0f;          // Cooldown time between drinks.

    private float drinkTimer = 0f;            // Timer to enforce cooldown.

    void Update()
    {
        drinkTimer += Time.deltaTime;

        // Check if the drink key is pressed and the cooldown has elapsed.
        if (Input.GetKeyDown(drinkKey) && drinkTimer >= drinkCooldown)
        {
            DrinkBlood();
            drinkTimer = 0f;
        }
    }

    // Casts a ray to detect a citizen in front of the player to drink blood from.
    void DrinkBlood()
    {
        RaycastHit hit;
        // Optionally, you might want to use the camera's position and forward vector if it's separate.
        if (Physics.Raycast(transform.position, transform.forward, out hit, drinkRange))
        {
            Citizen citizen = hit.collider.GetComponent<Citizen>();
            if (citizen != null && !citizen.isDrained)
            {
                citizen.Drain();
                VampireStats stats = GetComponent<VampireStats>();
                if (stats != null)
                {
                    stats.AddBlood(bloodAmountPerCitizen);
                }
                Debug.Log("Drank blood from citizen!");
            }
        }
    }

    // Placeholder for additional vampire abilities.
    public void UpgradeStealth()
    {
        // Implement stealth upgrade logic here.
        Debug.Log("Stealth upgraded!");
    }

    public void UpgradeSpeed()
    {
        // Implement speed upgrade logic here.
        Debug.Log("Speed upgraded!");
    }
}
