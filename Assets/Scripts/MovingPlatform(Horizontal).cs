
using UnityEngine;

/// <summary>
/// Makes a platform move forward and backward along the Z axis.
/// Shouldn't matter where on the Y axis you put the platform.
/// </summary>

public class MovingPlatform : MonoBehaviour
{
    public float speed = 3f;
    public float distance = 7f;

    private Vector3 startPosition;
    private Vector3 endPosition;

    void Start()
    {
        startPosition = transform.position;
        endPosition = startPosition + new Vector3(0, 0, distance);
    }

    void Update()
    {
        float time = Mathf.PingPong(Time.time * speed, 1f);

        // Smooth out the movement
        time = Mathf.SmoothStep(0f, 1f, time);

        transform.position = Vector3.Lerp(
            startPosition,
            endPosition,
            time
        );
    }
}
