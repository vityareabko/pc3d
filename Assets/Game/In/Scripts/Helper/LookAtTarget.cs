using UnityEngine;

[DisallowMultipleComponent]
public class LookAtTarget : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 worldUp = Vector3.up;

    [Header("Axes")]
    [Tooltip("Allow rotation on the X axis (pitch).")]
    public bool x = true;
    [Tooltip("Allow rotation on the Y axis (yaw).")]
    public bool y = true;
    [Tooltip("Allow rotation on the Z axis (roll).")]
    public bool z = true;

    [Header("Smoothing")]
    [Tooltip("Max turn speed in degrees per second. 0 = instant.")]
    public float maxDegreesPerSecond = 360f;
    [Tooltip("Update in LateUpdate (recommended for cameras).")]
    public bool useLateUpdate = true;

    void Update()
    {
        if (!useLateUpdate) DoLook();
    }

    void LateUpdate()
    {
        if (useLateUpdate) DoLook();
    }

    private void DoLook()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude < 1e-8f) return;

        // Desired full look rotation
        Quaternion desired = Quaternion.LookRotation(toTarget, worldUp);

        // Apply axis mask via eulers (preserve disabled axes)
        Vector3 curEuler = transform.rotation.eulerAngles;
        Vector3 desEuler = desired.eulerAngles;

        // Use DeltaAngle to avoid 0/360 jumps
        float newX = x ? curEuler.x + Mathf.DeltaAngle(curEuler.x, desEuler.x) : curEuler.x;
        float newY = y ? curEuler.y + Mathf.DeltaAngle(curEuler.y, desEuler.y) : curEuler.y;
        float newZ = z ? curEuler.z + Mathf.DeltaAngle(curEuler.z, desEuler.z) : curEuler.z;

        Quaternion maskedTarget = Quaternion.Euler(newX, newY, newZ);

        if (maxDegreesPerSecond <= 0f)
        {
            transform.rotation = maskedTarget;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                maskedTarget,
                maxDegreesPerSecond * Time.deltaTime
            );
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawWireSphere(target.position, 0.05f);
    }
#endif
}
