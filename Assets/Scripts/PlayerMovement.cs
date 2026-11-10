
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls first-person player movement using Unity's CharacterController.
///
/// Features:
/// - Independent forward, backward, and sideways movement speeds
/// - Configurable ground acceleration and deceleration
/// - Configurable air acceleration and deceleration
/// - Jumping with adjustable height
/// - Variable jump height based on how long Jump is held
/// - Adjustable gravity and falling speed
/// - Coyote time and jump buffering
///
/// Movement is relative to the player's orientation.
/// Camera rotation should be handled by a separate script.
///
/// This script requires:
/// - A CharacterController component
/// - Move and Jump actions from Unity's Input System
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // =========================================================
    // GROUND MOVEMENT SETTINGS
    // =========================================================

    [Header("Ground Movement")]

    [SerializeField, Min(0f)]
    [Tooltip("Maximum forward movement speed.")]
    private float forwardSpeed = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("Maximum backward movement speed.")]
    private float backwardSpeed = 5f;

    [SerializeField, Min(0f)]
    [Tooltip("Maximum sideways movement speed.")]
    private float strafeSpeed = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("How quickly the player accelerates on the ground.")]
    private float acceleration = 30f;

    [SerializeField, Min(0f)]
    [Tooltip("How quickly the player stops on the ground.")]
    private float deceleration = 40f;


    // =========================================================
    // AIR MOVEMENT SETTINGS
    // =========================================================

    [Header("Air Movement")]

    [SerializeField, Min(0f)]
    [Tooltip("How quickly horizontal velocity can change in the air.")]
    private float airAcceleration = 10f;

    [SerializeField, Min(0f)]
    [Tooltip("How quickly horizontal movement slows in the air when no input is held.")]
    private float airDeceleration = 2f;


    // =========================================================
    // JUMP SETTINGS
    // =========================================================

    [Header("Jumping")]

    [SerializeField, Min(0f)]
    [Tooltip("Approximate maximum jump height when Jump is held.")]
    private float jumpHeight = 2f;

    [SerializeField, Min(0f)]
    [Tooltip("Extra time after leaving a ledge during which jumping is allowed.")]
    private float coyoteTime = 0.1f;

    [SerializeField, Min(0f)]
    [Tooltip("How long a jump input is remembered before landing.")]
    private float jumpBufferTime = 0.1f;


    // =========================================================
    // GRAVITY SETTINGS
    // =========================================================

    [Header("Gravity")]

    [SerializeField, Min(0.01f)]
    [Tooltip("Base gravitational acceleration. Enter a positive number.")]
    private float gravity = 20f;

    [SerializeField, Min(1f)]
    [Tooltip("Multiplier applied to gravity while falling.")]
    private float fallMultiplier = 1.5f;

    [SerializeField, Min(1f)]
    [Tooltip("Multiplier applied to gravity when Jump is released during ascent.")]
    private float jumpReleaseMultiplier = 2f;

    [SerializeField, Min(0.01f)]
    [Tooltip("Maximum downward falling speed.")]
    private float maxFallSpeed = 50f;

    [SerializeField, Min(0f)]
    [Tooltip("Small downward velocity used to maintain ground contact.")]
    private float groundStickSpeed = 2f;


    // =========================================================
    // INPUT SETTINGS
    // =========================================================

    [Header("Input Actions")]

    [SerializeField]
    private InputActionReference moveAction;

    [SerializeField]
    private InputActionReference jumpAction;


    // =========================================================
    // INTERNAL STATE
    // =========================================================

    // Reference to the player's CharacterController.
    private CharacterController controller;

    // Current horizontal velocity, in world coordinates.
    // This is stored between frames so acceleration is gradual.
    private Vector3 horizontalVelocity;

    // Current upward/downward velocity.
    // Positive = rising; negative = falling.
    private float verticalVelocity;

    // Remaining time during which a jump is allowed
    // after the player leaves the ground.
    private float coyoteTimer;

    // Remaining time for a buffered jump input.
    private float jumpBufferTimer = -1f;


    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    /// <summary>
    /// Retrieves required components when the object initializes.
    /// </summary>
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }


    /// <summary>
    /// Enables input actions when this component becomes active.
    /// </summary>
    private void OnEnable()
    {
        if (moveAction != null)
            moveAction.action.Enable();

        if (jumpAction != null)
            jumpAction.action.Enable();
    }


    /// <summary>
    /// Disables input actions when this component becomes inactive.
    /// </summary>
    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.action.Disable();

        if (jumpAction != null)
            jumpAction.action.Disable();
    }


    /// <summary>
    /// Updates player movement once per rendered frame.
    ///
    /// Order of operations:
    /// 1. Read movement input.
    /// 2. Calculate horizontal velocity.
    /// 3. Update jump timing.
    /// 4. Calculate vertical velocity.
    /// 5. Apply the final movement.
    /// </summary>
    private void Update()
    {
        if (moveAction == null || jumpAction == null)
            return;

        float deltaTime = Time.deltaTime;

        // isGrounded describes the previous Move() result.
        bool isGrounded = controller.isGrounded;

        Vector2 movementInput =
            moveAction.action.ReadValue<Vector2>();

        UpdateHorizontalMovement(
            movementInput,
            isGrounded,
            deltaTime
        );

        UpdateJumpTimers(isGrounded, deltaTime);

        UpdateVerticalMovement(isGrounded, deltaTime);

        ApplyMovement(deltaTime);
    }


    // =========================================================
    // HORIZONTAL MOVEMENT
    // =========================================================

    /// <summary>
    /// Updates horizontal velocity using player input.
    ///
    /// Movement is relative to the player's orientation.
    /// Different speeds can be assigned to forward, backward,
    /// and sideways movement.
    ///
    /// Grounded and airborne movement use different
    /// acceleration and deceleration values.
    /// </summary>
    private void UpdateHorizontalMovement(
        Vector2 input,
        bool isGrounded,
        float deltaTime)
    {
        // Prevent diagonal input from exceeding magnitude 1.
        input = Vector2.ClampMagnitude(input, 1f);

        // Determine whether forward or backward speed applies.
        float selectedSpeed = forwardSpeed;

        if (input.y < 0f)
        {
            selectedSpeed = backwardSpeed;
        }

        // Calculate movement relative to player orientation.
        Vector3 forwardMovement =
            transform.forward * input.y * selectedSpeed;

        Vector3 sidewaysMovement =
            transform.right * input.x * strafeSpeed;

        // Combined desired horizontal velocity.
        Vector3 targetVelocity =
            forwardMovement + sidewaysMovement;

        // Horizontal movement must not modify vertical velocity.
        targetVelocity.y = 0f;

        // Determine whether movement input is being supplied.
        bool hasMovementInput = input.sqrMagnitude > 0f;

        // Choose an acceleration/deceleration rate.
        float movementRate;

        if (isGrounded)
        {
            movementRate = hasMovementInput
                ? acceleration
                : deceleration;
        }
        else
        {
            movementRate = hasMovementInput
                ? airAcceleration
                : airDeceleration;
        }

        // Gradually approach the desired horizontal velocity.
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetVelocity,
            movementRate * deltaTime
        );
    }


    // =========================================================
    // JUMP TIMING
    // =========================================================

    /// <summary>
    /// Updates the timers used for coyote time and jump buffering.
    ///
    /// Coyote time allows jumping shortly after leaving a ledge.
    ///
    /// Jump buffering allows an early jump press to be remembered
    /// briefly, so the jump can execute when the player lands.
    /// </summary>
    private void UpdateJumpTimers(
        bool isGrounded,
        float deltaTime)
    {
        // Refresh coyote time whenever grounded.
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= deltaTime;
        }

        // Remember a fresh jump press.
        if (jumpAction.action.WasPressedThisFrame())
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= deltaTime;
        }
    }


    // =========================================================
    // VERTICAL MOVEMENT
    // =========================================================

    /// <summary>
    /// Updates the player's vertical velocity.
    ///
    /// Responsible for:
    /// - Maintaining contact with the ground
    /// - Beginning jumps
    /// - Applying gravity
    /// - Increasing falling speed
    /// - Shortening jumps when the button is released
    /// - Limiting maximum falling speed
    /// </summary>
    private void UpdateVerticalMovement(
        bool isGrounded,
        float deltaTime)
    {
        // Maintain slight downward velocity when grounded.
        // This prevents accumulated downward speed from persisting.
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -groundStickSpeed;
        }

        // Attempt to execute a jump.
        TryJump();

        // Determine the gravity for the current situation.
        float currentGravity = gravity;

        if (!isGrounded && verticalVelocity < 0f)
        {
            // Apply additional gravity while falling.
            currentGravity *= fallMultiplier;
        }
        else if (
            verticalVelocity > 0f &&
            !jumpAction.action.IsPressed())
        {
            // Releasing Jump early produces a shorter jump.
            currentGravity *= jumpReleaseMultiplier;
        }

        // Gravity continuously reduces upward velocity
        // and increases downward velocity.
        verticalVelocity -= currentGravity * deltaTime;

        // Do not allow falling speed to exceed the limit.
        verticalVelocity = Mathf.Max(
            verticalVelocity,
            -maxFallSpeed
        );
    }


    /// <summary>
    /// Starts a jump if both conditions are satisfied:
    ///
    /// 1. A valid jump input is available.
    /// 2. The player is grounded or within coyote time.
    ///
    /// The initial upward velocity is calculated from
    /// the desired jump height and gravitational acceleration.
    /// </summary>
    private void TryJump()
    {
        bool hasJumpInput = jumpBufferTimer >= 0f;
        bool canJump = coyoteTimer >= 0f;

        if (!hasJumpInput || !canJump)
            return;

        // Initial velocity needed to reach the desired height.
        verticalVelocity = Mathf.Sqrt(
            2f * gravity * jumpHeight
        );

        // Consume the jump input and grounded grace period.
        // This prevents the same input from triggering again.
        jumpBufferTimer = -1f;
        coyoteTimer = -1f;
    }


    // =========================================================
    // APPLY MOVEMENT
    // =========================================================

    /// <summary>
    /// Combines horizontal and vertical velocity and moves
    /// the CharacterController.
    ///
    /// CharacterController.Move() takes displacement rather
    /// than velocity, so we multiply by deltaTime.
    ///
    /// Also cancels upward velocity if the player hits a ceiling.
    /// </summary>
    private void ApplyMovement(float deltaTime)
    {
        // Combine independent movement components.
        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        // Convert velocity into displacement for this frame.
        Vector3 displacement = finalVelocity * deltaTime;

        // Move while respecting CharacterController collisions.
        CollisionFlags collisions = controller.Move(displacement);

        // Stop upward movement when hitting a ceiling.
        bool hitCeiling =
            (collisions & CollisionFlags.Above) != 0;

        if (hitCeiling && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }
    }
}
