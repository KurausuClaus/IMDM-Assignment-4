
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws translucent beams between a target and the
/// platforms it controls.
///
/// Beams:
/// - Originate from the target's visible center.
/// - Connect to the top of each affected platform.
/// - Follow platforms if they move.
/// - Brighten and expand when the target is shot.
///
/// Attach this script to the same GameObject as Target.
/// </summary>
public class TargetConnectionFeedback : MonoBehaviour
{
    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("References")]

    [SerializeField]
    [Tooltip("The target responsible for activating the beams.")]
    private Target activationTarget;

    [SerializeField]
    [Tooltip("Platforms controlled by this target.")]
    private ReactToTarget[] affectedPlatforms;

    [SerializeField]
    [Tooltip("Transparent material used to render the beams.")]
    private Material beamMaterial;


    // =====================================================
    // BEAM POSITION
    // =====================================================

    [Header("Beam Position")]

    [SerializeField]
    [Tooltip("Additional offset from the target's visible center.")]
    private Vector3 targetOffset = Vector3.zero;

    [SerializeField]
    [Tooltip("Additional offset from the platform's top center.")]
    private Vector3 platformOffset = new Vector3(0f, 0.1f, 0f);


    // =====================================================
    // BEAM APPEARANCE
    // =====================================================

    [Header("Beam Appearance")]

    [SerializeField]
    private Color beamColor = new Color(0.3f, 0.9f, 1f);

    [SerializeField, Min(0.001f)]
    [Tooltip("Beam width when the target is idle.")]
    private float idleWidth = 0.045f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Beam opacity when idle.")]
    private float idleOpacity = 0.35f;

    [SerializeField, Min(0.001f)]
    [Tooltip("Maximum beam width during activation.")]
    private float pulseWidth = 0.12f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Maximum beam opacity during activation.")]
    private float pulseOpacity = 1f;

    [SerializeField, Min(0.01f)]
    [Tooltip("Duration of the activation pulse in seconds.")]
    private float pulseDuration = 0.7f;


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    private LineRenderer[] beams;

    private MeshRenderer targetRenderer;

    private MeshRenderer[] platformRenderers;

    private float pulseTimeRemaining;


    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void Awake()
    {
        // Retrieve the target automatically if necessary.
        if (activationTarget == null)
            activationTarget = GetComponent<Target>();

        // Retrieve the visible target mesh.
        targetRenderer = GetComponentInChildren<MeshRenderer>();

        if (beamMaterial == null)
        {
            Debug.LogError(
                "TargetConnectionFeedback requires a transparent Beam Material.",
                this
            );

            enabled = false;
            return;
        }

        if (affectedPlatforms == null)
            affectedPlatforms = new ReactToTarget[0];

        beams = new LineRenderer[affectedPlatforms.Length];

        platformRenderers =
            new MeshRenderer[affectedPlatforms.Length];

        // Generate one beam for each affected platform.
        for (int i = 0; i < affectedPlatforms.Length; i++)
        {
            ReactToTarget platform = affectedPlatforms[i];

            if (platform == null)
                continue;

            // Cache the platform's visible mesh renderer.
            platformRenderers[i] =
                platform.GetComponentInChildren<MeshRenderer>();

            GameObject child = new GameObject(
                "Beam to " + platform.name
            );

            child.transform.SetParent(transform, false);

            LineRenderer line =
                child.AddComponent<LineRenderer>();

            line.useWorldSpace = true;

            line.positionCount = 2;

            line.alignment = LineAlignment.View;

            line.numCapVertices = 4;

            line.shadowCastingMode = ShadowCastingMode.Off;

            line.receiveShadows = false;

            line.sharedMaterial = beamMaterial;

            line.startWidth = idleWidth;
            line.endWidth = idleWidth;

            beams[i] = line;
        }
    }


    private void OnEnable()
    {
        if (activationTarget != null)
            activationTarget.OnActivated += Pulse;
    }


    private void OnDisable()
    {
        if (activationTarget != null)
            activationTarget.OnActivated -= Pulse;
    }


    // =====================================================
    // PULSE LOGIC
    // =====================================================

    /// <summary>
    /// Called whenever the associated target is shot.
    ///
    /// Restarts the pulse animation, temporarily increasing
    /// beam width and opacity.
    /// </summary>
    private void Pulse()
    {
        pulseTimeRemaining = pulseDuration;
    }


    // =====================================================
    // BEAM POSITIONING
    // =====================================================

    /// <summary>
    /// Finds the visual center of the target.
    ///
    /// Uses the target mesh's world-space bounds rather
    /// than the GameObject's pivot.
    /// </summary>
    private Vector3 GetTargetPosition()
    {
        Vector3 center = targetRenderer != null
            ? targetRenderer.bounds.center
            : transform.position;

        return center + targetOffset;
    }


    /// <summary>
    /// Finds the top center of a platform.
    ///
    /// This prevents beams from terminating in the
    /// middle or bottom of platform geometry.
    /// </summary>
    private Vector3 GetPlatformPosition(int index)
    {
        ReactToTarget platform = affectedPlatforms[index];

        MeshRenderer renderer = platformRenderers[index];

        Vector3 position;

        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;

            position = new Vector3(
                bounds.center.x,
                bounds.max.y,
                bounds.center.z
            );
        }
        else
        {
            position = platform.transform.position;
        }

        return position + platformOffset;
    }


    // =====================================================
    // BEAM UPDATE
    // =====================================================

    /// <summary>
    /// Updates beam positions, width, and transparency.
    ///
    /// Positions update continuously so that beams
    /// follow moving targets and platforms.
    /// </summary>
    private void LateUpdate()
    {
        if (beams == null)
            return;

        // Determine the current pulse strength.
        // 1 = fully activated.
        // 0 = idle.
        float strength = pulseDuration > 0f
            ? Mathf.Clamp01(
                pulseTimeRemaining / pulseDuration
            )
            : 0f;

        // Interpolate between idle and active widths.
        float width = Mathf.Lerp(
            idleWidth,
            pulseWidth,
            strength
        );

        // Interpolate between idle and active opacity.
        Color color = beamColor;

        color.a = Mathf.Lerp(
            idleOpacity,
            pulseOpacity,
            strength
        );

        // Calculate the target's visual center.
        Vector3 startPosition = GetTargetPosition();

        // Update every connection.
        for (int i = 0; i < beams.Length; i++)
        {
            LineRenderer line = beams[i];

            ReactToTarget platform = affectedPlatforms[i];

            if (line == null || platform == null)
                continue;

            // Calculate the top center of this platform.
            Vector3 endPosition = GetPlatformPosition(i);

            // Update beam endpoints.
            line.SetPosition(0, startPosition);

            line.SetPosition(1, endPosition);

            // Update beam thickness.
            line.startWidth = width;
            line.endWidth = width;

            // Update beam color and transparency.
            line.startColor = color;
            line.endColor = color;
        }

        // Gradually reduce the pulse strength.
        pulseTimeRemaining = Mathf.Max(
            0f,
            pulseTimeRemaining - Time.deltaTime
        );
    }
}
