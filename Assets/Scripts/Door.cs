using UnityEngine;

public class Door : InteractiveObject
{
    public bool isOpen = false;
    public float openAngle = 90f;
    public float openSpeed = 3f;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isMoving = false;
    [Header("House Settings")]
    public WaypointGroup houseGroup;
    private CitizenScheduleManager scheduleManager;

    void Start()
    {
        closedRotation = transform.rotation;
        openRotation = closedRotation * Quaternion.Euler(0, openAngle, 0);
        UpdatePrompt();
        scheduleManager = FindObjectOfType<CitizenScheduleManager>();
    }

    public override void Interact(PlayerController player)
    {
        if (!isMoving)
        {
            ToggleDoor();
            
            // If this is a house door, notify the schedule manager
            if (houseGroup != null && houseGroup.groupType == WaypointType.House && scheduleManager != null)
            {
                bool isStealthy = player != null && player.IsCrouched;
                scheduleManager.OnHouseDoorOpened(houseGroup, isStealthy);
            }
        }
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;
        UpdatePrompt();
        StopAllCoroutines();
        StartCoroutine(RotateDoor(isOpen ? openRotation : closedRotation));
    }

    System.Collections.IEnumerator RotateDoor(Quaternion targetRot)
    {
        isMoving = true;
        while (Quaternion.Angle(transform.rotation, targetRot) > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * openSpeed);
            yield return null;
        }
        transform.rotation = targetRot;
        isMoving = false;
    }

    void UpdatePrompt()
    {
        promptText = isOpen ? "Press E to close door" : "Press E to open door";
    }
} 