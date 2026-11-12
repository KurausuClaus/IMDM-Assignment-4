
using UnityEngine;

/// <summary>
/// Makes a platform slowly move up and down.
/// </summary>

public class MovingPlatformVertical : MonoBehaviour
{
    public float speed = 0.3f;
    public float distance = 8f;

    private Vector3 startPosition;
    private Vector3 endPosition;

    void Start()
    {
        startPosition = transform.position;

        // Move upward along the Y axis
        endPosition = startPosition + new Vector3(0, distance, 0);
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
