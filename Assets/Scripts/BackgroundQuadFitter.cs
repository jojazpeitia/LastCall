using UnityEngine;

/// <summary>
/// Fits a quad to perfectly fill the camera's frustum at a given local Z distance.
/// Parent the quad to the camera, set distanceFromCamera, and assign your camera.
/// Recalculates in editor (ExecuteAlways) so you can see it as you tweak.
/// </summary>
[ExecuteAlways]
public class BackgroundQuadFitter : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float distanceFromCamera = 10f;

    [Header("Optional override (use the rendered image's pixel aspect)")]
    [SerializeField] private bool overrideAspect = false;
    [SerializeField] private float aspectOverride = 16f / 9f;

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // Keep quad parented to camera, locked to the same forward distance.
        Vector3 localPos = transform.localPosition;
        localPos.x = 0f;
        localPos.y = 0f;
        localPos.z = distanceFromCamera;
        transform.localPosition = localPos;
        transform.localRotation = Quaternion.identity;

        // Frustum height at distance d for a perspective camera with vertical FOV theta:
        //   h = 2 * d * tan(theta / 2)
        float vFovRad = targetCamera.fieldOfView * Mathf.Deg2Rad;
        float frustumHeight = 2f * distanceFromCamera * Mathf.Tan(vFovRad * 0.5f);

        float aspect = overrideAspect ? aspectOverride : targetCamera.aspect;
        float frustumWidth = frustumHeight * aspect;

        transform.localScale = new Vector3(frustumWidth, frustumHeight, 1f);
    }
}
