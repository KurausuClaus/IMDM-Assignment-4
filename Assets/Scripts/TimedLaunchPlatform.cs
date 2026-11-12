
using UnityEngine;

/// <summary>
/// A platform that begins moving rapidly when its activation
/// countdown reaches a specified amount of time remaining.
///
/// The platform itself does not modify player velocity.
///
/// PlayerMovement handles inheriting platform momentum
/// when the player jumps or walks off.
///
/// When the activation ends, the platform returns to
/// its original position.
/// </summary>
[RequireComponent(typeof(ReactToTarget))]
[DefaultExecutionOrder(-100)]
public class TimedLaunchPlatform : MonoBehaviour
{
    // =====================================================
    // LAUNCH TIMING
    // =====================================================

    [Header("Launch Timing")]

    [SerializeField, Min(0f)]
    [Tooltip("Begin moving when this many seconds remain.")]
    private float secondsRemainingToLaunch = 2f;


    // =====================================================
    // PLATFORM MOVEMENT
    // =====================================================

    [Header("Platform Movement")]

    [SerializeField]
    [Tooltip("World-space direction of platform movement.")]
    private Vector3 moveDirection = Vector3.right;

    [SerializeField, Min(0f)]
    [Tooltip("Movement speed in units per second.")]
    private float platformSpeed = 30f;


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    private ReactToTarget reaction;

    private Vector3 startingPosition;

    private bool timerWasRunning;

    private bool isMoving;


    // =====================================================
    // INITIALIZATION
    // =====================================================

    private void Awake()
    {
        reaction = GetComponent<ReactToTarget>();

        startingPosition = transform.position;
    }


    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        bool timerRunning =
            reaction.IsRevertCountdownActive;

        // ---------------------------------------------
        // TIMER FINISHED
        // ---------------------------------------------

        if (!timerRunning)
        {
            if (timerWasRunning)
            {
                ResetPlatform();
            }

            timerWasRunning = false;
            return;
        }

        // ---------------------------------------------
        // NEW ACTIVATION
        // ---------------------------------------------

        if (!timerWasRunning)
        {
            ResetPlatform();
        }

        timerWasRunning = true;

        // ---------------------------------------------
        // CHECK REMAINING TIME
        // ---------------------------------------------

        float secondsRemaining =
            reaction.RevertSecondsRemaining;

        if (!isMoving &&
            secondsRemaining <= secondsRemainingToLaunch)
        {
            isMoving = true;
        }

        // ---------------------------------------------
        // MOVE PLATFORM
        // ---------------------------------------------

        if (isMoving &&
            moveDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 direction =
                moveDirection.normalized;

            transform.position +=
                direction * platformSpeed * Time.deltaTime;
        }
    }


    // =====================================================
    // RESET PLATFORM
    // =====================================================

    private void ResetPlatform()
    {
        isMoving = false;

        transform.position = startingPosition;
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
