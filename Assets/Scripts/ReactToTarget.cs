
using UnityEngine;

/// <summary>
/// Reacts to a specified target being shot.
///
/// When the target is activated:
/// 1. Toggle the object's collision.
/// 2. Update its color and transparency.
///
/// The GameObject remains active at all times.
/// </summary>
public class ReactToTarget : MonoBehaviour
{
    // =====================================================
    // TARGET SETTINGS
    // =====================================================

    [Header("Target Settings")]

    [SerializeField]
    [Tooltip("The target this object listens to.")]
    private Target activationTarget;


    // =====================================================
    // OBJECT SETTINGS
    // =====================================================

    [Header("Object Settings")]

    [SerializeField]
    [Tooltip("Collider to enable or disable.")]
    private Collider objectCollider;

    [SerializeField]
    [Tooltip("Renderer whose appearance changes.")]
    private Renderer objectRenderer;


    // =====================================================
    // COLOR SETTINGS
    // =====================================================

    [Header("Color Settings")]

    [SerializeField]
    private Color activeColor = Color.green;

    [SerializeField]
    private Color inactiveColor = Color.red;


    // =====================================================
    // OPACITY SETTINGS
    // =====================================================

    [Header("Opacity Settings")]

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Opacity when collision is enabled.")]
    private float activeOpacity = 1f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Opacity when collision is disabled.")]
    private float inactiveOpacity = 0.25f;


    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void OnEnable()
    {
        if (activationTarget != null)
            activationTarget.OnActivated += Toggle;
    }

    private void OnDisable()
    {
        if (activationTarget != null)
            activationTarget.OnActivated -= Toggle;
    }

    private void Start()
    {
        // Automatically retrieve components if not assigned.
        if (objectCollider == null)
            objectCollider = GetComponent<Collider>();

        if (objectRenderer == null)
            objectRenderer = GetComponent<Renderer>();

        UpdateAppearance();
    }


    // =====================================================
    // ACTIVATION LOGIC
    // =====================================================

    /// <summary>
    /// Toggle collision when the assigned target is shot.
    /// </summary>
    private void Toggle()
    {
        if (objectCollider == null)
            return;

        objectCollider.enabled = !objectCollider.enabled;

        UpdateAppearance();

        Debug.Log(
            $"{gameObject.name} collision enabled: {objectCollider.enabled}"
        );
    }


    // =====================================================
    // VISUAL LOGIC
    // =====================================================

    /// <summary>
    /// Update color and opacity based on collision state.
    /// </summary>
    private void UpdateAppearance()
    {
        if (objectRenderer == null || objectCollider == null)
            return;

        bool isActive = objectCollider.enabled;

        Color newColor = isActive
            ? activeColor
            : inactiveColor;

        newColor.a = isActive
            ? activeOpacity
            : inactiveOpacity;

        objectRenderer.material.color = newColor;
    }
}
