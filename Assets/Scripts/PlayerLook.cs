
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls first-person camera rotation using mouse movement.
///
/// Horizontal mouse movement rotates the player.
/// Vertical mouse movement rotates only the camera.
///
/// Attach this script to the Player GameObject,
/// not to the camera itself.
/// </summary>
public class PlayerLook : MonoBehaviour
{
    // =====================================================
    // CAMERA SETTINGS
    // =====================================================

    [Header("Camera Settings")]

    [SerializeField]
    [Tooltip("The camera attached to the player.")]
    private Transform playerCamera;

    [SerializeField, Min(0f)]
    [Tooltip("Degrees of rotation per unit of mouse movement.")]
    private float mouseSensitivity = 0.15f;

    [SerializeField, Range(0f, 89f)]
    [Tooltip("Maximum angle the camera can look up or down.")]
    private float verticalLookLimit = 85f;

    [SerializeField]
    [Tooltip("Reverse the direction of vertical mouse movement.")]
    private bool invertY = false;


    // =====================================================
    // INPUT SETTINGS
    // =====================================================

    [Header("Input Actions")]

    [SerializeField]
    private InputActionReference lookAction;


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    // Current vertical camera angle, measured in degrees.
    private float cameraPitch;


    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void Awake()
    {
        // Read the camera's initial local vertical rotation.
        if (playerCamera != null)
        {
            cameraPitch = Mathf.DeltaAngle(
                0f,
                playerCamera.localEulerAngles.x
            );
        }
    }

    private void OnEnable()
    {
        if (lookAction != null)
            lookAction.action.Enable();
    }

    private void OnDisable()
    {
        if (lookAction != null)
            lookAction.action.Disable();

        UnlockCursor();
    }

    private void Update()
    {
        HandleCursor();

        // Do not rotate the camera when the cursor is free.
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (playerCamera == null || lookAction == null)
            return;

        Vector2 mouseInput =
            lookAction.action.ReadValue<Vector2>();

        RotatePlayer(mouseInput.x);
        RotateCamera(mouseInput.y);
    }


    // =====================================================
    // HORIZONTAL ROTATION
    // =====================================================

    /// <summary>
    /// Rotates the entire player around the world's Y axis.
    ///
    /// This also changes the direction of movement because
    /// PlayerMovement uses transform.forward and transform.right.
    /// </summary>
    private void RotatePlayer(float mouseX)
    {
        float horizontalRotation =
            mouseX * mouseSensitivity;

        transform.Rotate(
            Vector3.up,
            horizontalRotation,
            Space.World
        );
    }


    // =====================================================
    // VERTICAL ROTATION
    // =====================================================

    /// <summary>
    /// Rotates only the camera around its local X axis.
    ///
    /// The player itself remains upright.
    ///
    /// Vertical rotation is clamped to prevent the player
    /// from looking beyond the allowed viewing angles.
    /// </summary>
    private void RotateCamera(float mouseY)
    {
        float verticalRotation =
            mouseY * mouseSensitivity;

        // In normal FPS controls, moving the mouse upward
        // makes the camera look upward.
        if (!invertY)
            cameraPitch -= verticalRotation;
        else
            cameraPitch += verticalRotation;

        // Prevent excessive upward or downward rotation.
        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -verticalLookLimit,
            verticalLookLimit
        );

        // Apply rotation relative to the player.
        playerCamera.localRotation = Quaternion.Euler(
            cameraPitch,
            0f,
            0f
        );
    }


    // =====================================================
    // CURSOR MANAGEMENT
    // =====================================================

    /// <summary>
    /// Click to lock the mouse inside the Game window.
    /// Press Escape to unlock it.
    /// </summary>
    private void HandleCursor()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
            return;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
