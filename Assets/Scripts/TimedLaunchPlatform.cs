
using UnityEngine;

/// <summary>
/// A platform that remains stationary after activation,
/// then moves rapidly and launches the player when
/// its activation timer reaches a specified threshold.
///
/// Designed to work with ReactToTarget.
///
/// The player is launched only if standing on the
/// platform when the launch begins.
///
/// After the activation timer expires, the platform
/// returns to its original position.
/// </summary>
[RequireComponent(typeof(ReactToTarget))]
[DefaultExecutionOrder(-100)]
public class TimedLaunchPlatform : MonoBehaviour
{
    // =====================================================
    // TIMING SETTINGS
    // =====================================================

    [Header("Timing")]

    [SerializeField, Min(0.01f)]
    [Tooltip("Must match Revert Delay in ReactToTarget.")]
    private float activationDurationSeconds = 10f;

    [SerializeField, Min(0f)]
    [Tooltip("Launch when this many seconds remain.")]
    private float secondsRemainingToLaunch = 2f;


    // =====================================================
    // PLATFORM MOVEMENT
    // =====================================================

    [Header("Platform Movement")]

    [SerializeField]
    [Tooltip("World-space direction of platform travel.")]
    private Vector3 moveDirection = Vector3.right;

    [SerializeField, Min(0f)]
    [Tooltip("Platform speed in units per second.")]
    private float platformSpeed = 30f;


    // =====================================================
    // PLAYER LAUNCH
    // =====================================================

    [Header("Player Launch")]

    [SerializeField, Min(0f)]
    [Tooltip("Horizontal launch velocity.")]
    private float horizontalLaunchSpeed = 30f;

    [SerializeField, Min(0f)]
    [Tooltip("Upward launch velocity.")]
    private float upwardLaunchSpeed = 12f;


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    private ReactToTarget reaction;

    private PlayerMovement player;

    private CharacterController playerController;

    private Vector3 startingPosition;

    private bool timerWasRunning;

    private bool hasActivatedLaunch;


    // =====================================================
    // INITIALIZATION
    // =====================================================

    private void Awake()
    {
        reaction = GetComponent<ReactToTarget>();

        startingPosition = transform.position;

        // Find the player once.
        player = FindFirstObjectByType<PlayerMovement>();

        if (player != null)
        {
            playerController =
                player.GetComponent<CharacterController>();
        }
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        // Read the existing activation countdown.
        bool timerRunning =
            reaction.IsRevertCountdownActive;


        // =================================================
        // TIMER INACTIVE
        // =================================================

        if (!timerRunning)
        {
            // Restore the platform after deactivation.
            if (timerWasRunning)
            {
                transform.position = startingPosition;

                hasActivatedLaunch = false;
            }

            timerWasRunning = false;

            return;
        }


        // =================================================
        // TIMER JUST STARTED
        // =================================================

        if (!timerWasRunning)
        {
            transform.position = startingPosition;

            hasActivatedLaunch = false;
        }

        timerWasRunning = true;


        // =================================================
        // REMAINING TIME
        // =================================================

        float secondsRemaining =
            reaction.RevertRemainingNormalized *
            activationDurationSeconds;


        // =================================================
        // TRIGGER MOVEMENT AND LAUNCH
        // =================================================

        if (!hasActivatedLaunch &&
            secondsRemaining <= secondsRemainingToLaunch)
        {
            hasActivatedLaunch = true;

            TryLaunchPlayer();
        }


        // =================================================
        // MOVE PLATFORM
        // =================================================

        if (hasActivatedLaunch &&
            moveDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 direction =
                moveDirection.normalized;

            transform.position +=
                direction *
                platformSpeed *
                Time.deltaTime;
        }
    }


    // =====================================================
    // PLAYER LAUNCH
    // =====================================================

    /// <summary>
    /// Checks whether the player is standing on this
    /// platform, then applies a launch velocity.
    ///
    /// This raycast runs only once per activation.
    /// </summary>
    private void TryLaunchPlayer()
    {
        if (player == null ||
            playerController == null)
        {
            return;
        }

        Bounds bounds =
            playerController.bounds;

        // Raycast downward from the player's center.
        Vector3 origin = bounds.center;

        float distance =
            bounds.extents.y + 0.15f;

        if (!Physics.Raycast(
            origin,
            Vector3.down,
            out RaycastHit hit,
            distance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // Verify the player is standing specifically
        // on this platform.
        Transform hitTransform =
            hit.collider.transform;

        bool standingOnThisPlatform =
            hitTransform == transform ||
            hitTransform.IsChildOf(transform);

        if (!standingOnThisPlatform)
            return;


        // =================================================
        // CALCULATE LAUNCH VELOCITY
        // =================================================

        // Use horizontal movement direction.
        Vector3 horizontalDirection =
            Vector3.ProjectOnPlane(
                moveDirection,
                Vector3.up
            ).normalized;

        Vector3 launchVelocity =
            horizontalDirection *
            horizontalLaunchSpeed +
            Vector3.up *
            upwardLaunchSpeed;


        // =================================================
        // LAUNCH PLAYER
        // =================================================

        player.Launch(launchVelocity);

        Debug.Log(
            $"TimedLaunchPlatform launched player: " +
            $"{launchVelocity}"
        );
    }


    // =====================================================
    // SCENE VISUALIZATION
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        if (moveDirection.sqrMagnitude < 0.0001f)
            return;

        Gizmos.color = Color.yellow;

        Vector3 origin = Application.isPlaying
            ? startingPosition
            : transform.position;

        Gizmos.DrawRay(
            origin,
            moveDirection.normalized * 3f
        );
    }
}
