using UnityEngine;

public class LookAt
{
    public bool LookAtTarget(Transform monster, Transform target, float rotationSpeed)
    {
        float epsilon = 0.01f;
        Vector3 direction = (target.position - monster.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        monster.rotation = Quaternion.Slerp(monster.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        if (Quaternion.Angle(monster.rotation, lookRotation) < epsilon)
        {
            monster.rotation = lookRotation; // Snap to the exact rotation if within epsilon
            return true; // Finished looking at the target
        }

        return false; // Still rotating towards the target
    }
}
