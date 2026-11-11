
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles level completion when the player touches a goal.
///
/// When the player enters the goal's trigger:
/// 1. Verify that the object is the player.
/// 2. Prevent repeated activation.
/// 3. Load the specified next level.
///
/// The destination scene is configurable in the Inspector.
///
/// Requirements:
/// - A Collider with Is Trigger enabled.
/// - A valid destination scene in the Build Profiles scene list.
/// - A player with a PlayerMovement component.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LevelGoal : MonoBehaviour
{
    // =====================================================
    // LEVEL SETTINGS
    // =====================================================

    [Header("Level Settings")]

    [SerializeField]
    [Tooltip("Name of the scene to load when the goal is reached.")]
    private string nextSceneName;


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    // Prevents multiple scene transitions.
    private bool isLoading = false;


    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    /// <summary>
    /// Detects when an object enters the goal's trigger.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Ignore collisions while transitioning.
        if (isLoading)
            return;

        // Verify that the object is the player.
        // Searching parents also supports child colliders.
        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        CompleteLevel();
    }


    // =====================================================
    // LEVEL COMPLETION
    // =====================================================

    /// <summary>
    /// Completes the current level and loads the
    /// specified destination scene.
    /// </summary>
    private void CompleteLevel()
    {
        // Validate the destination scene name.
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogError(
                "LevelGoal: No destination scene assigned!",
                this
            );

            return;
        }

        // Verify that the scene is available.
        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError(
                $"LevelGoal: Scene '{nextSceneName}' could not be found. " +
                "Make sure it is included in the Build Profiles scene list.",
                this
            );

            return;
        }

        // Prevent multiple activations.
        isLoading = true;

        Debug.Log(
            $"Level completed! Loading: {nextSceneName}"
        );

        // Load the next level, replacing the current scene.
        SceneManager.LoadScene(
            nextSceneName,
            LoadSceneMode.Single
        );
    }
}
