using UnityEngine;

/// <summary>
/// Sets all child renderers' material render queues to 2002, ensuring they
/// render AFTER the InvisibleMat depth pass (queue 1999/Geometry-1) so that
/// depth occlusion works correctly — the player visually disappears behind
/// invisible geometry that's closer to the camera.
///
/// Attach to your Player (or any object you want correctly occluded by the
/// invisible scene mesh).
/// </summary>
public class Obscurable : MonoBehaviour
{
    void Start()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Each material gets its renderQueue bumped so this renderer
            // draws after the depth-only InvisibleMat pass.
            foreach (Material mat in r.materials)
            {
                mat.renderQueue = 2002;
            }
        }
    }
}
