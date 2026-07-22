using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    public class JointLimitationCone : JointLimitation
    {
        [field: SerializeField] public float Angle { get; private set; } = 90.0f;

        protected override Vector3 Solve(Vector3 direction)
        {
            // Assume the central axis (Forward of the cone) is +Y.
            // This is based on the coordinate system set by JointLimitation.MakeSpace(Vector3, Vector3, Quaternion).
            Vector3 centreAxis = new Vector3(0, 1, 0);
            
            // Apply the limitation if the angle exceeds radiusRange * PI.
            float currentAngle = Vector3.Angle(direction, centreAxis);
            float maxAngle = Angle * 0.5f * Mathf.Deg2Rad;

            if (currentAngle <= maxAngle)
                return direction; // We're within the valid angle range, so the direction is unchanged.

            // We're outside the valid angle range.
            // Calculate the closest direction within the range.

            // Define a plane using the central axis and the direction vector.
            Vector3 planeNormal =
                MathUtils.IsApproximatelyEqual(currentAngle, Mathf.PI) ? centreAxis.GetAnyPerpendicular() // Select an arbitraty perpendicular axis if the direction vector is completely opposite to the central axis.
                : Vector3.Cross(centreAxis, direction).normalized; // Otherwise, calculate plane normal normally.

            // Calculate a vector rotated by the maximum allowed angle along the plane.
            Quaternion rotation = Quaternion.AngleAxis(maxAngle, planeNormal);
            Vector3 limitedDir = rotation * centreAxis;

            // Return the vector within the limitation range that is closest to the passed direction.
            // This aims to preserve the directionality as much as possible.
            Vector3 projection = direction - centreAxis * Vector3.Dot(direction, centreAxis);
            if (projection.sqrMagnitude > 0.00001f)
            {
                Vector3 sideDir = projection.normalized;
                Quaternion sideRotation = Quaternion.AngleAxis(maxAngle, Vector3.Cross(centreAxis, sideDir).normalized);
                limitedDir = sideRotation * centreAxis;
            }

            // Return the limited direction.
            return limitedDir.normalized;
        }
    }
}