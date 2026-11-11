using System.Collections;
using UnityEngine;

/// <summary>
/// Toggles collision and appearance when a specified target is shot.
/// Each instance can react after a delay and optionally revert after
/// its own independent timer.
/// </summary>
public class ReactToTarget : MonoBehaviour
{
    // =====================================================
    // TARGET SETTINGS
    // =====================================================

    [Header("Target Settings")]
    [SerializeField, Tooltip("The target this object listens to.")]
    private Target activationTarget;

    // =====================================================
    // OBJECT SETTINGS
    // =====================================================

    [Header("Object Settings")]
    [SerializeField, Tooltip("Collider to enable or disable.")]
    private Collider objectCollider;

    [SerializeField, Tooltip("Renderer whose appearance changes.")]
    private Renderer objectRenderer;

    // =====================================================
    // COLOR AND OPACITY SETTINGS
    // =====================================================

    [Header("Color Settings")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.red;

    [Header("Opacity Settings")]
    [SerializeField, Range(0f, 1f)]
    private float activeOpacity = 1f;

    [SerializeField, Range(0f, 1f)]
    private float inactiveOpacity = 0.25f;

    // =====================================================
    // TIMER SETTINGS
    // =====================================================

    [Header("Timer Settings")]
    [SerializeField, Min(0f), Tooltip("Seconds to wait after a shot before toggling.")]
    private float reactionDelay = 0f;

    [SerializeField, Tooltip("Automatically restore the previous state after a delay.")]
    private bool automaticallyRevert = true;

    [SerializeField, Min(0f), Tooltip("Seconds to stay toggled before reverting.")]
    private float revertDelay = 5f;

    // =====================================================
    // TIMER STATE (READ-ONLY TO OTHER SCRIPTS)
    // =====================================================

    private Coroutine activationCoroutine;
    private float revertEndTime = -1f;

    /// <summary>Whether this platform is waiting to revert.</summary>
    public bool IsRevertCountdownActive =>
        revertEndTime >= 0f && Time.time < revertEndTime;

    /// <summary>
    /// Fraction of the revert timer remaining, from 1 (full) to 0 (empty).
    /// </summary>
    public float RevertRemainingNormalized =>
        IsRevertCountdownActive && revertDelay > 0f
            ? Mathf.Clamp01((revertEndTime - Time.time) / revertDelay)
            : 0f;

    /// <summary>Seconds remaining until the platform reverts.</summary>
    public float RevertSecondsRemaining =>
        IsRevertCountdownActive ? revertEndTime - Time.time : 0f;

    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void Awake()
    {
        if (objectCollider == null)
            objectCollider = GetComponent<Collider>();

        if (objectRenderer == null)
            objectRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        if (activationTarget != null)
            activationTarget.OnActivated += Toggle;
    }

    private void OnDisable()
    {
        if (activationTarget != null)
            activationTarget.OnActivated -= Toggle;

        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        revertEndTime = -1f;
    }

    private void Start()
    {
        UpdateAppearance();
    }

    // =====================================================
    // ACTIVATION LOGIC
    // =====================================================

    private void Toggle()
    {
        if (objectCollider == null)
            return;

        // A new hit cancels the previous pending reaction/reversion.
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }

        revertEndTime = -1f;
        activationCoroutine = StartCoroutine(ToggleWithTimer());
    }

    private IEnumerator ToggleWithTimer()
    {
        if (reactionDelay > 0f)
            yield return new WaitForSeconds(reactionDelay);

        bool previousState = objectCollider.enabled;
        SetActivation(!previousState);

        if (automaticallyRevert)
        {
            if (revertDelay > 0f)
            {
                // The countdown starts when collision actually toggles,
                // not when the target is first shot.
                revertEndTime = Time.time + revertDelay;
                yield return new WaitForSeconds(revertDelay);
            }

            revertEndTime = -1f;
            SetActivation(previousState);
        }

        activationCoroutine = null;
    }

    private void SetActivation(bool active)
    {
        if (objectCollider == null)
            return;

        objectCollider.enabled = active;
        UpdateAppearance();
        Debug.Log($"{gameObject.name} collision enabled: {active}");
    }

    // =====================================================
    // VISUAL LOGIC
    // =====================================================

    private void UpdateAppearance()
    {
        if (objectRenderer == null || objectCollider == null)
            return;

        bool isActive = objectCollider.enabled;
        Color color = isActive ? activeColor : inactiveColor;
        color.a = isActive ? activeOpacity : inactiveOpacity;
        objectRenderer.material.color = color;
    }
}
