using UnityEngine;

/// <summary>
/// Pans the camera's view by shifting its projection matrix off-center.
/// Camera position never changes, so the pre-rendered background and the
/// invisible 3D mesh stay perfectly aligned.
///
/// Sets the matrix in OnPreCull so it runs after Post-Processing v2 has
/// done its own projection setup, preventing the matrix from being reset.
/// </summary>
[RequireComponent(typeof(Camera))]
public class PrerenderedCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera secondaryCamera;
    [SerializeField] private Transform playerTransform;

    [Header("Projection Offset Range")]
    [SerializeField] private Vector2 minCamOffset = new Vector2(-0.05f, -0.02f);
    [SerializeField] private Vector2 maxCamOffset = new Vector2( 0.05f,  0.02f);

    [Header("Player Screen Bounds (0..1)")]
    [SerializeField] private Vector2 minScreenPoint = new Vector2(0.3f, 0.35f);
    [SerializeField] private Vector2 maxScreenPoint = new Vector2(0.7f, 0.65f);

    [Header("Smoothing")]
    [SerializeField] private float panSmoothTime = 0.4f;

    [Header("Debug")]
    [SerializeField] private bool isDebug = false;
    [SerializeField] private Vector2 debugCamOffset;
    [SerializeField] private Vector2 debugPlayerScreenPos;

    private Camera primaryCamera;
    private Vector2 currentOffset;
    private Vector2 currentVelocity;

    void Awake()
    {
        primaryCamera = GetComponent<Camera>();
        if (secondaryCamera != null)
            secondaryCamera.enabled = false;
    }

    // Compute the smoothed target offset in LateUpdate (after gameplay).
    void LateUpdate()
    {
        Vector2 targetOffset;

        if (isDebug)
        {
            targetOffset = debugCamOffset;
        }
        else
        {
            if (playerTransform == null || secondaryCamera == null) return;

            Vector3 screenPoint = secondaryCamera.WorldToScreenPoint(playerTransform.position);
            float playerScreenX = screenPoint.x / Screen.width;
            float playerScreenY = screenPoint.y / Screen.height;
            debugPlayerScreenPos = new Vector2(playerScreenX, playerScreenY);

            float tx = Mathf.InverseLerp(minScreenPoint.x, maxScreenPoint.x, playerScreenX);
            float ty = Mathf.InverseLerp(minScreenPoint.y, maxScreenPoint.y, playerScreenY);
            targetOffset = new Vector2(
                Mathf.Lerp(minCamOffset.x, maxCamOffset.x, tx),
                Mathf.Lerp(minCamOffset.y, maxCamOffset.y, ty)
            );
        }

        currentOffset = Vector2.SmoothDamp(currentOffset, targetOffset,
                                           ref currentVelocity, panSmoothTime);
    }

    // Apply the projection matrix in OnPreCull, which runs AFTER
    // Post-Processing v2 has set its own projection. Our matrix is the
    // last write, so it's what the renderer uses.
    void OnPreCull()
    {
        if (primaryCamera == null) primaryCamera = GetComponent<Camera>();
        SetVanishingPoint(primaryCamera, currentOffset);
    }

    private void SetVanishingPoint(Camera cam, Vector2 perspectiveOffset)
    {
        cam.ResetProjectionMatrix();
        Matrix4x4 m = cam.projectionMatrix;

        float w = 2f * cam.nearClipPlane / m.m00;
        float h = 2f * cam.nearClipPlane / m.m11;

        float left   = -w / 2f - perspectiveOffset.x;
        float right  = left + w;
        float bottom = -h / 2f - perspectiveOffset.y;
        float top    = bottom + h;

        cam.projectionMatrix = PerspectiveOffCenter(left, right, bottom, top,
                                                    cam.nearClipPlane,
                                                    cam.farClipPlane);
    }

    private static Matrix4x4 PerspectiveOffCenter(
        float left, float right, float bottom, float top,
        float near, float far)
    {
        float x = (2f * near) / (right - left);
        float y = (2f * near) / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2f * far * near) / (far - near);

        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;    m[0, 2] = a;
        m[1, 1] = y;    m[1, 2] = b;
        m[2, 2] = c;    m[2, 3] = d;
        m[3, 2] = -1f;
        return m;
    }

    void OnDisable()
    {
        if (primaryCamera != null) primaryCamera.ResetProjectionMatrix();
    }
}