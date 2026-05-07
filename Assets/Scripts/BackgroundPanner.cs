using UnityEngine;

/// <summary>
/// Creates a subtle "Ken Burns" pan illusion by shifting the background
/// texture's UVs on the quad based on where the player is in the camera's
/// view — WITHOUT moving the 3D camera (which would break alignment with
/// the InvisibleMat geometry).
///
/// Works in conjunction with the UnlitBackground shader, which exposes a
/// _UVOffset property and a _UVZoom property.
///
/// How it works:
///   - We zoom in slightly on the texture (e.g. 1.1x), giving us a margin
///     of pixels that exist outside the visible frame.
///   - As the player moves toward a screen edge, we shift the UVs in the
///     OPPOSITE direction so the player visually appears to drift toward
///     the center as the camera "pans".
///   - All movement is purely in texture space; the 3D camera, the
///     InvisibleMat mesh, and the player's actual world position never
///     change relative to each other.
///
/// Attach to your Quad (the one with BG_Material).
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class BackgroundPanner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera viewCamera;
    [SerializeField] private Transform playerTransform;

    [Header("Pan Range (in UV units, 0..1)")]
    [Tooltip("Maximum UV shift from center. Small values (0.01-0.05) feel subtle.")]
    [SerializeField] private Vector2 maxUVOffset = new Vector2(0.03f, 0.015f);

    [Header("Player Screen Bounds (where pan kicks in, 0..1)")]
    [SerializeField] private Vector2 minScreenPoint = new Vector2(0.3f, 0.3f);
    [SerializeField] private Vector2 maxScreenPoint = new Vector2(0.7f, 0.7f);

    [Header("Zoom (must match _UVZoom on material; 1.05-1.15 typical)")]
    [Tooltip("How much the texture is zoomed-in. Margin = (zoom-1)/2 in UV units. "
           + "Make sure maxUVOffset stays smaller than this margin or you'll see "
           + "the edge of the image.")]
    [SerializeField] private float uvZoom = 1.10f;

    [Header("Smoothing")]
    [SerializeField] private float panSmoothTime = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool isDebug = false;
    [SerializeField] private Vector2 debugUVOffset;

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock mpb;
    private Vector2 currentOffset;
    private Vector2 currentVelocity;

    static readonly int UVOffsetID = Shader.PropertyToID("_UVOffset");
    static readonly int UVZoomID   = Shader.PropertyToID("_UVZoom");

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        mpb = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        if (viewCamera == null || playerTransform == null) return;

        // 1. Find player's normalized screen position.
        Vector3 screenPoint = viewCamera.WorldToScreenPoint(playerTransform.position);
        float playerScreenX = screenPoint.x / Screen.width;
        float playerScreenY = screenPoint.y / Screen.height;

        // 2. Map to a UV offset.
        Vector2 targetOffset;
        if (isDebug)
        {
            targetOffset = debugUVOffset;
        }
        else
        {
            float tx = Mathf.InverseLerp(minScreenPoint.x, maxScreenPoint.x, playerScreenX);
            float ty = Mathf.InverseLerp(minScreenPoint.y, maxScreenPoint.y, playerScreenY);

            // Lerp from -max to +max. Note: SHIFT IN OPPOSITE direction from the player —
            // when player is on the right of screen, image shifts left so player appears
            // closer to center as the "camera pans right".
            // (You can flip the signs below if you want the inverse feel.)
            targetOffset = new Vector2(
                Mathf.Lerp(-maxUVOffset.x, maxUVOffset.x, tx),
                Mathf.Lerp(-maxUVOffset.y, maxUVOffset.y, ty)
            );
        }

        // 3. Smooth toward target.
        currentOffset = Vector2.SmoothDamp(currentOffset, targetOffset,
                                           ref currentVelocity, panSmoothTime);

        // 4. Push to the material via property block (fast, no instance copy).
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetVector(UVOffsetID, new Vector4(currentOffset.x, currentOffset.y, 0, 0));
        mpb.SetFloat(UVZoomID, uvZoom);
        meshRenderer.SetPropertyBlock(mpb);
    }
}
