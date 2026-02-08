using UnityEngine;

public class OrbitAroundTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float orbitRadius = 1f;
    [SerializeField] private float angularSpeedDegreesPerSecond = 45f;
    [SerializeField] private Vector3 orbitAxis = Vector3.up;
    [SerializeField] private bool matchTargetScale = true;
    [SerializeField] private bool selfSpinEnabled = true;
    [SerializeField] private float selfSpinDegreesPerSecond = 90f;

    // Internal current angle tracking (in degrees) for stable orbit radius
    private float _currentAngleDegrees;

    private void Reset()
    {
        // Sensible defaults when the component is first added
        orbitRadius = 1f;
        angularSpeedDegreesPerSecond = 45f;
        orbitAxis = Vector3.up;
        matchTargetScale = true;
        selfSpinEnabled = true;
        selfSpinDegreesPerSecond = 90f;
    }

    private void Start()
    {
        // Ensure we have a normalized orbit axis to avoid errors
        if (orbitAxis == Vector3.zero)
        {
            orbitAxis = Vector3.up;
        }
        else
        {
            orbitAxis = orbitAxis.normalized;
        }

        if (target != null)
        {
            // If orbit radius is not positive, derive it from current position
            if (orbitRadius <= 0f)
            {
                orbitRadius = Vector3.Distance(transform.position, target.position);
                if (orbitRadius <= 0f)
                    orbitRadius = 1f;
            }

            // Initialize position on the orbit circle, preserving current direction if possible
            Vector3 offset = transform.position - target.position;
            if (offset.sqrMagnitude < 0.0001f)
            {
                // If we're exactly at the target, place the sphere on +X at the desired radius
                offset = Vector3.right * orbitRadius;
            }
            else
            {
                offset = offset.normalized * orbitRadius;
            }

            transform.position = target.position + offset;

            // Initialize current angle by projecting onto orbit plane and measuring around axis
            // For simplicity we start at angle 0 along the current offset direction.
            _currentAngleDegrees = 0f;

            // Match scale from the start if requested
            if (matchTargetScale)
            {
                transform.localScale = target.localScale;
            }
        }
    }

    private void Update()
    {
        if (target == null)
            return;

        // Continuously match scale if enabled
        if (matchTargetScale)
        {
            transform.localScale = target.localScale;
        }

        // Orbit movement
        float deltaAngle = angularSpeedDegreesPerSecond * Time.deltaTime;
        _currentAngleDegrees += deltaAngle;

        // Compute new position by rotating an initial offset around the orbit axis
        // Get current radial direction from target to this object
        Vector3 fromTarget = transform.position - target.position;

        if (fromTarget.sqrMagnitude < 0.0001f)
        {
            // If we somehow got too close, re-establish a valid offset
            fromTarget = Vector3.right * orbitRadius;
        }
        else
        {
            fromTarget = fromTarget.normalized * orbitRadius;
        }

        // Rotate the offset around the orbit axis
        Quaternion rotationStep = Quaternion.AngleAxis(deltaAngle, orbitAxis);
        Vector3 newOffset = rotationStep * fromTarget;

        // Apply new position, enforcing the exact orbit radius
        newOffset = newOffset.normalized * orbitRadius;
        transform.position = target.position + newOffset;

        // Optional self-spin around its own up axis
        if (selfSpinEnabled)
        {
            transform.Rotate(Vector3.up, selfSpinDegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        if (target == null)
            return;

        // Recompute orbit radius based on new target
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > 0f)
        {
            orbitRadius = distance;
        }
        else if (orbitRadius <= 0f)
        {
            orbitRadius = 1f;
            transform.position = target.position + Vector3.right * orbitRadius;
        }

        // Immediately match scale if enabled
        if (matchTargetScale)
        {
            transform.localScale = target.localScale;
        }
    }
}