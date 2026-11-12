
using UnityEngine;

/// <summary>
/// Continuously spawns physics-enabled spheres at a
/// configurable position.
///
/// Spheres fall under gravity, bounce, roll, and pile up.
///
/// Designed for a victory celebration.
/// </summary>
public class VictorySphereRain : MonoBehaviour
{
    // =====================================================
    // SPAWN SETTINGS
    // =====================================================

    [Header("Spawn Settings")]

    [SerializeField]
    [Tooltip("Optional spawn location. Uses this object's position if empty.")]
    private Transform spawnPoint;

    [SerializeField, Min(0.01f)]
    [Tooltip("Time between sphere spawns, in seconds.")]
    private float spawnInterval = 0.1f;

    [SerializeField, Min(1)]
    [Tooltip("Maximum number of spheres spawned.")]
    private int maxSpheres = 200;

    [SerializeField]
    [Tooltip("Random position offset around the spawn point.")]
    private Vector3 spawnSpread = new Vector3(3f, 0f, 3f);


    // =====================================================
    // SPHERE SETTINGS
    // =====================================================

    [Header("Sphere Settings")]

    [SerializeField, Min(0.05f)]
    [Tooltip("Diameter of each sphere.")]
    private float sphereSize = 1f;

    [SerializeField, Min(0.01f)]
    [Tooltip("Mass of each sphere.")]
    private float sphereMass = 1f;

    [SerializeField]
    [Tooltip("Give each sphere a random color.")]
    private bool randomColors = true;


    // =====================================================
    // ACTIVATION
    // =====================================================

    [Header("Activation")]

    [SerializeField]
    [Tooltip("Begin spawning automatically when the scene starts.")]
    private bool startAutomatically = true;


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    private float spawnTimer;
    private int spheresSpawned;
    private bool isRaining;


    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void Start()
    {
        if (startAutomatically)
            StartRain();
    }

    private void Update()
    {
        if (!isRaining)
            return;

        spawnTimer -= Time.deltaTime;

        while (spawnTimer <= 0f &&
               spheresSpawned < maxSpheres)
        {
            SpawnSphere();
            spawnTimer += spawnInterval;
        }

        if (spheresSpawned >= maxSpheres)
            isRaining = false;
    }


    // =====================================================
    // PUBLIC CONTROLS
    // =====================================================

    /// <summary>
    /// Starts raining spheres.
    /// Can also be called by other scripts.
    /// </summary>
    public void StartRain()
    {
        spheresSpawned = 0;
        spawnTimer = 0f;
        isRaining = true;
    }

    /// <summary>
    /// Stops spawning new spheres.
    /// Existing spheres remain in the scene.
    /// </summary>
    public void StopRain()
    {
        isRaining = false;
    }


    // =====================================================
    // SPAWN SPHERE
    // =====================================================

    private void SpawnSphere()
    {
        Vector3 center = spawnPoint != null
            ? spawnPoint.position
            : transform.position;

        Vector3 offset = new Vector3(
            Random.Range(-spawnSpread.x, spawnSpread.x),
            Random.Range(-spawnSpread.y, spawnSpread.y),
            Random.Range(-spawnSpread.z, spawnSpread.z)
        );

        GameObject sphere = GameObject.CreatePrimitive(
            PrimitiveType.Sphere
        );

        sphere.name = $"Victory Sphere {spheresSpawned + 1}";

        sphere.transform.position = center + offset;

        sphere.transform.localScale =
            Vector3.one * sphereSize;

        // Give the sphere normal Unity physics.
        Rigidbody rb = sphere.AddComponent<Rigidbody>();

        rb.mass = sphereMass;
        rb.useGravity = true;
        rb.isKinematic = false;

        // Set a random bright color.
        if (randomColors)
        {
            Renderer renderer = sphere.GetComponent<Renderer>();

            renderer.material.color = Color.HSVToRGB(
                Random.value,
                0.85f,
                1f
            );
        }

        spheresSpawned++;
    }
}
