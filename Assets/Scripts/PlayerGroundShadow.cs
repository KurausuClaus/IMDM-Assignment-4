
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Positions a single URP Decal Projector on the surface beneath the player.
/// Uses one downward raycast per frame, with no procedural mesh or UI rebuilding.
/// A decal projects straight down, regardless of scene lighting direction.
///
/// Setup: attach to the Player (with CharacterController), assign a Decal
/// Projector on a separate GameObject, and give the projector a decal material.
/// Requires the Universal Render Pipeline.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerGroundShadow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DecalProjector shadowProjector;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0.1f)] private float maxGroundDistance = 30f;

    [Header("Shadow")]
    [SerializeField, Min(0.05f)] private float diameter = 1.2f;
    [SerializeField, Min(0.1f)] private float projectionDepth = 1f;
    [SerializeField, Range(0f, 1f)] private float nearOpacity = 0.65f;
    [SerializeField, Range(0f, 1f)] private float farOpacity = 0.15f;
    [SerializeField, Min(0.01f)] private float fadeDistance = 8f;
    [SerializeField, Min(0f)] private float projectionHeightOffset = 0.1f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (shadowProjector == null)
        {
            Debug.LogError("Assign a URP Decal Projector to PlayerGroundShadow.", this);
            enabled = false;
            return;
        }

        // The Decal Projector's local +Z projection axis points downward.
        shadowProjector.transform.rotation =
            Quaternion.LookRotation(Vector3.down, Vector3.forward);
        shadowProjector.size = new Vector3(diameter, diameter, projectionDepth);
        shadowProjector.pivot = Vector3.zero;
        shadowProjector.enabled = false;
    }

    private void LateUpdate()
    {
        if (shadowProjector == null)
            return;

        Bounds bounds = controller.bounds;
        Vector3 origin = bounds.center;
        float castLength = maxGroundDistance + bounds.extents.y;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                castLength, groundLayers, QueryTriggerInteraction.Ignore))
        {
            shadowProjector.enabled = false;
            return;
        }

        // Keep the projector's volume centered near the detected surface.
        // Projection depth allows the decal to wrap onto curved geometry.
        shadowProjector.transform.position =
            hit.point + Vector3.up * projectionHeightOffset;

        float heightAboveSurface = Mathf.Max(
            0f, bounds.min.y - hit.point.y);
        float fade = Mathf.Clamp01(heightAboveSurface / fadeDistance);
        shadowProjector.fadeFactor = Mathf.Lerp(nearOpacity, farOpacity, fade);

        shadowProjector.enabled = true;
    }

    private void OnDisable()
    {
        if (shadowProjector != null)
            shadowProjector.enabled = false;
    }
}
