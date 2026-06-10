using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target;

    [Header("Settings")]
    public float smoothSpeed = 0.125f; // How smoothly the camera follows (lower is smoother)
    public Vector3 offset = new Vector3(0, 1f, -10f); // Camera offset relative to the player

    [Header("Auto Clamping (Prevents showing empty space)")]
    [Tooltip("Drag your main background object here to lock the camera inside it")]
    public SpriteRenderer backgroundBounds;

    private Camera cam;

    private float shakeDuration = 0f;
    private float shakeAmount = 0.1f;
    private float decreaseFactor = 1.0f;
    private Vector3 currentShakeOffset = Vector3.zero;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Calculate target position with the offset
        Vector3 desiredPosition = target.position + offset;

        // Smoothly interpolate between camera's current position and desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Auto clamp inside background bounds if a background is assigned
        if (backgroundBounds != null && cam != null)
        {
            Bounds bgBounds = backgroundBounds.bounds;

            // Get camera viewport dimensions in world space
            float camHeight = cam.orthographicSize;
            float camWidth = camHeight * cam.aspect;

            // Calculate clamp limits so the camera viewport never goes outside the background bounds
            float minX = bgBounds.min.x + camWidth;
            float maxX = bgBounds.max.x - camWidth;
            float minY = bgBounds.min.y + camHeight;
            float maxY = bgBounds.max.y - camHeight;

            // If the background is narrower than the camera, center the camera horizontally
            float clampedX = (bgBounds.size.x < camWidth * 2f) 
                ? bgBounds.center.x 
                : Mathf.Clamp(smoothedPosition.x, minX, maxX);

            // If the background is shorter than the camera, center the camera vertically
            float clampedY = (bgBounds.size.y < camHeight * 2f) 
                ? bgBounds.center.y 
                : Mathf.Clamp(smoothedPosition.y, minY, maxY);

            smoothedPosition = new Vector3(clampedX, clampedY, smoothedPosition.z);
        }

        // Apply shake offset if active
        if (shakeDuration > 0)
        {
            currentShakeOffset = Random.insideUnitSphere * shakeAmount;
            currentShakeOffset.z = 0f;
            shakeDuration -= Time.deltaTime * decreaseFactor;
        }
        else
        {
            currentShakeOffset = Vector3.zero;
        }

        // Apply the position with shake offset
        transform.position = smoothedPosition + currentShakeOffset;
    }

    public void TriggerShake(float duration = 0.15f, float amount = 0.1f)
    {
        shakeDuration = duration;
        shakeAmount = amount;
    }
}
