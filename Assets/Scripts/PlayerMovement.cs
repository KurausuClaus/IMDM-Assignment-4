using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// First-person movement with responsive ground movement, air control,
/// variable jumping, moving-platform riding and momentum inheritance,
/// collectible interaction, and DeathZone handling.
///
/// Moving platforms do NOT need a special tag or script. Their collider must
/// stay enabled while solid, and their Transform must move (directly or via
/// a parent / Rigidbody). This component records the surface touched beneath
/// the character and follows its translation and rotation.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DefaultExecutionOrder(100)]
public class PlayerMovement : MonoBehaviour
{
    // =========================================================
    // GROUND MOVEMENT
    // =========================================================

    [Header("Ground Movement")]
    [SerializeField, Min(0f)] private float forwardSpeed = 6f;
    [SerializeField, Min(0f)] private float backwardSpeed = 5f;
    [SerializeField, Min(0f)] private float strafeSpeed = 6f;
    [SerializeField, Min(0f)] private float acceleration = 30f;
    [SerializeField, Min(0f)] private float deceleration = 40f;

    // =========================================================
    // AIR MOVEMENT
    // =========================================================

    [Header("Air Movement")]
    [SerializeField, Min(0f)] private float airAcceleration = 10f;
    [SerializeField, Min(0f)]
    [Tooltip("Set to 0 to preserve player-controlled air momentum when releasing movement keys.")]
    private float airDeceleration = 0f;

    // =========================================================
    // JUMPING
    // =========================================================

    [Header("Jumping")]
    [SerializeField, Min(0f)] private float jumpHeight = 2f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
    [SerializeField, Min(0f)] private float jumpBufferTime = 0.1f;
    [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.4f;

    // =========================================================
    // GRAVITY
    // =========================================================

    [Header("Gravity")]
    [SerializeField, Min(0.01f)] private float gravity = 20f;
    [SerializeField, Min(1f)] private float fallMultiplier = 1.5f;
    [SerializeField, Min(1f)] private float jumpReleaseMultiplier = 2f;
    [SerializeField, Min(0.01f)] private float maxFallSpeed = 50f;
    [SerializeField, Min(0f)] private float groundStickSpeed = 2f;

    // =========================================================
    // MOVING PLATFORMS
    // =========================================================

    [Header("Moving Platforms")]
    [SerializeField]
    [Tooltip("Carry the player along when standing on a translating or rotating platform.")]
    private bool rideMovingPlatforms = true;

    [SerializeField]
    [Tooltip("Inherit platform speed when jumping or running off an edge.")]
    private bool inheritPlatformMomentum = true;

    [SerializeField, Min(0f)]
    [Tooltip("1 = full platform velocity; 0 = no inherited velocity; >1 = amplified launch.")]
    private float platformMomentumMultiplier = 1f;

    [SerializeField, Range(0f, 89f)]
    [Tooltip("A collision must have a sufficiently upward-facing normal to count as floor.")]
    private float maxGroundAngle = 60f;

    // =========================================================
    // INPUT / INTERACTION
    // =========================================================

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference interactAction;

    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField, Min(0f)] private float interactionRange = 3f;

    // =========================================================
    // INTERNAL STATE
    // =========================================================

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float coyoteTimer;
    private float jumpBufferTimer = -1f;
    private bool isDead;
    private bool jumpedThisFrame;

    // The player's own horizontal speed is separate from velocity inherited
    // from a launch platform. This prevents air steering from immediately
    // cancelling a high-speed platform launch.
    private Vector3 inheritedHorizontalVelocity;

    // Platform attachment uses a point in the platform collider's LOCAL space.
    // This tracks both linear movement and rotation without parenting the player.
    private Collider standingCollider;
    private Transform standingTransform;
    private Vector3 platformLocalAnchor;
    private Vector3 lastPlatformAnchorWorld;
    private Vector3 platformVelocity;
    private Vector3 platformMotion;

    // OnControllerColliderHit fires during CharacterController.Move().
    private Collider groundHitThisFrame;
    private float bestGroundNormalY;

    // =========================================================
    // UNITY LIFECYCLE
    // =========================================================

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
        if (interactAction != null) interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
        if (interactAction != null) interactAction.action.Disable();
    }

    private void Update()
    {
        if (isDead || moveAction == null || jumpAction == null)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        // isGrounded reports the result of last frame's Move().
        bool wasGrounded = controller.isGrounded;
        jumpedThisFrame = false;

        // Measure where the attached platform carried the player's local
        // standing point since the previous frame.
        UpdatePlatformMovement(wasGrounded, dt);

        if (wasGrounded)
            inheritedHorizontalVelocity = Vector3.zero;

        Vector2 input = moveAction.action.ReadValue<Vector2>();
        UpdateHorizontalMovement(input, wasGrounded, dt);
        UpdateJumpTimers(wasGrounded, dt);
        UpdateVerticalMovement(wasGrounded, dt);

        // A jump inherits platform velocity, rather than both inheriting
        // velocity AND taking another full platform displacement this frame.
        Vector3 carry = jumpedThisFrame ? Vector3.zero : platformMotion;
        ApplyMovement(dt, carry);

        if (!isDead)
            HandleInteraction();
    }

    // =========================================================
    // HORIZONTAL MOVEMENT
    // =========================================================

    private void UpdateHorizontalMovement(Vector2 input, bool isGrounded, float dt)
    {
        input = Vector2.ClampMagnitude(input, 1f);
        bool hasInput = input.sqrMagnitude > 0f;

        float speed = input.y < 0f ? backwardSpeed : forwardSpeed;
        Vector3 targetVelocity =
            transform.forward * input.y * speed +
            transform.right * input.x * strafeSpeed;
        targetVelocity.y = 0f;

        float movementRate = isGrounded
            ? (hasInput ? acceleration : deceleration)
            : (hasInput ? airAcceleration : airDeceleration);

        // Immediate stop on the ground, including when the platform moves.
        // The platform's motion is carried separately from user input.
        if (isGrounded && !hasInput)
            horizontalVelocity = Vector3.zero;
        else
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity, targetVelocity, movementRate * dt);
    }

    // =========================================================
    // JUMPING / GRAVITY
    // =========================================================

    private void UpdateJumpTimers(bool isGrounded, float dt)
    {
        if (isGrounded) coyoteTimer = coyoteTime;
        else coyoteTimer -= dt;

        if (jumpAction.action.WasPressedThisFrame())
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= dt;
    }

    private void UpdateVerticalMovement(bool isGrounded, float dt)
    {
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -groundStickSpeed;

        TryJump();

        if (jumpAction.action.WasReleasedThisFrame() && verticalVelocity > 0f)
            verticalVelocity *= jumpCutMultiplier;

        float currentGravity = gravity;
        if (!isGrounded && verticalVelocity < 0f)
            currentGravity *= fallMultiplier;
        else if (verticalVelocity > 0f && !jumpAction.action.IsPressed())
            currentGravity *= jumpReleaseMultiplier;

        verticalVelocity -= currentGravity * dt;
        verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);
    }

    private void TryJump()
    {
        if (jumpBufferTimer < 0f || coyoteTimer < 0f)
            return;

        jumpedThisFrame = true;
        verticalVelocity = Mathf.Sqrt(2f * gravity * jumpHeight);

        if (inheritPlatformMomentum && rideMovingPlatforms &&
            standingCollider != null)
        {
            // Add the platform's velocity without affecting air steering.
            // If this is a coyote-time jump after stepping off, keep the
            // inherited horizontal speed that was already assigned.
            inheritedHorizontalVelocity = new Vector3(
                platformVelocity.x, 0f, platformVelocity.z)
                * platformMomentumMultiplier;
            verticalVelocity += platformVelocity.y * platformMomentumMultiplier;
        }

        jumpBufferTimer = -1f;
        coyoteTimer = -1f;
    }

    // =========================================================
    // MOVING PLATFORM TRACKING
    // =========================================================

    private void UpdatePlatformMovement(bool wasGrounded, float dt)
    {
        platformMotion = Vector3.zero;
        platformVelocity = Vector3.zero;

        if (!rideMovingPlatforms || !wasGrounded ||
            standingCollider == null || !standingCollider.enabled ||
            !standingCollider.gameObject.activeInHierarchy ||
            standingTransform == null)
        {
            ClearPlatform();
            return;
        }

        Vector3 currentAnchorWorld =
            standingTransform.TransformPoint(platformLocalAnchor);

        platformMotion = currentAnchorWorld - lastPlatformAnchorWorld;
        platformVelocity = platformMotion / dt;
    }

    private void AttachToPlatform(Collider ground)
    {
        standingCollider = ground;
        standingTransform = ground.transform;

        // Refresh every frame, so the player can walk around on the platform.
        platformLocalAnchor =
            standingTransform.InverseTransformPoint(transform.position);
        lastPlatformAnchorWorld =
            standingTransform.TransformPoint(platformLocalAnchor);
    }

    private void ClearPlatform()
    {
        standingCollider = null;
        standingTransform = null;
        platformMotion = Vector3.zero;
        platformVelocity = Vector3.zero;
    }

    // =========================================================
    // APPLY MOVEMENT
    // =========================================================

    private void ApplyMovement(float dt, Vector3 carry)
    {
        // Gather ground contacts from THIS move only.
        groundHitThisFrame = null;
        bestGroundNormalY = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);

        Vector3 velocity = horizontalVelocity + inheritedHorizontalVelocity;
        velocity.y = verticalVelocity;

        // One controller.Move call handles both our input and platform carry.
        CollisionFlags flags = controller.Move(velocity * dt + carry);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
            verticalVelocity = 0f;

        bool onFloor = (flags & CollisionFlags.Below) != 0;

        // Landing removes any inherited air momentum, so releasing movement
        // keys stops immediately and we don't slide off narrow platforms.
        if (onFloor)
            inheritedHorizontalVelocity = Vector3.zero;

        // If we're standing on something, remember it for next frame.
        if (!jumpedThisFrame && onFloor && groundHitThisFrame != null &&
            rideMovingPlatforms)
        {
            AttachToPlatform(groundHitThisFrame);
        }
        else
        {
            // Walking off an edge transfers the platform's last velocity.
            if (!jumpedThisFrame && !onFloor && inheritPlatformMomentum &&
                rideMovingPlatforms && standingCollider != null)
            {
                inheritedHorizontalVelocity = new Vector3(
                    platformVelocity.x, 0f, platformVelocity.z)
                    * platformMomentumMultiplier;
            }

            ClearPlatform();
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        CheckForDeath(hit.collider);
        if (isDead)
            return;

        // Only record supporting ground, not walls or ceilings.
        float normalY = hit.normal.y;
        if (normalY > bestGroundNormalY && hit.collider != null &&
            !hit.collider.isTrigger)
        {
            bestGroundNormalY = normalY;
            groundHitThisFrame = hit.collider;
        }
    }

    // =========================================================
    // COLLECTIBLES
    // =========================================================

    private void HandleInteraction()
    {
        if (interactAction == null || playerCamera == null ||
            !interactAction.action.WasPressedThisFrame())
            return;

        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            return;

        Transform current = hit.collider.transform;
        while (current != null)
        {
            if (current.CompareTag("Collectible"))
            {
                Debug.Log($"Collected: {current.name}");
                Destroy(current.gameObject);
                return;
            }
            current = current.parent;
        }
    }

    // =========================================================
    // DEATH ZONES
    // =========================================================

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
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
