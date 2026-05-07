using UnityEngine;

/// <summary>
/// Basic WASD movement for a fixed-camera (Resident Evil style) game.
/// Movement is relative to the camera, so "W" always means "into the screen"
/// regardless of which direction the camera is facing. The character rotates
/// to face its movement direction.
/// Attach to a GameObject with a CharacterController.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeed = 720f;   // degrees / second

    [Header("Gravity")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Camera Reference")]
    [Tooltip("The camera the player movement is relative to. Usually Main Camera.")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Vector3 verticalVelocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // 1. Read input.
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v);
        if (input.sqrMagnitude > 1f) input.Normalize();

        // 2. Build a movement direction relative to the camera (flattened to XZ).
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight   = cameraTransform.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * input.y + camRight * input.x);

        // 3. Apply horizontal motion.
        Vector3 horizontal = moveDir * moveSpeed;

        // 4. Apply gravity / vertical motion.
        if (controller.isGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = -2f; // small constant to keep grounded
        else
            verticalVelocity.y += gravity * Time.deltaTime;

        // 5. Move.
        Vector3 finalVelocity = horizontal + verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);

        // 6. Rotate to face movement direction.
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }
}
