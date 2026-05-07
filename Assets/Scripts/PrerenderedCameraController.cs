using UnityEngine;

/// <summary>
/// Pans a pre-rendered-background camera based on where the player is on screen.
/// Attach to your MAIN camera (the one imported from your FBX).
/// You need a SECONDARY camera at the same starting transform that does NOT pan;
/// it's used purely to read the player's "true" screen position before any pan offset.
/// </summary>
[RequireComponent(typeof(Camera))]
public class PrerenderedCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("A non-panning duplicate of this camera. Used to compute player screen pos.")]
    [SerializeField] private Camera secondaryCamera;
    [SerializeField] private Transform playerTransform;

    [Header("Camera Pan Range (local offsets from FBX position)")]
    [SerializeField] private Vector2 minCamOffset = new Vector2(-0.5f, -0.2f);
    [SerializeField] private Vector2 maxCamOffset = new Vector2( 0.5f,  0.2f);

    [Header("Player Screen Bounds (0..1, the dead zone edges)")]
    [SerializeField] private Vector2 minScreenPoint = new Vector2(0.3f, 0.3f);
    [SerializeField] private Vector2 maxScreenPoint = new Vector2(0.7f, 0.7f);

    [Header("Smoothing")]
    [SerializeField] private float panSmoothTime = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool isDebug = false;
    [SerializeField] private Vector2 debugCamOffset;
    [SerializeField] private Vector2 debugPlayerScreenPos;

    private Vector3 originPosition;
    private Vector3 currentVelocity;

    void Awake()
    {
        // The FBX-exported transform is the "home" position the camera pans around.
        originPosition = transform.position;

        if (secondaryCamera != null)
        {
            // Make sure secondary camera doesn't actually render anything.
            secondaryCamera.enabled = false;
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null || secondaryCamera == null) return;

        // 1. Find the player's position on screen using the NON-panning camera.
        Vector3 screenPoint = secondaryCamera.WorldToScreenPoint(playerTransform.position);
        float playerScreenX = screenPoint.x / Screen.width;
        float playerScreenY = screenPoint.y / Screen.height;

        debugPlayerScreenPos = new Vector2(playerScreenX, playerScreenY);

        // 2. Map the player's screen position to a target camera offset.
        // When the player is at the left edge of the dead zone -> offset is at min.
        // When at the right edge -> offset is at max. In between: lerp.
        Vector2 targetOffset;
        if (isDebug)
        {
            targetOffset = debugCamOffset;
        }
        else
        {
            float tx = Mathf.InverseLerp(minScreenPoint.x, maxScreenPoint.x, playerScreenX);
            float ty = Mathf.InverseLerp(minScreenPoint.y, maxScreenPoint.y, playerScreenY);

            targetOffset = new Vector2(
                Mathf.Lerp(minCamOffset.x, maxCamOffset.x, tx),
                Mathf.Lerp(minCamOffset.y, maxCamOffset.y, ty)
            );
        }

        // 3. Apply the offset relative to the camera's local axes (right/up of the FBX cam).
        Vector3 desiredWorldPos = originPosition
            + transform.right * targetOffset.x
            + transform.up    * targetOffset.y;

        // 4. Smoothly move toward the target.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredWorldPos,
            ref currentVelocity,
            panSmoothTime
        );
    }
}
