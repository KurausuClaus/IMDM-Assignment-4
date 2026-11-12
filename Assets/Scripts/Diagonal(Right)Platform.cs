
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public float speed = 3f;
    public float distance = 7f;

    private Vector3 startPosition;
    private Vector3 endPosition;

    void Start()
    {
        startPosition = transform.position;

        // Diagonal movement: right and forward
        endPosition = startPosition + new Vector3(distance, 0, distance);
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
