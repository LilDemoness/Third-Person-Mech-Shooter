using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 RotateAroundY(this Vector3 point, float angle, Vector3 origin) => (Quaternion.AngleAxis(angle, Vector3.up) * (point - origin)) + origin;
}