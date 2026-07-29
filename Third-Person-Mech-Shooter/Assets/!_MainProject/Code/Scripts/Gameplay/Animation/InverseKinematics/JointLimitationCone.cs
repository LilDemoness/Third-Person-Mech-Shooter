using UnityEngine;

namespace Gameplay.Animations.InverseKinematics
{
    [System.Serializable]
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
            float maxAngle = Angle * 0.5f;

            if (currentAngle <= maxAngle)
                return direction; // We're within the valid angle range, so the direction is unchanged.

            // We're outside the valid angle range.
            // Calculate the closest direction within the range.

            // Define a plane using the central axis and the direction vector.
            Vector3 planeNormal =
                MathUtils.IsApproximatelyEqual(currentAngle, 360.0f) ? centreAxis.GetAnyPerpendicular() // Select an arbitraty perpendicular axis if the direction vector is completely opposite to the central axis.
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


        public override void DrawLimitationGizmos(Transform joint, Quaternion jointRotation, Vector3 forward, Vector3 right, Vector3 up)
        {
            const float GIZMO_LENGTH = 0.2f;
            const int ITERATIONS = 3;

            // Basic Implementation.
            Quaternion rightRot = Quaternion.AngleAxis(Angle * 0.5f, Vector3.forward);

            // Draw our default rays.
            Gizmos.color = Color.green;
            Vector3 gizmoVector = Vector3.up * GIZMO_LENGTH;
            float angle = 360.0f / (ITERATIONS * 4);
            for(int i = 0; i < 4 * ITERATIONS; ++i)
            {
                Quaternion cornerRotation = Quaternion.AngleAxis(angle * i, Vector3.up);
                Vector3 current = jointRotation * cornerRotation * rightRot * gizmoVector;
                Vector3 previous = jointRotation * Quaternion.AngleAxis(angle * (i - 1), Vector3.up) * rightRot * gizmoVector;

                Gizmos.DrawLine(joint.position, joint.position + current);
                Gizmos.DrawLine(joint.position + previous, joint.position + current);
            }            
        }
    }
}