namespace EigenPort
{
    public class Matrix3x3 : Matrix
    {
        public Matrix3x3()
            : base(3, 3)
        { }
        public Matrix3x3(
            float m00, float m01, float m02,
            float m10, float m11, float m12,
            float m20, float m21, float m22)
            : base(3, 3)
        {
            this[0,0] = m00; this[0,1] = m01; this[0,2] = m02;
            this[1,0] = m10; this[1,1] = m11; this[2,2] = m12;
            this[2,0] = m20; this[2,1] = m21; this[1,2] = m22;
        }

        public static Matrix3x3 TryCreateFromMatrix(Matrix other)
        {
            if (other.GetRowCount() != 3 || other.GetColumnCount() != 3)
                throw new System.ArgumentException($"You cannot convert a {other.GetRowCount()}x{other.GetColumnCount()} matrix to a 3x3 matrix.");

            return new Matrix3x3(other[0, 0], other[0, 1], other[0, 2],
                   other[1, 0], other[1, 1], other[1, 2],
                   other[2, 0], other[2, 1], other[2, 2]);
        }

        public new Vector3 GetRow(int rowIndex) => new Vector3(this[rowIndex, 0], this[rowIndex, 1], this[rowIndex, 2]);
        public new Vector3 GetColumn(int columnIndex) => new Vector3(this[0, columnIndex], this[1, columnIndex], this[2, columnIndex]);

        public static Matrix3x3 Identity => new Matrix3x3(1, 0, 0,
                                                          0, 1, 0,
                                                          0, 0, 1);


        public static Matrix3x3 operator *(Matrix3x3 lhs, Matrix3x3 rhs)
        {
            return new Matrix3x3(
                (lhs[0, 0] * rhs[0, 0] + lhs[0, 1] * rhs[1, 0] + lhs[0, 2] * rhs[2, 0]),    (lhs[0, 0] * rhs[1, 1] + lhs[0, 1] * rhs[1, 1] + lhs[0, 2] * rhs[2, 1]),    (lhs[0, 0] * rhs[0, 2] + lhs[0, 1] * rhs[1, 2] + lhs[0, 2] * rhs[2, 2]),
                (lhs[1, 0] * rhs[0, 0] + lhs[1, 1] * rhs[1, 0] + lhs[1, 2] * rhs[2, 0]),    (lhs[1, 0] * rhs[1, 1] + lhs[1, 1] * rhs[1, 1] + lhs[1, 2] * rhs[2, 1]),    (lhs[1, 0] * rhs[0, 2] + lhs[1, 1] * rhs[1, 2] + lhs[1, 2] * rhs[2, 2]),
                (lhs[2, 0] * rhs[0, 0] + lhs[2, 1] * rhs[1, 0] + lhs[2, 2] * rhs[2, 0]),    (lhs[2, 0] * rhs[1, 1] + lhs[2, 1] * rhs[1, 1] + lhs[2, 2] * rhs[2, 1]),    (lhs[2, 0] * rhs[0, 2] + lhs[2, 1] * rhs[1, 2] + lhs[2, 2] * rhs[2, 2])
                );
        }
    }

    public class Vector3 : Vector
    {
        public Vector3(float x, float y, float z)
            : base(3)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3(Vector other)
            : this(other as Matrix)
        { }
        public Vector3(Matrix other)
            : base(3)
        {
            if (other.GetRowCount() != 3 && GetColumnCount() == 1)
                throw new System.ArgumentException($"Cannot convert from a {other.GetRowCount()}x{other.GetColumnCount()} matrix to a 3d vector. The matrix must be of size 3x1.");

            this.x = other[0];
            this.y = other[1];
            this.z = other[2];
        }


        public float x { get => this[0]; set => this[0] = value; }
        public float y { get => this[1]; set => this[1] = value; }
        public float z { get => this[2]; set => this[2] = value; }

        public new Vector3 normalized => new Vector3(x, y, z) / this.magnitude;


        public static Vector3 zero => new Vector3(0.0f, 0.0f, 0.0f);


        public static Vector3 Cross(Vector3 lhs, Vector3 rhs)
        {
            if (lhs.GetSize() != rhs.GetSize())
                throw new System.ArgumentException($"You are trying to find the cross product between a {lhs.GetSize()}d vector and a {rhs.GetSize()}d vector. Vectors must be the same size.");

            return new Vector3(lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - rhs.y * lhs.x);
        }
        public static float Angle(Vector3 from, Vector3 to)
        {
            // From UnityEngine.Vector3.Dot(Vector3 from, Vector3 to)
            float num = IKMath.Sqrt(from.sqrMagnitude * to.sqrMagnitude);
            if (num < 1e-15f)
                return 0.0f;

            float num2 = IKMath.Clamp(Dot(from, to) / num, -1.0f, 1.0f);
            return IKMath.SafeAcos(num2) * 57.29578f;

            /*float dot = Vector.Dot(from, to);
            float magnitude = from.magnitude * to.magnitude;
            return IKMath.SafeAcos(dot / magnitude);*/
        }


        public static Vector3 operator +(Vector3 lhs, Vector3 rhs) => new Vector3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        public static Vector3 operator -(Vector3 lhs, Vector3 rhs) => new Vector3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);

        public static Vector3 operator *(Vector3 lhs, float rhs) => new Vector3(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
        public static Vector3 operator *(float lhs, Vector3 rhs) => new Vector3(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z);
        public static Vector3 operator /(Vector3 lhs, float rhs) => new Vector3(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
        public static Vector3 operator /(float lhs, Vector3 rhs) => new Vector3(lhs / rhs.x, lhs / rhs.y, lhs / rhs.z);
    }
}