
using UnityEngine;

/// <summary>
/// Moves a platform back and forth at a constant speed.
/// Useful for testing moving-platform riding and momentum inheritance.
/// </summary>
[DefaultExecutionOrder(-100)]
public class FastMovingPlatform : MonoBehaviour
{
    // =====================================================
    // MOVEMENT SETTINGS
    // =====================================================

    [Header("Movement")]

    [SerializeField]
    [Tooltip("World-space movement direction.")]
    private Vector3 direction = Vector3.right;

    [SerializeField, Min(0f)]
    [Tooltip("Distance between the two endpoints.")]
    private float travelDistance = 10f;

    [SerializeField, Min(0f)]
    [Tooltip("Movement speed in units per second.")]
    private float speed = 15f;


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    private Vector3 startingPosition;
    private float elapsedTime;


    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void Start()
    {
        startingPosition = transform.position;
    }

    private void Update()
    {
        if (travelDistance <= 0f ||
            speed <= 0f ||
            direction.sqrMagnitude < 0.0001f)
            return;

        elapsedTime += Time.deltaTime;

        // Move between 0 and travelDistance repeatedly.
        float distance = Mathf.PingPong(
            elapsedTime * speed,
            travelDistance
        );

        transform.position =
            startingPosition + direction.normalized * distance;
    }


    // =====================================================
    // SCENE VISUALIZATION
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Vector3 start = Application.isPlaying
            ? startingPosition
            : transform.position;

        Vector3 end =
            start + direction.normalized * travelDistance;

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.2f);
        Gizmos.DrawWireSphere(end, 0.2f);
    }
}
