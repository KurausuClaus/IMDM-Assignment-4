
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// First-person movement controller.
///
/// Features:
/// - Customizable movement speeds
/// - Ground acceleration and deceleration
/// - Air movement and momentum
/// - Variable-height jumping
/// - Coyote time and jump buffering
/// - Moving platform support
/// - Platform momentum inheritance
/// - External launch functionality
/// - Collectible interaction
/// - DeathZone handling
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(100)]
public class PlayerMovement : MonoBehaviour
{
    // =====================================================
    // GROUND MOVEMENT
    // =====================================================

    [Header("Ground Movement")]

    [SerializeField, Min(0f)]
    private float forwardSpeed = 6f;

    [SerializeField, Min(0f)]
    private float backwardSpeed = 5f;

    [SerializeField, Min(0f)]
    private float strafeSpeed = 6f;

    [SerializeField, Min(0f)]
    private float acceleration = 30f;

    [SerializeField, Min(0f)]
    private float deceleration = 40f;


    // =====================================================
    // AIR MOVEMENT
    // =====================================================

    [Header("Air Movement")]

    [SerializeField, Min(0f)]
    private float airAcceleration = 10f;

    [SerializeField, Min(0f)]
    [Tooltip("Set to 0 to preserve momentum in midair.")]
    private float airDeceleration = 0f;


    // =====================================================
    // JUMPING
    // =====================================================

    [Header("Jumping")]

    [SerializeField, Min(0f)]
    private float jumpHeight = 2f;

    [SerializeField, Min(0f)]
    private float coyoteTime = 0.1f;

    [SerializeField, Min(0f)]
    private float jumpBufferTime = 0.1f;

    [SerializeField, Range(0f, 1f)]
    private float jumpCutMultiplier = 0.4f;


    // =====================================================
    // GRAVITY
    // =====================================================

    [Header("Gravity")]

    [SerializeField, Min(0.01f)]
    private float gravity = 20f;

    [SerializeField, Min(1f)]
    private float fallMultiplier = 1.5f;

    [SerializeField, Min(1f)]
    private float jumpReleaseMultiplier = 2f;

    [SerializeField, Min(0.01f)]
    private float maxFallSpeed = 50f;

    [SerializeField, Min(0f)]
    private float groundStickSpeed = 2f;


    // =====================================================
    // MOVING PLATFORMS
    // =====================================================

    [Header("Moving Platforms")]

    [SerializeField]
    [Tooltip("Move with platforms while standing on them.")]
    private bool rideMovingPlatforms = true;

    [SerializeField]
    [Tooltip("Inherit platform velocity when leaving it.")]
    private bool inheritPlatformMomentum = true;

    [SerializeField, Min(0f)]
    [Tooltip("Multiplier for inherited platform momentum.")]
    private float platformMomentumMultiplier = 1f;

    [SerializeField, Range(0f, 89f)]
    private float maxGroundAngle = 60f;


    // =====================================================
    // INPUT
    // =====================================================

    [Header("Input Actions")]

    [SerializeField]
    private InputActionReference moveAction;

    [SerializeField]
    private InputActionReference jumpAction;

    [SerializeField]
    private InputActionReference interactAction;


    // =====================================================
    // INTERACTION
    // =====================================================

    [Header("Interaction")]

    [SerializeField]
    private Camera playerCamera;

    [SerializeField, Min(0f)]
    private float interactionRange = 3f;


    // =====================================================
    // MOVEMENT STATE
    // =====================================================

    private CharacterController controller;

    private Vector3 horizontalVelocity;

    private Vector3 inheritedHorizontalVelocity;

    private float verticalVelocity;

    private float coyoteTimer;

    private float jumpBufferTimer = -1f;

    private bool jumpedThisFrame;

    private bool isDead;


    // =====================================================
    // EXTERNAL LAUNCH STATE
    // =====================================================

    private bool hasPendingLaunch;

    private Vector3 pendingLaunchVelocity;


    // =====================================================
    // MOVING PLATFORM STATE
    // =====================================================

    private Collider standingCollider;

    private Transform standingTransform;

    private Vector3 platformLocalAnchor;

    private Vector3 lastPlatformAnchorWorld;

    private Vector3 platformVelocity;

    private Vector3 platformMotion;


    // =====================================================
    // COLLISION TRACKING
    // =====================================================

    private Collider groundHitThisFrame;

    private float bestGroundNormalY;


    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (moveAction != null)
            moveAction.action.Enable();

        if (jumpAction != null)
            jumpAction.action.Enable();

        if (interactAction != null)
            interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.action.Disable();

        if (jumpAction != null)
            jumpAction.action.Disable();

        if (interactAction != null)
            interactAction.action.Disable();
    }


    // =====================================================
    // MAIN UPDATE
    // =====================================================

    private void Update()
    {
        if (isDead || moveAction == null || jumpAction == null)
            return;

        float dt = Time.deltaTime;

        if (dt <= 0f)
            return;

        bool wasGrounded = controller.isGrounded;

        jumpedThisFrame = false;

        // Measure the movement of the platform
        // the player was previously standing on.
        UpdatePlatformMovement(wasGrounded, dt);

        if (wasGrounded)
            inheritedHorizontalVelocity = Vector3.zero;

        // Read movement input.
        Vector2 input =
            moveAction.action.ReadValue<Vector2>();

        UpdateHorizontalMovement(
            input,
            wasGrounded,
            dt
        );

        UpdateJumpTimers(
            wasGrounded,
            dt
        );

        UpdateVerticalMovement(
            wasGrounded,
            dt
        );


        // =================================================
        // EXTERNAL LAUNCH
        // =================================================

        // Apply any launch requested by another script.
        //
        // This is separate from ordinary jumping.
        if (hasPendingLaunch)
        {
            hasPendingLaunch = false;

            // Do not also carry the player with the
            // platform during the launch frame.
            jumpedThisFrame = true;

            // Keep horizontal launch velocity separate
            // from player-controlled movement.
            inheritedHorizontalVelocity = new Vector3(
                pendingLaunchVelocity.x,
                0f,
                pendingLaunchVelocity.z
            );

            verticalVelocity =
                pendingLaunchVelocity.y;

            // Prevent buffered jumps from interfering.
            jumpBufferTimer = -1f;
            coyoteTimer = -1f;

            // Detach from the current platform.
            ClearPlatform();
        }


        // =================================================
        // APPLY MOVEMENT
        // =================================================

        Vector3 carry = jumpedThisFrame
            ? Vector3.zero
            : platformMotion;

        ApplyMovement(dt, carry);

        if (!isDead)
            HandleInteraction();
    }


    // =====================================================
    // EXTERNAL LAUNCH API
    // =====================================================

    /// <summary>
    /// Launches the player using a specified world-space
    /// velocity, measured in units per second.
    ///
    /// Example:
    ///
    /// Launch(new Vector3(20f, 12f, 0f));
    ///
    /// This launches the player toward positive X
    /// while also sending them upward.
    ///
    /// The launch is applied during the next Update().
    /// </summary>
    public void Launch(Vector3 velocity)
    {
        pendingLaunchVelocity = velocity;

        hasPendingLaunch = true;
    }


    // =====================================================
    // HORIZONTAL MOVEMENT
    // =====================================================

    private void UpdateHorizontalMovement(
        Vector2 input,
        bool isGrounded,
        float dt)
    {
        input = Vector2.ClampMagnitude(input, 1f);

        bool hasInput = input.sqrMagnitude > 0f;

        float speed = input.y < 0f
            ? backwardSpeed
            : forwardSpeed;

        Vector3 targetVelocity =
            transform.forward * input.y * speed +
            transform.right * input.x * strafeSpeed;

        targetVelocity.y = 0f;

        float movementRate = isGrounded
            ? (hasInput ? acceleration : deceleration)
            : (hasInput ? airAcceleration : airDeceleration);

        if (isGrounded && !hasInput)
        {
            horizontalVelocity = Vector3.zero;
        }
        else
        {
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetVelocity,
                movementRate * dt
            );
        }
    }


    // =====================================================
    // JUMP TIMING
    // =====================================================

    private void UpdateJumpTimers(
        bool isGrounded,
        float dt)
    {
        if (isGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= dt;

        if (jumpAction.action.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= dt;
    }


    // =====================================================
    // VERTICAL MOVEMENT
    // =====================================================

    private void UpdateVerticalMovement(
        bool isGrounded,
        float dt)
    {
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -groundStickSpeed;

        TryJump();

        // Shorten jump when the button is released.
        if (jumpAction.action.WasReleasedThisFrame() &&
            verticalVelocity > 0f)
        {
            verticalVelocity *= jumpCutMultiplier;
        }

        float currentGravity = gravity;

        if (!isGrounded && verticalVelocity < 0f)
        {
            currentGravity *= fallMultiplier;
        }
        else if (verticalVelocity > 0f &&
                 !jumpAction.action.IsPressed())
        {
            currentGravity *= jumpReleaseMultiplier;
        }

        verticalVelocity -= currentGravity * dt;

        verticalVelocity = Mathf.Max(
            verticalVelocity,
            -maxFallSpeed
        );
    }


    // =====================================================
    // JUMP EXECUTION
    // =====================================================

    private void TryJump()
    {
        if (jumpBufferTimer < 0f || coyoteTimer < 0f)
            return;

        jumpedThisFrame = true;

        verticalVelocity = Mathf.Sqrt(
            2f * gravity * jumpHeight
        );

        if (inheritPlatformMomentum &&
            rideMovingPlatforms &&
            standingCollider != null)
        {
            inheritedHorizontalVelocity =
                new Vector3(
                    platformVelocity.x,
                    0f,
                    platformVelocity.z
                ) * platformMomentumMultiplier;

            verticalVelocity +=
                platformVelocity.y *
                platformMomentumMultiplier;
        }

        jumpBufferTimer = -1f;

        coyoteTimer = -1f;
    }


    // =====================================================
    // MOVING PLATFORM TRACKING
    // =====================================================

    private void UpdatePlatformMovement(
        bool wasGrounded,
        float dt)
    {
        platformMotion = Vector3.zero;

        platformVelocity = Vector3.zero;

        if (!rideMovingPlatforms ||
            !wasGrounded ||
            standingCollider == null ||
            !standingCollider.enabled ||
            !standingCollider.gameObject.activeInHierarchy ||
            standingTransform == null)
        {
            ClearPlatform();
            return;
        }

        Vector3 currentAnchorWorld =
            standingTransform.TransformPoint(
                platformLocalAnchor
            );

        platformMotion =
            currentAnchorWorld - lastPlatformAnchorWorld;

        platformVelocity =
            platformMotion / dt;
    }


    private void AttachToPlatform(Collider ground)
    {
        standingCollider = ground;

        standingTransform = ground.transform;

        platformLocalAnchor =
            standingTransform.InverseTransformPoint(
                transform.position
            );

        lastPlatformAnchorWorld =
            standingTransform.TransformPoint(
                platformLocalAnchor
            );
    }


    private void ClearPlatform()
    {
        standingCollider = null;

        standingTransform = null;

        platformMotion = Vector3.zero;

        platformVelocity = Vector3.zero;
    }


    // =====================================================
    // APPLY MOVEMENT
    // =====================================================

    private void ApplyMovement(
        float dt,
        Vector3 carry)
    {
        groundHitThisFrame = null;

        bestGroundNormalY = Mathf.Cos(
            maxGroundAngle * Mathf.Deg2Rad
        );

        Vector3 velocity =
            horizontalVelocity +
            inheritedHorizontalVelocity;

        velocity.y = verticalVelocity;

        // Apply ordinary movement and platform carry.
        CollisionFlags flags =
            controller.Move(velocity * dt + carry);

        // Stop upward movement after hitting a ceiling.
        if ((flags & CollisionFlags.Above) != 0 &&
            verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }

        bool onFloor =
            (flags & CollisionFlags.Below) != 0;

        if (onFloor)
        {
            inheritedHorizontalVelocity = Vector3.zero;
        }

        // Store the supporting platform.
        if (!jumpedThisFrame &&
            onFloor &&
            groundHitThisFrame != null &&
            rideMovingPlatforms)
        {
            AttachToPlatform(groundHitThisFrame);
        }
        else
        {
            // Preserve platform momentum when walking
            // off an edge without jumping.
            if (!jumpedThisFrame &&
                !onFloor &&
                inheritPlatformMomentum &&
                rideMovingPlatforms &&
                standingCollider != null)
            {
                inheritedHorizontalVelocity =
                    new Vector3(
                        platformVelocity.x,
                        0f,
                        platformVelocity.z
                    ) * platformMomentumMultiplier;
            }

            ClearPlatform();
        }
    }


    // =====================================================
    // COLLISION DETECTION
    // =====================================================


    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // =====================================================
        // DEATH ZONE DETECTION
        // =====================================================

        CheckForDeath(hit.collider);

        if (isDead)
            return;


        // =====================================================
        // GROUND DETECTION
        // =====================================================

        float normalY = hit.normal.y;

        // Only count supporting ground.
        if (normalY > bestGroundNormalY &&
            hit.collider != null &&
            !hit.collider.isTrigger)
        {
            bestGroundNormalY = normalY;

            groundHitThisFrame = hit.collider;
        }


        // =====================================================
        // WALL COLLISION
        // =====================================================

        // Only process approximately vertical walls.
        if (Mathf.Abs(normalY) < 0.5f)
        {
            // Horizontal normal pointing away from the wall.
            Vector3 wallNormal = new Vector3(
                hit.normal.x,
                0f,
                hit.normal.z
            ).normalized;

            // Cancel inherited momentum directed into the wall.
            float inheritedIntoWall =
                Vector3.Dot(
                    inheritedHorizontalVelocity,
                    wallNormal
                );

            if (inheritedIntoWall < 0f)
            {
                inheritedHorizontalVelocity -=
                    wallNormal * inheritedIntoWall;
            }

            // Cancel ordinary movement velocity directed
            // into the wall as well.
            float movementIntoWall =
                Vector3.Dot(
                    horizontalVelocity,
                    wallNormal
                );

            if (movementIntoWall < 0f)
            {
                horizontalVelocity -=
                    wallNormal * movementIntoWall;
            }
        }
    }



    // =====================================================
    // COLLECTIBLE INTERACTION
    // =====================================================

    private void HandleInteraction()
    {
        if (interactAction == null ||
            playerCamera == null ||
            !interactAction.action.WasPressedThisFrame())
        {
            return;
        }

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (!Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide))
        {
            return;
        }

        Transform current =
            hit.collider.transform;

        while (current != null)
        {
            if (current.CompareTag("Collectible"))
            {
                Debug.Log(
                    $"Collected: {current.name}"
                );

                Destroy(current.gameObject);

                return;
            }

            current = current.parent;
        }
    }


    // =====================================================
    // DEATH ZONES
    // =====================================================

    private void OnTriggerEnter(Collider other)
    {
        CheckForDeath(other);
    }


    private void CheckForDeath(Collider other)
    {
        if (isDead || other == null)
            return;

        Transform current = other.transform;

        while (current != null)
        {
            if (current.CompareTag("DeathZone"))
            {
                Die();

                return;
            }

            current = current.parent;
        }
    }


    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("Player died!");

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }
}
