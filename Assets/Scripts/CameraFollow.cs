using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 10, -8);
    public float lookHeight = 1f;

    [Header("Smoothing")]
    public float followSmoothTime = 0.12f;
    public float zoomSpeed = 8f;

    [Header("Collision")]
    public LayerMask obstacleMask = ~0;
    public float cameraRadius = 0.3f;
    public float minDistance = 3f;

    [Header("Debug")]
    public bool debugLogs = true;

    private Vector3 velocity;
    private float currentDistance;
    private Vector3 smoothedFocus;
    private Collider lastBlocker;
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    // initialize the camera follow component
    void Start()
    {
        // initialize the current distance based on the initial offset magnitude
        currentDistance = offset.magnitude;

        if (target == null)
        {
            Debug.LogError("[Camera] No target assigned. Drag the Player into the Target slot.", this);
            return;
        }

        // initialize the smoothed focus point to the target's position
        smoothedFocus = target.position;

        if (debugLogs)
            Debug.Log("[Camera] Following '" + target.name + "' | offset " + offset + " | min distance " + minDistance, this);
    }

    // update the camera position after all other updates
    void LateUpdate()
    {
        if (target == null) return;

        // smooth the focus point so the player's per-frame movement doesn't jitter the camera
        smoothedFocus = Vector3.SmoothDamp(smoothedFocus, target.position, ref velocity, followSmoothTime);

        // calculate the desired camera position based on the smoothed focus point and offset
        Vector3 focusPoint = smoothedFocus + Vector3.up * lookHeight;
        Vector3 direction = offset.normalized;
        float desiredDistance = offset.magnitude;
        float allowedDistance = desiredDistance;
        Collider blockedBy = null;

        // perform a sphere cast to detect obstacles between the focus point and the desired camera position
        int count = Physics.SphereCastNonAlloc(focusPoint, cameraRadius, direction, hitBuffer,
            desiredDistance, obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            // skip the first hit if it's the player itself or other non-blocking objects
            RaycastHit h = hitBuffer[i];
            if (h.transform.IsChildOf(target)) continue;          // player + sword
            if (h.collider.CompareTag("Gear")) continue;          // any gear, even if not parented
            if (h.collider.attachedRigidbody != null) continue;   // enemies, moving props
            if (h.distance <= 0f) continue;                       // cast started inside it

            // calculate the distance to the hit point, ensuring it respects the minimum distance
            float d = Mathf.Max(h.distance, minDistance);
            if (d < allowedDistance)
            {
                allowedDistance = d;
                blockedBy = h.collider;
            }
        }

        if (debugLogs && blockedBy != lastBlocker)
        {
            if (blockedBy != null)
                Debug.Log("[Camera] Blocked by '" + blockedBy.name + "' at distance " + allowedDistance.ToString("F1"), blockedBy);
            else
                Debug.Log("[Camera] Clear again");
        }

        // update the last blocker reference
        lastBlocker = blockedBy;

        // smoothly interpolate the current distance towards the allowed distance
        currentDistance = Mathf.Lerp(currentDistance, allowedDistance, zoomSpeed * Time.deltaTime);

        // update the camera position based on the smoothed distance and direction
        transform.position = focusPoint + direction * currentDistance;
        transform.LookAt(focusPoint);
    }
}