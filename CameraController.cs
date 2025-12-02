using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Look Sensitivity")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 90f;

    [Header("Camera Bob")]
    [SerializeField] private float bobSpeed = 5f;
    [SerializeField] private float bobAmount = 0.1f;
    [SerializeField] private float sprintBobMultiplier = 1.5f;
    [SerializeField] private float crouchBobMultiplier = 0.5f;

    [Header("Head Movement")]
    [SerializeField] private float headBobSmoothing = 10f;

    private float xRotation = 0f;
    private Vector3 originalCameraPosition;
    private float bobTimer = 0f;
    private PlayerMovement playerMovement;
    private Rigidbody playerRigidbody;

    void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerRigidbody = GetComponentInParent<Rigidbody>();
        originalCameraPosition = transform.localPosition;

        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMouseLook();
        HandleCameraBob();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player left/right
        transform.parent.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down (clamped)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Unlock cursor with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void HandleCameraBob()
    {
        float speed = playerRigidbody != null 
            ? playerRigidbody.linearVelocity.magnitude 
            : 0f;

        // Get bob multiplier based on movement state
        float bobMultiplier = 1f;
        if (playerMovement != null)
        {
            if (playerMovement.IsSprinting())
                bobMultiplier = sprintBobMultiplier;
            else if (playerMovement.IsCrouching())
                bobMultiplier = crouchBobMultiplier;
        }

        // Only bob if moving
        if (speed > 0.1f)
        {
            bobTimer += Time.deltaTime * bobSpeed * bobMultiplier;
        }
        else
        {
            // Gradually stop bobbing
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 5f);
        }

        // Calculate bob offset
        Vector3 bobOffset = Vector3.zero;
        bobOffset.y = Mathf.Sin(bobTimer) * bobAmount * bobMultiplier;
        bobOffset.x = Mathf.Cos(bobTimer * 0.5f) * bobAmount * 0.5f * bobMultiplier;

        // Apply bob with smoothing
        Vector3 targetPosition = originalCameraPosition + bobOffset;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * headBobSmoothing);
    }
}
