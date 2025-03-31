using UnityEngine;

public class FollowPlayerUI : MonoBehaviour
{
    public Transform player; // Reference to the player (or camera if in VR)
    public Vector3 offset = new Vector3(0, 1.5f, 2f); // Offset position from the player
    public float followSpeed = 5f; // Speed at which the panel follows the player
    public bool isVisible = false; // Toggle visibility state

    private Canvas canvas; // Reference to the UI canvas

    void Start()
    {
        // Get the Canvas component
        canvas = GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = isVisible; // Set initial visibility
        }
    }

    void Update()
    {
        if (isVisible)
        {
            // Calculate the target position based on player's position and offset
            Vector3 targetPosition = player.position + player.forward * offset.z + player.up * offset.y;

            // Smoothly move the panel to the target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);

            // Make the panel face the player
            transform.LookAt(player);
            transform.Rotate(0, 180, 0); // Adjust rotation if needed to face properly
        }
    }

    public void ToggleVisibility()
    {
        isVisible = !isVisible;
        if (canvas != null)
        {
            canvas.enabled = isVisible;

            if (isVisible)
            {
                // Move the panel instantly to the correct position when toggled on
                transform.position = player.position + player.forward * offset.z + player.up * offset.y;
            }
        }
    }
}
