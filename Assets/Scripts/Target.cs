
using System;
using UnityEngine;

/// <summary>
/// Represents an object that can be activated by shooting it.
///
/// When hit, the target broadcasts an event that other
/// objects can subscribe to.
/// </summary>
public class Target : MonoBehaviour
{
    // =====================================================
    // TARGET SETTINGS
    // =====================================================

    [Header("Target Settings")]

    [SerializeField]
    [Tooltip("Whether the target can be activated multiple times.")]
    private bool reusable = false;

    // Tracks whether the target has already been activated.
    private bool isActivated = false;


    // =====================================================
    // TARGET EVENTS
    // =====================================================

    // Other objects can subscribe to this event.
    public event Action OnActivated;


    // =====================================================
    // TARGET LOGIC
    // =====================================================

    /// <summary>
    /// Called by PlayerShooting when this target is shot.
    /// </summary>
    public void Hit()
    {
        // Only accept objects tagged as targets.
        if (!CompareTag("Target"))
            return;

        // Ignore subsequent shots if the target is not reusable.
        if (isActivated && !reusable)
            return;

        isActivated = true;

        Debug.Log($"{gameObject.name} was activated!");

        // Notify every object listening to this target.
        OnActivated?.Invoke();
    }
}
