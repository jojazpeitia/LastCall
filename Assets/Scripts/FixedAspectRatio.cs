using UnityEngine;

/// <summary>
/// Forces a fixed visible aspect ratio. Two modes:
///
///   Match (default):
///     The camera's rendered aspect equals the visible aspect. Letterboxes
///     or pillarboxes the screen so the viewport is exactly targetAspect.
///     Use this when your render aspect == your gameplay aspect.
///
///   CropFromWider:
///     The camera renders at renderAspect (e.g. 2.2222 for 2400x1080) but
///     only the center targetAspect portion is shown (e.g. 1.7778 for 16:9).
///     Used for "render wide, show narrow" pre-rendered background games
///     where the camera pans within the wider rendered area.
///
/// Attach to the Main Camera. Set the camera's Physical Camera sensor to
/// match the WIDER renderAspect (e.g. sensor 36 x 16.2 for 2400x1080).
/// </summary>
[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class FixedAspectRatio : MonoBehaviour
{
    public enum Mode { Match, CropFromWider }

    [SerializeField] private Mode mode = Mode.Match;

    [Tooltip("The aspect ratio the player sees (e.g. 1.7778 for 16:9).")]
    [SerializeField] private float targetAspect = 16f / 9f;

    [Tooltip("The aspect the camera actually renders. Only used in CropFromWider mode. "
           + "Should match your pre-rendered image's aspect (e.g. 2.2222 for 2400x1080).")]
    [SerializeField] private float renderAspect = 2400f / 1080f;

    private Camera cam;

    void OnEnable() { cam = GetComponent<Camera>(); Apply(); }
    void Update()   { Apply(); }

    void Apply()
    {
        if (cam == null) return;

        float windowAspect = (float)Screen.width / (float)Screen.height;
        Rect rect;

        if (mode == Mode.Match)
        {
            // Letterbox/pillarbox to make the viewport exactly targetAspect.
            float scaleHeight = windowAspect / targetAspect;
            rect = new Rect(0f, 0f, 1f, 1f);
            if (scaleHeight < 1f)
            {
                rect.width  = 1f;
                rect.height = scaleHeight;
                rect.x = 0f;
                rect.y = (1f - scaleHeight) / 2f;
            }
            else
            {
                float scaleWidth = 1f / scaleHeight;
                rect.width  = scaleWidth;
                rect.height = 1f;
                rect.x = (1f - scaleWidth) / 2f;
                rect.y = 0f;
            }
        }
        else // CropFromWider
        {
            // The camera renders renderAspect but the visible viewport should
            // show only the center targetAspect slice.
            //
            // The camera's frustum is sized for renderAspect. We want to crop
            // horizontal sides off so only the center targetAspect remains.
            //
            // visibleWidth (in normalized viewport units relative to camera frustum)
            //   = targetAspect / renderAspect
            // The remaining horizontal area = sides cropped equally.
            //
            // Then we further fit that cropped result to the screen window
            // with letterboxing/pillarboxing.

            float cropWidth = targetAspect / renderAspect; // < 1
            float cropX = (1f - cropWidth) / 2f;

            // After cropping, the visible content has aspect == targetAspect.
            // Now letterbox/pillarbox to fit the screen window.
            float scaleHeight = windowAspect / targetAspect;

            if (scaleHeight < 1f)
            {
                // window is taller than target -> pillar nothing horizontally,
                // letterbox top/bottom inside the cropped strip.
                rect = new Rect(cropX, (1f - scaleHeight) / 2f, cropWidth, scaleHeight);
            }
            else
            {
                // window is wider than target -> need to pillarbox.
                float scaleWidth = 1f / scaleHeight;
                float finalCropWidth = cropWidth * scaleWidth;
                rect = new Rect(
                    cropX + (cropWidth - finalCropWidth) / 2f,
                    0f,
                    finalCropWidth,
                    1f
                );
            }
        }

        cam.rect = rect;
    }
}