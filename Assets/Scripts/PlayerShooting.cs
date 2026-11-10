
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles hitscan shooting from the player's camera.
///
/// When the Shoot action is pressed:
/// 1. Create a ray originating from the camera.
/// 2. Cast it forward up to the maximum shooting distance.
/// 3. Check whether it intersects a Collider.
/// 4. Report the result in Unity's Console.
///
/// No physical projectile is created.
/// </summary>
public class PlayerShooting : MonoBehaviour
{
    // =====================================================
    // SHOOTING SETTINGS
    // =====================================================

    [Header("Shooting Settings")]

    [SerializeField, Min(0.1f)]
    [Tooltip("Maximum distance a shot can travel.")]
    private float shootingRange = 100f;

    [SerializeField]
    [Tooltip("Camera from which shots originate.")]
    private Camera playerCamera;


    // =====================================================
    // INPUT SETTINGS
    // =====================================================

    [Header("Input Actions")]

    [SerializeField]
    private InputActionReference shootAction;


    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void OnEnable()
    {
        if (shootAction != null)
            shootAction.action.Enable();
    }

    private void OnDisable()
    {
        if (shootAction != null)
            shootAction.action.Disable();
    }

    private void Update()
    {
        if (shootAction == null || playerCamera == null)
            return;

        // Only allow shooting while the mouse is captured.
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        // Detect a new press of the shooting button.
        if (shootAction.action.WasPressedThisFrame())
        {
            Shoot();
        }
    }


    // =====================================================
    // SHOOTING LOGIC
    // =====================================================

    /// <summary>
    /// Casts a ray forward from the camera and checks
    /// whether it collides with an object.
    /// </summary>
    private void Shoot()
    {
        // Create a ray using the camera's position
        // and forward-facing direction.
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        // Attempt to find the first Collider along the ray.
        bool didHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            shootingRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        if (didHit)
        {
            // Report which object was hit.
            Debug.Log(
                $"Hit: {hit.collider.gameObject.name}"
            );

            // Show the ray in the Scene view.
            Debug.DrawRay(
                ray.origin,
                ray.direction * hit.distance,
                Color.green,
                1f
            );
        }
        else
        {
            // Report a missed shot.
            Debug.Log("Shot missed!");

            Debug.DrawRay(
                ray.origin,
                ray.direction * shootingRange,
                Color.red,
                1f
            );
        }
    }
}
