using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public float minWaitTime = 2f;         // Minimum wait time at this waypoint.
    public float maxWaitTime = 5f;         // Maximum wait time at this waypoint.

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmos()
    {
        // Draw a yellow wire sphere to represent the view distance.
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 1f);
    }

    void OnDrawGizmosSelected()
    {
        // Draw a yellow wire sphere to represent the view distance.
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 1f);
    }
}
