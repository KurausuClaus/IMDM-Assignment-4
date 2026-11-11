
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a world-space countdown bar above a platform.
///
/// Features:
/// - Shrinks as the platform's timer decreases.
/// - Changes color from green to red.
/// - Maintains a visible background.
/// - Keeps the foreground aligned with the background.
/// - Supports configurable width and height.
/// - Automatically faces the player's camera.
/// - Follows the platform's position.
/// - Disappears when no countdown is active.
///
/// Uses the actual timer from ReactToTarget.
/// Does not maintain a separate countdown.
/// </summary>
public class PlatformTimerBar : MonoBehaviour
{
    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("References")]

    [SerializeField]
    [Tooltip("Platform whose timer should be displayed.")]
    private ReactToTarget platform;

    [SerializeField]
    [Tooltip("Camera the countdown should face.")]
    private Camera playerCamera;


    // =====================================================
    // BAR PLACEMENT
    // =====================================================

    [Header("Placement")]

    [SerializeField]
    [Tooltip("Offset from the top center of the platform.")]
    private Vector3 offset = new Vector3(0f, 0.3f, 0f);

    [SerializeField, Min(0.01f)]
    [Tooltip("Width of the bar in world units.")]
    private float barLength = 1.2f;

    [SerializeField, Min(0.01f)]
    [Tooltip("Height of the bar in world units.")]
    private float barHeight = 0.12f;


    // =====================================================
    // COLOR SETTINGS
    // =====================================================

    [Header("Colors")]

    [SerializeField]
    [Tooltip("Color of the empty background.")]
    private Color backgroundColor =
        new Color(0.05f, 0.07f, 0.1f, 0.8f);

    [SerializeField]
    [Tooltip("Color when the timer is full.")]
    private Color fullColor =
        new Color(0.1f, 1f, 0.4f, 1f);

    [SerializeField]
    [Tooltip("Color when the timer is empty.")]
    private Color emptyColor =
        new Color(1f, 0.15f, 0.1f, 1f);


    // =====================================================
    // INTERNAL STATE
    // =====================================================

    private GameObject canvasObject;

    private RectTransform canvasRect;

    private RectTransform fillRect;

    private RawImage backgroundImage;

    private RawImage fillImage;

    private MeshRenderer platformRenderer;

    // Reference width used for UI scaling.
    private const float UIWidth = 160f;

    // Padding between the foreground and background edges.
    private const float Padding = 2f;


    // =====================================================
    // UNITY LIFECYCLE
    // =====================================================

    private void Awake()
    {
        // Automatically retrieve the platform component.
        if (platform == null)
            platform = GetComponent<ReactToTarget>();

        // Automatically retrieve the main camera.
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Validate the platform reference.
        if (platform == null)
        {
            Debug.LogError(
                "PlatformTimerBar requires ReactToTarget.",
                this
            );

            enabled = false;
            return;
        }

        // Retrieve the platform's visible mesh.
        platformRenderer =
            platform.GetComponentInChildren<MeshRenderer>();

        // Generate the countdown UI.
        CreateTimerUI();
    }


    private void OnDisable()
    {
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }


    private void OnDestroy()
    {
        // Clean up the generated UI.
        if (canvasObject != null)
            Destroy(canvasObject);
    }


    // =====================================================
    // UI GENERATION
    // =====================================================

    /// <summary>
    /// Creates a world-space Canvas containing:
    ///
    /// 1. A full-size background.
    /// 2. A colored foreground representing remaining time.
    ///
    /// The foreground is anchored to the left edge so
    /// that it shrinks without shifting position.
    /// </summary>
    private void CreateTimerUI()
    {
        // Create the Canvas.
        canvasObject = new GameObject(
            "Platform Countdown UI",
            typeof(RectTransform),
            typeof(Canvas)
        );

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode = RenderMode.WorldSpace;

        canvas.overrideSorting = true;
        canvas.sortingOrder = 10;

        canvasRect =
            canvasObject.GetComponent<RectTransform>();

        // Calculate the correct initial dimensions.
        float canvasHeight =
            UIWidth * (barHeight / barLength);

        canvasRect.sizeDelta = new Vector2(
            UIWidth,
            canvasHeight
        );

        // Convert UI dimensions into world-space units.
        canvasRect.localScale =
            Vector3.one * (barLength / UIWidth);


        // =================================================
        // BACKGROUND
        // =================================================

        backgroundImage = CreateImage(
            "Timer Background"
        );

        RectTransform backgroundRect =
            backgroundImage.rectTransform;

        // Stretch the background across the entire Canvas.
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;

        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        backgroundImage.color = backgroundColor;


        // =================================================
        // FOREGROUND
        // =================================================

        fillImage = CreateImage(
            "Timer Fill"
        );

        fillRect = fillImage.rectTransform;

        // Anchor the foreground to the left edge.
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);

        // Keep the left edge stationary as width changes.
        fillRect.pivot = new Vector2(0f, 0.5f);

        // Position inside the background's left padding.
        //
        // The anchor already represents the left edge,
        // so we only need to add Padding.
        fillRect.anchoredPosition = new Vector2(
            Padding,
            0f
        );

        // Calculate the usable dimensions.
        float usableWidth =
            UIWidth - Padding * 2f;

        float usableHeight = Mathf.Max(
            0f,
            canvasHeight - Padding * 2f
        );

        fillRect.sizeDelta = new Vector2(
            usableWidth,
            usableHeight
        );

        fillImage.color = fullColor;

        // Initially hide the countdown.
        canvasObject.SetActive(false);
    }


    // =====================================================
    // IMAGE CREATION
    // =====================================================

    /// <summary>
    /// Creates a UI image under the countdown Canvas.
    ///
    /// Uses Unity's built-in white texture so its color
    /// and transparency are directly adjustable.
    /// </summary>
    private RawImage CreateImage(string objectName)
    {
        GameObject child = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(RawImage)
        );

        child.transform.SetParent(
            canvasRect,
            false
        );

        RawImage image =
            child.GetComponent<RawImage>();

        image.texture = Texture2D.whiteTexture;

        image.raycastTarget = false;

        return image;
    }


    // =====================================================
    // BAR POSITIONING
    // =====================================================

    /// <summary>
    /// Finds the top center of the platform's geometry.
    ///
    /// Uses the renderer's world-space bounds rather
    /// than the GameObject's pivot.
    /// </summary>
    private Vector3 GetBarPosition()
    {
        if (platformRenderer != null)
        {
            Bounds bounds = platformRenderer.bounds;

            return new Vector3(
                bounds.center.x,
                bounds.max.y,
                bounds.center.z
            ) + offset;
        }

        return platform.transform.position + offset;
    }


    // =====================================================
    // TIMER VISUALIZATION
    // =====================================================

    /// <summary>
    /// Updates the countdown bar every frame.
    ///
    /// The background maintains its full dimensions.
    ///
    /// The foreground:
    /// - Shrinks from right to left.
    /// - Maintains a constant height.
    /// - Remains aligned with the background.
    /// - Gradually changes from green to red.
    ///
    /// The Canvas disappears when the timer ends.
    /// </summary>
    private void LateUpdate()
    {
        if (canvasObject == null || platform == null)
            return;

        // Determine whether a countdown is active.
        bool isCountingDown =
            platform.IsRevertCountdownActive;

        // Show or hide the Canvas.
        if (canvasObject.activeSelf != isCountingDown)
        {
            canvasObject.SetActive(isCountingDown);
        }

        if (!isCountingDown)
            return;

        // Recover the camera reference if necessary.
        if (playerCamera == null)
            playerCamera = Camera.main;


        // =================================================
        // UPDATE POSITION AND ROTATION
        // =================================================

        // Position above the platform's visible geometry.
        canvasRect.position = GetBarPosition();

        // Rotate the Canvas to face the player camera.
        if (playerCamera != null)
        {
            canvasRect.rotation =
                playerCamera.transform.rotation;
        }


        // =================================================
        // UPDATE PROPORTIONAL DIMENSIONS
        // =================================================

        // Calculate the scale required to produce
        // the desired world-space width.
        float scale = barLength / UIWidth;

        canvasRect.localScale =
            Vector3.one * scale;

        // Convert the desired world-space height
        // into the equivalent Canvas height.
        float canvasHeight =
            barHeight / scale;

        // Update the Canvas dimensions.
        canvasRect.sizeDelta = new Vector2(
            UIWidth,
            canvasHeight
        );


        // =================================================
        // UPDATE REMAINING TIME
        // =================================================

        // 1 = full timer.
        // 0 = timer expired.
        float fraction = Mathf.Clamp01(
            platform.RevertRemainingNormalized
        );

        // Calculate usable dimensions after padding.
        float usableWidth =
            UIWidth - Padding * 2f;

        float usableHeight = Mathf.Max(
            0f,
            canvasHeight - Padding * 2f
        );

        // The width decreases with the timer.
        // The height remains unchanged.
        fillRect.sizeDelta = new Vector2(
            usableWidth * fraction,
            usableHeight
        );

        // Maintain the foreground's alignment
        // with the left edge of the background.
        fillRect.anchoredPosition = new Vector2(
            Padding,
            0f
        );


        // =================================================
        // UPDATE COLORS
        // =================================================

        // Green when full, red when nearly empty.
        fillImage.color = Color.Lerp(
            emptyColor,
            fullColor,
            fraction
        );

        // Maintain the background color.
        backgroundImage.color = backgroundColor;
    }
}
