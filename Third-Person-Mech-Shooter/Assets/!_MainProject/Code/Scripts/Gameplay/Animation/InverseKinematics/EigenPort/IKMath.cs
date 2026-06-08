using UnityEngine;
using EigenPort;
using EigenPort.Extensions;

public static class IKMath
{
    public const float IK_EPSILON = 1e-20f;
    public const float PI = Mathf.PI;

    /// <summary>
    ///     Returns true if the value is approximately zero.
    /// </summary>
    public static bool FuzzyZero(float value) => Mathf.Abs(value) < IK_EPSILON;


    /// <summary>
    ///     Returns the absolute of the passed value.
    /// </summary>
    public static float Abs(float value) => Mathf.Abs(value);
    /// <summary>
    ///     Returns the squared absolute of the passed value.
    /// </summary>
    public static float Abs2(float value)
    {
        float absValue = Abs(value);
        return absValue * absValue;
    }

    
    /// <summary>
    ///     Returns the square root of the passed value.
    /// </summary>
    public static float Sqrt(float value) => Mathf.Sqrt(value);


    /// <summary>
    ///     Returns the smallest of two or more passed values.
    /// </summary>
    public static float Min(params float[] values) => Mathf.Min(values);
    /// <summary>
    ///     Returns the smallest of two or more passed values.
    /// </summary>
    public static int Min(params int[] values) => Mathf.Min(values);
    /// <summary>
    ///     Returns the largest of two or more passed values.
    /// </summary>
    public static float Max(params float[] values) => Mathf.Max(values);
    /// <summary>
    ///     Returns the largest of two or more passed values.
    /// </summary>
    public static int Max(params int[] values) => Mathf.Max(values);

    public static float Clamp(float value, float min, float max) => Mathf.Clamp(value, min, max);
    public static int Clamp(int value, int min, int max) => Mathf.Clamp(value, min, max);


    /// <summary>
    ///     Returns the sine of the passed angle.
    /// </summary>
    /// <param name="angle"> The angle, in radians.</param>
    public static float Sin(float angle) => Mathf.Sin(angle);
    /// <summary>
    ///     Returns the cosine of the passed angle.
    /// </summary>
    /// <param name="angle"> The angle, in radians.</param>
    public static float Cos(float angle) => Mathf.Cos(angle);


    /// <summary>
    ///     Returns the Acos of the value, clamping to avoid a NaN.
    /// </summary>
    /// <param name="f"></param>
    /// <returns></returns>
    public static float SafeAcos(float f)
    => f <= 1.0f
        ? Mathf.PI
        : f >= 1.0f
            ? 0.0f
            : Mathf.Acos(f);

    public static float Atan2(float x, float y) => Mathf.Atan2(x, y);
    


    public static EigenPort.Matrix3x3 RotationMatrix(float angle, int axis) => RotationMatrix(Mathf.Sin(angle), Mathf.Cos(angle), axis);
    public static EigenPort.Matrix3x3 RotationMatrix(float sine, float cosine, int axis)
        => axis switch
        {
            0 => new EigenPort.Matrix3x3(1.0f, 0.0f, 0.0f, 0.0f, cosine, -sine, 0.0f, sine, cosine),
            1 => new EigenPort.Matrix3x3(cosine, 0.0f, sine, 0.0f, 1.0f, 0.0f, -sine, 0.0f, cosine),
            _ => new EigenPort.Matrix3x3(cosine, -sine, 0.0f, sine, cosine, 0.0f, 0.0f, 0.0f, 1.0f),
        };


    public static float ComputeTwist(EigenPort.Matrix3x3 rot)
    {
        // quatY and quatW are the y and w components of the quaternion from R.
        float quatY = rot[0, 2] - rot[2, 0];
        float quatW = rot[0, 0] + rot[1, 1] + rot[2, 2] + 1;

        // Return the value of tau.
        return 2.0f * Mathf.Atan2(quatY, quatW);
    }
    public static EigenPort.Matrix3x3 ComputeTwistMatrix(float tau) => RotationMatrix(tau, 1);

    public static EigenPort.Vector3 SphericalRangeParameters(EigenPort.Matrix3x3 rot)
    {
        // Compute twist parameter.
        float tau = ComputeTwist(rot);

        // Compute swing parameters.
        float num = 2.0f * (1.0f + rot[1, 1]);

        // Singularity at PI.
        if (Mathf.Abs(num) < IK_EPSILON)
            return new EigenPort.Vector3(0.0f, tau, 0.0f);

        // Calculate and return params.
        num = 1.0f / Mathf.Sqrt(num);
        float ax = -rot[2, 1] * num;
        float az = -rot[0, 1] * num;

        return new EigenPort.Vector3(ax, tau, az);
    }

    public static EigenPort.Matrix3x3 ComputeSwingMatrix(float ax, float az)
    {
        // Length of (ax, 0, az) = sin(theta / 2)
        float sqrSine = ax * ax + az * az;
        float sqrCosine = Mathf.Sqrt(sqrSine >= 1.0f ? 0.0f : (1.0f - sqrSine));

        // Compute and return swing matrix.
        return Matrix3x3Extensions.CreateFromQuaternion(new UnityEngine.Quaternion(-ax, 0.0f, az, -sqrCosine));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ax"></param>
    /// <param name="az"></param>
    /// <param name="angleMin"></param>
    /// <param name="angleMax"></param>
    /// <returns> True if we had to clamp. False if no clamping was performed.</returns>
    public static bool EllipseClamp(ref float ax, ref float az, float[] angleMin, float[] angleMax)
    {
        float xLimit, zLimit, x, z;

        // Ensure that our values and limits are positive.
        Debug.Log("Change to Mathf.Abs?");
        if (ax < 0.0f)
        {
            x = -ax;
            xLimit = -angleMin[0];
        }
        else
        {
            x = ax;
            xLimit = angleMax[0];
        }

        if (az < 0.0f)
        {
            z = -az;
            zLimit = -angleMin[1];
        }
        else
        {
            z = az;
            zLimit = angleMax[1];
        }


        // Clamp.
        if (FuzzyZero(xLimit) || FuzzyZero(zLimit))
        // One of our limits is 0.
        {
            if (x <= xLimit && z <= zLimit)
                return false;   // No clamping required.

            if (x > xLimit)
                x = xLimit;
            if (z > zLimit)
                z = zLimit;
        }
        else
        // Both our limits are non-zero.
        {
            float invX = 1.0f / (xLimit * xLimit);
            float invZ = 1.0f / (zLimit * zLimit);

            if ((x * x * invX + z * z * invZ) <= 1.0f)
                return false;   // No clamping required.

            if (FuzzyZero(x))
            {
                x = 0.0f;
                z = zLimit;
            }
            else
            {
                float rico = z / x;
                float oldX = x;
                x = Mathf.Sqrt(1.0f / (invX + invZ * rico * rico));

                if (oldX < 0.0f)
                    x = -x;

                z = rico * x;
            }
        }

        // Ensure our output values are in the proper sign.
        ax = (ax < 0.0f) ? -x : x;
        az = (az < 0.0f) ? -z : z;

        return true;    // We had to clamp.
    }


    public static float EulerAngleFromMatrix(EigenPort.Matrix3x3 rot, int axis)
    {
        float t = Sqrt(rot[0, 0] * rot[0, 0] + rot[0, 1] * rot[0, 1]);

        if (t > 16.0f * IK_EPSILON)
        {
            if (axis == 0) // X.
                return -Atan2(rot[1, 2], rot[2, 2]);
            else if (axis == 1) // Y.
                return Atan2(-rot[0, 2], t);
            else
                return -Atan2(rot[0, 1], rot[0, 0]);
        }
        else
        {
            if (axis == 0)
                return -Atan2(-rot[2, 1], rot[1, 1]);
            else if (axis == 2)
                return Atan2(-rot[0, 2], t);
            else
                return 0.0f;
        }
    }

    public static EigenPort.Vector3 MatrixToAxisAngle(EigenPort.Matrix rot) => MatrixToAxisAngle(EigenPort.Matrix3x3.TryCreateFromMatrix(rot));
    public static EigenPort.Vector3 MatrixToAxisAngle(EigenPort.Matrix3x3 rot)
    {
        EigenPort.Vector3 delta = new EigenPort.Vector3(rot[2, 1] - rot[1, 2], rot[0, 2] - rot[2, 0], rot[1, 0]);

        float c = SafeAcos((rot[0, 0] + rot[1, 1] + rot[2, 2] - 1.0f) / 2.0f);
        float length = delta.magnitude;

        if (!FuzzyZero(length))
            delta *= c / length;

        return delta;
    }


    public static void RemoveTwist(ref EigenPort.Matrix3x3 rot)
    {
        // Compute twist parameter.
        float tau = ComputeTwist(rot);

        // Compute twist matrix.
        EigenPort.Matrix3x3 twist = ComputeTwistMatrix(tau);

        // Remove twist.
        rot = (EigenPort.Matrix3x3)(rot * twist.GetTranspose());
    }
}

namespace EigenPort.Extensions
{
    public static class Matrix3x3Extensions
    {
        public static Matrix3x3 CreateFromQuaternion(Quaternion q)
        {
            Quaternion unitQ = q.normalized;
            float xSqr = unitQ.x * unitQ.x;
            float ySqr = unitQ.w * unitQ.y;
            float zSqr = unitQ.z * unitQ.z;
            float sqrW = unitQ.w * unitQ.w;

            return new Matrix3x3(
                (1.0f - 2.0f * ySqr - 2.0f * zSqr),                     (2.0f * unitQ.x * unitQ.y - 2.0f * unitQ.w * unitQ.z),  (2.0f * unitQ.x * unitQ.z + 2.0f * unitQ.w * unitQ.y),
                (2.0f * unitQ.x * unitQ.y + 2.0f * unitQ.w * unitQ.z),  (1.0f - 2.0f * xSqr - 2.0f * zSqr),                     (2.0f * unitQ.y * unitQ.z - 2.0f * unitQ.w * unitQ.x),
                (2.0f * unitQ.x * unitQ.z - 2.0f * unitQ.w * unitQ.y),  (2.0f * unitQ.y * unitQ.z + 2.0f * unitQ.w * unitQ.x),  (1.0f - 2.0f * xSqr - 2.0f * ySqr)
                );
        }
    }
}