
using UnityEngine;

/// <summary>
/// Allows the player to walk through this object
/// while keeping it detectable by shooting raycasts.
///
/// Physical collisions are disabled only between
/// this object's colliders and the player's
/// CharacterController.
///
/// Raycast detection remains unaffected.
/// </summary>
public class ShootablePassThrough : MonoBehaviour
{
    private void Start()
    {
        // Find the player in the scene.
        PlayerMovement player =
            FindFirstObjectByType<PlayerMovement>();

        if (player == null)
            return;

        // Retrieve the player's CharacterController.
        CharacterController controller =
            player.GetComponent<CharacterController>();

        if (controller == null)
            return;

        // Disable physical collisions between the player
        // and every collider belonging to this object.
        foreach (Collider targetCollider in
                 GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(
                controller,
                targetCollider,
                true
            );
        }
    }
}
