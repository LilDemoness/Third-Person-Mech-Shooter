using UnityEngine;

namespace Gameplay.Animations
{
    public struct Matrix3x3 : IMatrixBase
    {
        // Row 0.
        float m00;
        float m01;
        float m02;

        // Row 1.
        float m10;
        float m11;
        float m12;

        // Row 2.
        float m20;
        float m21;
        float m22;


        public Matrix3x3(float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m02 = m02;

            this.m10 = m10;
            this.m11 = m11;
            this.m12 = m12;

            this.m20 = m20;
            this.m21 = m21;
            this.m22 = m22;
        }
        public static Matrix3x3 Identity => new Matrix3x3(1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);


        public float this[int row, int column]
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return this[row + column * 3];
            }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                this[row + column * 3] = value;
            }
        }
        public float this[int index]
        {
            get
            {
                return index switch
                {
                    0 => m00,
                    1 => m10,
                    2 => m20,

                    3 => m01,
                    4 => m11,
                    5 => m21,

                    6 => m02,
                    7 => m12,
                    8 => m22,

                    _ => throw new System.IndexOutOfRangeException("Invalid matrix index!")
                };
            }
            set
            {
                switch (index)
                {
                    case 0: m00 = value; break;
                    case 1: m10 = value; break;
                    case 2: m20 = value; break;

                    case 3: m01 = value; break;
                    case 4: m11 = value; break;
                    case 5: m21 = value; break;

                    case 6: m02 = value; break;
                    case 7: m12 = value; break;
                    case 8: m22 = value; break;

                    default: throw new System.IndexOutOfRangeException("Invalid matrix index!");
                }
            }
        }
        public float GetCoefficient(int row, int column) => this[row, column];


        public void SetIdentity()
        {
            /*for (int row = 0; row < 3; ++row)
                for (int column = 0; column < 3; ++column)
                    this[row, column] = row == column ? 1 : 0;*/
            m00 = 1; m10 = 0; m20 = 0;
            m01 = 0; m11 = 1; m21 = 0;
            m02 = 0; m12 = 0; m22 = 1;
        }
        public void SetZeros()
        {
            m00 = 0; m10 = 0; m20 = 0;
            m01 = 0; m11 = 0; m21 = 0;
            m02 = 0; m12 = 0; m22 = 0;
        }
        public void SetOnes()
        {
            m00 = 1; m10 = 1; m20 = 1;
            m01 = 1; m11 = 1; m21 = 1;
            m02 = 1; m12 = 1; m22 = 1;
        }


        public Vector3 GetColumn(int columnIndex)
        {
            return columnIndex switch
            {
                0 => new Vector3(m00, m10, m20),
                1 => new Vector3(m01, m11, m21),
                2 => new Vector3(m02, m12, m22),

                _ => throw new System.IndexOutOfRangeException("Invalid Column Index")
            };
        }
        public void SetColumn(int columnIndex, Vector3 newValues) => SetColumn(columnIndex, newValues.x, newValues.y, newValues.z);
        public void SetColumn(int columnIndex, float row0, float row1, float row2)
        {
            switch (columnIndex)
            {
                case 0: m00 = row0; m10 = row1; m20 = row2; break;
                case 1: m01 = row0; m11 = row1; m21 = row2; break;
                case 2: m02 = row0; m12 = row1; m22 = row2; break;

                default:
                    throw new System.IndexOutOfRangeException("Invalid Column Index");
            }
        }
        public int GetColumnCount() => 3;


        public Vector3 GetRow(int rowIndex)
        {
            return rowIndex switch
            {
                0 => new Vector3(m00, m01, m02),
                1 => new Vector3(m10, m11, m12),
                2 => new Vector3(m20, m21, m22),

                _ => throw new System.IndexOutOfRangeException("Invalid Row Index")
            };
        }
        public void SetRow(int rowIndex, Vector3 newValues) => SetRow(rowIndex, newValues.x, newValues.y, newValues.z);
        public void SetRow(int rowIndex, float col0, float col1, float col2)
        {
            switch (rowIndex)
            {
                case 0: m00 = col0; m01 = col1; m02 = col2; break;
                case 1: m10 = col0; m11 = col1; m12 = col2; break;
                case 2: m20 = col0; m21 = col1; m22 = col2; break;

                default:
                    throw new System.IndexOutOfRangeException("Invalid Row Index");
            }
        }
        public int GetRowCount() => 3;


        /// <summary>
        ///     Returns a new Matrix3x3 flipped over the primary diagonal.<br/>
        ///     E.g. [0,1] becomes [1,0]
        /// </summary>
        public Matrix3x3 GetTransposition() => 
            new Matrix3x3(
                m00, m10, m20,
                m01, m11, m21,
                m02, m12, m22
                );


        public void ApplyHouseholderOnTheLeft(IMatrixBase essential, float tau, ref float workspace);
        public void ApplyHouseholderOnTheRight(IMatrixBase essential, float tau, ref float workspace);

        public void ApplyOnTheLeft(int p, int q, JacobiRotation j);
        public void ApplyOnTheRight(int p, int q, JacobiRotation j);


        #region Equals Overloading



        #endregion

        #region Operator Overloading

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
        {
            return new Matrix3x3(
                // Top Row.
    // c[0,0] = (a[0,0] * b[0,0]) + (a[0,1] * b[1,0]) + (a[0,2] * b[2,0])
                (a.m00 * b.m00) + (a.m01 * b.m10) + (a.m02 * b.m20),
    // c[0,1] = (a[0,0] * b[0,1]) + (a[0,1] * b[1,1]) + (a[0,2] * b[2,1])
                (a.m00 * b.m01) + (a.m01 * b.m11) + (a.m02 * b.m21),
    // c[0,2] = (a[0,0] * b[0,2]) + (a[0,1] * b[1,2]) + (a[0,2] * b[2,2])
                (a.m00 * b.m02) + (a.m01 * b.m12) + (a.m02 * b.m22),

                // Middle Row.
    // c[1,0] = (a[1,0] * b[0,0]) + (a[1,1] * b[1,0]) + (a[1,2] * b[2,0])
                (a.m10 * b.m00) + (a.m11 * b.m10) + (a.m12 * b.m20),
    // c[1,1] = (a[1,0] * b[0,1]) + (a[1,1] * b[1,0]) + (a[1,2] * b[2,0])
                (a.m10 * b.m01) + (a.m11 * b.m11) + (a.m12 * b.m21),
    // c[1,2] = (a[1,0] * b[0,2]) + (a[1,1] * b[1,0]) + (a[1,2] * b[2,0])
                (a.m10 * b.m02) + (a.m11 * b.m12) + (a.m12 * b.m22),

                // Bottom Row.
    // c[2,0] = (a[2,0] * b[0,0]) + (a[2,1] * b[1,0]) + (a[2,2] * b[2,0])
                (a.m20 * b.m00) + (a.m21 * b.m10) + (a.m22 * b.m20),
    // c[2,1] = (a[2,0] * b[0,1]) + (a[2,1] * b[1,1]) + (a[2,2] * b[2,1])
                (a.m20 * b.m01) + (a.m21 * b.m11) + (a.m22 * b.m21),
    // c[2,2] = (a[2,0] * b[0,2]) + (a[2,1] * b[1,2]) + (a[2,2] * b[2,2])
                (a.m20 * b.m02) + (a.m21 * b.m12) + (a.m22 * b.m22)
                );
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Matrix3x3 a, Vector3 b)
        {
            return new Vector3(
                a.m00 * b.x + a.m01 * b.x + a.m02 * b.x,
                a.m10 * b.y + a.m11 * b.y + a.m12 * b.y,
                a.m20 * b.z + a.m21 * b.z + a.m22 * b.z
                );
        }

        #endregion
    }
}