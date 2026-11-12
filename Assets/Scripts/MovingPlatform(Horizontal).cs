
using UnityEngine;

public class MovingPlatformHorizontal : MonoBehaviour
{
    public float speed = 3f;
    public float distance = 7f;

    private Vector3 startPosition;
    private Vector3 endPosition;

    void Start()
    {
        startPosition = transform.position;

        // Move forward and backward along the Z axis
        endPosition = startPosition + new Vector3(0, 0, distance);
    }

    void Update()
    {
        float time = Mathf.PingPong(Time.time * speed, 1f);

        time = Mathf.SmoothStep(0f, 1f, time);

        transform.position = Vector3.Lerp(
            startPosition,
            endPosition,
            time
        );
    }
}
