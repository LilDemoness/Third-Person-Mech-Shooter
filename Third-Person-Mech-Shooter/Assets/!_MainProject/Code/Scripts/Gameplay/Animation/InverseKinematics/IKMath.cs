using UnityEngine;

namespace Gameplay.Animations
{
    public class Affine3
    {
        //typedef Transform<double,3,Affine> Affine3d;

        //template<typename _Scalar, int _Dim, int _Mode, int _Options>
        //class Transform

        private MatrixX _matrix;


        public Affine3()
        {
            _matrix = new MatrixX(3, 4);
        }
        public Affine3(Affine3 other)
        {
            _matrix = other._matrix;
        }
        public Affine3(Matrix3x3 linear, Vector3 translation)
        {
            _matrix = new MatrixX(3, 4);
            _matrix.Set3x3Submatrix(linear, 0, 0);
            _matrix.SetColumn(3, translation.ToVectorX());
        }


        public float this[int row, int column]
        {
            get => _matrix[row, column];
            set => _matrix[row, column] = value;
        }


        /// <summary>
        ///     Returns the linear part of the transformation.
        /// </summary>
        public Matrix3x3 GetLinear() => _matrix.Get3x3Submatrix(0, 0);
        public void SetLinear(Matrix3x3 newLinear) => _matrix.Set3x3Submatrix(newLinear, 0, 0);


        /*/// <summary>
        ///     Returns the 3x4 Affine part of the transformation.
        /// </summary>
        /// <returns></returns>
        public Matrix3x3 GetAffine() => ;
        public void SetAffine(Matrix3x3 newAffine) => ;*/


        /// <summary>
        ///     Returns the translation vector of the transformation.
        /// </summary>
        public Vector3 GetTranslation() => _matrix.GetColumn(3).ToVector3();
        public void SetTranslation(Vector3 newTranslation)
        {
            _matrix[0, 3] = newTranslation[0];
            _matrix[1, 3] = newTranslation[1];
            _matrix[2, 3] = newTranslation[2];
        }



        #region Operator Overloading

        /// <summary>
        ///     Concatenates two Affine3 Transformations.
        /// </summary>
        public static Affine3 operator *(Affine3 a, Affine3 b)
        {
            return new Affine3(
                linear: a.GetLinear() * b.GetLinear(),
                translation: a.GetLinear() * b.GetTranslation() + a.GetTranslation()
                );
        }

        #endregion
    }
    public class Translation3
    {

    }



    public struct Matrix3x3
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


        public void SetIdentity()
        {
            /*for (int row = 0; row < 3; ++row)
                for (int column = 0; column < 3; ++column)
                    this[row, column] = row == column ? 1 : 0;*/
            m00 = 1;
            m10 = 0;
            m20 = 0;

            m01 = 0;
            m11 = 1;
            m21 = 0;

            m02 = 0;
            m12 = 0;
            m22 = 1;
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
    public struct MatrixX
    {
        private float[] _values;
        private int _rowCount, _columnCount;
        private int _totalCount => _rowCount * _columnCount;

        public MatrixX(int rowCount, int columnCount)
        {
            _rowCount = rowCount;
            _columnCount = columnCount;
            _values = new float[rowCount * columnCount];

            SetZero();
        }

        public float this[int row, int column]
        {
            get => this[row + column * row];
            set => this[row + column * row] = value;
        }
        public float this[int index]
        {
            get => _values[index];
            set => _values[index] = value;
        }


        public void Resize(int rowCount, int columnCount)
        {
            if (_rowCount == rowCount && _columnCount == columnCount)
                return; // Already the correct size.

            _rowCount = rowCount;
            _columnCount = columnCount;
            _values = new float[_totalCount];
        }
        public void SetZero()
        {
            for (int i = 0; i < _totalCount; i++)
                _values[i] = 0.0f;
        }
        public void SetOnes()
        {
            for (int i = 0; i < _totalCount; i++)
                _values[i] = 1.0f;
        }
        public void SetIdentity() => SetIdentity(_rowCount, _columnCount);
        public void SetIdentity(int rows, int columns)
        {
            Resize(rows, columns);

            for (int row = 0; row < rows; ++row)
                for (int column = 0; column < columns; ++column)
                    this[row, column] = row == column ? 1 : 0;
        }


        public int GetRowCount() => _rowCount;
        public VectorX GetRow(int rowIndex)
        {
            if (rowIndex >= _rowCount)
                throw new System.IndexOutOfRangeException($"Invalid Row Index {rowIndex} for a {_rowCount}x{_columnCount} Matrix");

            VectorX returnedRow = new VectorX(_rowCount);
            for (int i = 0; i < _rowCount; ++i)
                returnedRow[i] = this[i, rowIndex];

            return returnedRow;
        }
        public void SetRow(int rowIndex, VectorX rowValues)
        {
            if (rowIndex >= _rowCount)
                throw new System.IndexOutOfRangeException($"Invalid Row Index {rowIndex} for a {_rowCount}x{_columnCount} Matrix");
            if (rowValues.GetSize() >= _columnCount)
                throw new System.ArgumentException($"Cannot set the values of a Row within a {_rowCount}x{_columnCount} Matrix with a Vector{rowValues.GetSize()}");

            for (int columnIndex = 0; columnIndex < rowValues.GetSize(); ++columnIndex)
                this[rowIndex, columnIndex] = rowValues[columnIndex];
        }
        public void SwapRows(int rowIndexA, int rowIndexB)
        {
            float temp;
            for (int i = 0; i < _columnCount; ++i)
            {
                temp = this[rowIndexA, i];
                this[rowIndexA, i] = this[rowIndexB, i];
                this[rowIndexB, i] = temp;
            }
        }


        public int GetColumnCount() => _columnCount;
        public VectorX GetColumn(int columnIndex)
        {
            if (columnIndex >= _columnCount)
                throw new System.IndexOutOfRangeException($"Invalid Column Index {columnIndex} for a {_rowCount}x{_columnCount} Matrix");

            VectorX returnedColumn = new VectorX(_columnCount);
            for(int i = 0; i < _columnCount; ++i)
                returnedColumn[i] = this[i, columnIndex];

            return returnedColumn;
        }
        public void SetColumn(int columnIndex, VectorX columnValues)
        {
            if (columnIndex >= _columnCount)
                throw new System.IndexOutOfRangeException($"Invalid Column Index {columnIndex} for a {_rowCount}x{_columnCount} Matrix");
            if (columnValues.GetSize() >= _rowCount)
                throw new System.ArgumentException($"Cannot set the values of a Column within a {_rowCount}x{_columnCount} Matrix with a Vector{columnValues.GetSize()}");

            for (int rowIndex = 0; rowIndex < columnValues.GetSize(); ++rowIndex)
                this[rowIndex, columnIndex] = columnValues[rowIndex];
        }
        public void SwapColumns(int columnIndexA, int columnIndexB)
        {
            float temp;
            for(int i = 0; i < _rowCount; ++i)
            {
                temp = this[i, columnIndexA];
                this[i, columnIndexA] = this[i, columnIndexB];
                this[i, columnIndexB] = temp;
            }
        }


        public MatrixX GetSubmatrix(int rows, int columns, int rowOffset, int columnOffset)
        {
            if ((rows + rowOffset) >= _rowCount)
                throw new System.ArgumentException($"Invalid Row Count '{rows}' or Offset '{rowOffset}' for a {_rowCount}x{_columnCount} Matrix");
            if ((columns + columnOffset) >= _columnCount)
                throw new System.ArgumentException($"Invalid Column Count '{columns}' or Offset '{columnOffset}' for Matrix{_rowCount}x{_columnCount}");

            MatrixX returnedMatrix = new MatrixX(rows, columns);
            for(int row = 0; row < rows; ++row)
                for(int column = 0; column < columns; ++column)
                    returnedMatrix[row, column] = this[row + rowOffset, column + columnOffset];

            return returnedMatrix;
        }
        public void SetSubmatrix(MatrixX newValues, int rowOffset, int columnOffset)
        {
            if ((newValues._rowCount + rowOffset) >= _rowCount)
                throw new System.ArgumentException($"Invalid Row Count '{newValues._rowCount}' or Offset '{rowOffset}' for Matrix{_rowCount}x{_columnCount}");
            if ((newValues._columnCount + columnOffset) >= _columnCount)
                throw new System.ArgumentException($"Invalid Column Count '{newValues._columnCount}' or Offset '{columnOffset}' for Matrix{_rowCount}x{_columnCount}");

            for(int row = 0; row < newValues._rowCount; ++row)
                for(int column = 0; column < newValues._columnCount; ++column)
                    this[row + rowOffset, column + columnOffset] = newValues[row, column];
        }

        public Matrix3x3 Get3x3Submatrix(int rowOffset, int columnOffset)
        {
            if ((3 + rowOffset) >= _rowCount)
                throw new System.ArgumentException($"Invalid Row Count '3' or Offset '{rowOffset}' for Matrix{_rowCount}x{_columnCount}");
            if ((3 + columnOffset) >= _columnCount)
                throw new System.ArgumentException($"Invalid Column Count '3' or Offset '{columnOffset}' for Matrix{_rowCount}x{_columnCount}");

            Matrix3x3 returnedMatrix = new Matrix3x3();
            for (int row = 0; row < 3; ++row)
                for (int column = 0; column < 3; ++column)
                    returnedMatrix[row, column] = this[row + rowOffset, column + columnOffset];

            return returnedMatrix;
        }
        public void Set3x3Submatrix(Matrix3x3 newValues, int rowOffset, int columnOffset)
        {
            if ((3 + rowOffset) >= _rowCount)
                throw new System.ArgumentException($"Invalid Row Count '3' or Offset '{rowOffset}' for Matrix{_rowCount}x{_columnCount}");
            if ((3 + columnOffset) >= _columnCount)
                throw new System.ArgumentException($"Invalid Column Count '3' or Offset '{columnOffset}' for Matrix{_rowCount}x{_columnCount}");

            for (int row = 0; row < 3; ++row)
                for (int column = 0; column < 3; ++column)
                    this[row + rowOffset, column + columnOffset] = newValues[row, column];
        }


        /// <summary>
        ///     Returns the coefficient-wise absolute value.
        /// </summary>
        /// <remarks>
        ///     Coefficient-wise operations are operations applied to the individual entries of a matrix.
        /// </remarks>
        public MatrixX GetCoefficientWiseAbs()
        {
            MatrixX result = new MatrixX(_rowCount, _columnCount);

            for(int row = 0; row < _rowCount; ++row)
                for(int column = 0; column < _columnCount; ++column)
                    result[row, column] = Mathf.Abs(this[row, column]);

            return result;
        }

        /// <summary>
        ///     Returns the maximum coefficient (Element) within the matrix.
        /// </summary>
        public float GetMaxCoefficient()
        {
            float maxValue = float.MinValue;

            for(int i = 0; i < _rowCount * _columnCount; ++i)
            {
                if (_values[i] > maxValue)
                    maxValue = _values[i];
            }

            return maxValue;
        }
        /// <inheritdoc cref="GetMaxCoefficient"/>
        public float GetMaxCoefficient(out int maxRowIndex, out int maxColumnIndex)
        {
            maxRowIndex = -1;
            maxColumnIndex = -1;
            float maxValue = float.MinValue;

            for (int row = 0; row < _rowCount; ++row)
            {
                for(int column = 0; column < _columnCount; ++column)
                {
                    if (this[row, column] > maxValue)
                    {
                        maxValue = _values[row];
                        maxRowIndex = row;
                        maxColumnIndex = column;
                    }
                }
            }

            return maxValue;
        }


        /// <summary>
        ///     Returns the main diagonal of the matrix (E.g. m00, m11, m22, etc)
        /// </summary>
        public VectorX GetDiagonal()
        {
            int diagonalLength = Mathf.Min(_rowCount, _columnCount);
            VectorX result = new VectorX(diagonalLength);
            for(int i = 0; i < diagonalLength; ++i)
                result[i] = this[i, i];

            return result;
        }
        public int GetDiagonalSize() => Mathf.Max(_rowCount, _columnCount);


        /// <summary>
        ///     Returns the conjugate transpose of the matrix.<br/>    
        ///     
        ///     For real matrices (Which all MatrixXs are in our implementation), the conjugate transpose is just the transpose.
        /// </summary>
        public MatrixX GetConjugate() => GetTransposition();



        public static VectorX operator *(MatrixX a, VectorX b)
        {
            if (a.GetRowCount() != b.GetSize())
                throw new System.ArithmeticException($"Cannot Multiply a Matrix{a.GetRowCount()}x{a.GetColumnCount()} with a Vector{b.GetSize()}");

            // Create an empty vector to hold our values (Initial values are 0).
            VectorX result = new VectorX(b.GetSize());

            // Multiply the two matrices together (Vectors are 1xX matrices).
            for (int row = 0; row < a.GetRowCount(); ++row)
                for (int column = 0; column < a.GetColumnCount(); ++column)
                    result[row] += a[row, column] * b[row];

            return result;
        }
        public static MatrixX operator *(MatrixX a, MatrixX b)
        {
            if (a.GetColumnCount() != b.GetRowCount()) // a's row count can differ from b's column count, but a's column count cannot differ from b's row count.
                throw new System.ArithmeticException($"Cannot Multiply a Matrix{a.GetRowCount()}x{a.GetColumnCount()} with a Matrix{b.GetRowCount()}x{b.GetColumnCount()}");
            // Create an empty vector to hold our values (Initial values are 0).
            MatrixX c = new MatrixX(a.GetRowCount(), b.GetColumnCount());

            // Multiply the two matrices together (Vectors are 1xX matrices).
            // Top Row.
            // c[0,0] = (a[0,0] * b[0,0]) + (a[0,1] * b[1,0]) + (a[0,2] * b[2,0])
            // c[0,1] = (a[0,0] * b[0,1]) + (a[0,1] * b[1,1]) + (a[0,2] * b[2,1])
            // c[0,2] = (a[0,0] * b[0,2]) + (a[0,1] * b[1,2]) + (a[0,2] * b[2,2])

            // Middle Row.
            // c[1,0] = (a[1,0] * b[0,0]) + (a[1,1] * b[1,0]) + (a[1,2] * b[2,0])
            // c[1,1] = (a[1,0] * b[0,1]) + (a[1,1] * b[1,0]) + (a[1,2] * b[2,0])
            // c[1,2] = (a[1,0] * b[0,2]) + (a[1,1] * b[1,0]) + (a[1,2] * b[2,0])

            // Bottom Row.
            // c[2,0] = (a[2,0] * b[0,0]) + (a[2,1] * b[1,0]) + (a[2,2] * b[2,0])
            // c[2,1] = (a[2,0] * b[0,1]) + (a[2,1] * b[1,1]) + (a[2,2] * b[2,1])
            // c[2,2] = (a[2,0] * b[0,2]) + (a[2,1] * b[1,2]) + (a[2,2] * b[2,2])
            for (int row = 0; row < c.GetRowCount(); ++row)
            {
                for (int column = 0; column < c.GetColumnCount(); ++column)
                {
                    for(int i = 0; i < b.GetColumnCount(); ++i)
                    {
                        c[row, column] += a[row, i] * b[i, column];
                    }
                }
            }

            return c;
        }


        public static MatrixX operator *(MatrixX a, float b)
        {
            MatrixX result = new MatrixX(a.GetRowCount(), a.GetColumnCount());

            for (int i = 0; i < a.GetRowCount() * a.GetColumnCount(); ++i)
                result[i] = a[i] * b;

            return result;
        }
        public static MatrixX operator /(MatrixX a, float b)
        {
            MatrixX result = new MatrixX(a.GetRowCount(), a.GetColumnCount());

            for(int i = 0; i < a.GetRowCount() * a.GetColumnCount(); ++i)
                result[i] = a[i] / b;

            return result;
        }


        /// <summary>
        ///     Returns a new MatrixX flipped over the primary diagonal.<br/>
        ///     E.g. [0,1] becomes [1,0]
        /// </summary>
        public MatrixX GetTransposition()
        {
            MatrixX returnedMatrix = new MatrixX(_columnCount, _rowCount);
            for(int row = 0; row < _rowCount; ++row)
                for (int column = 0; column < _columnCount; column++)
                    returnedMatrix[column, row] = this[row, column];

            return returnedMatrix;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///     A Vector of size 'X' is the same as a matrix the size 'X'x1 ('X' rows, 1 column).
    /// </remarks>
    public struct VectorX : System.IEquatable<VectorX>, System.IEquatable<UnityEngine.Vector2>, System.IEquatable<UnityEngine.Vector3>
    {
        private float[] _values;
        private int _size;

        public VectorX(int size)
        {
            _values = new float[size];
            _size = size;

            SetZero();
        }
        public VectorX(VectorX other)
            : this(other._size)
        {
            for (int i = 0; i < _size; ++i)
                _values[i] = other._values[i];
        }
        public VectorX(Vector3 other)
            : this(3)
        {
            _values[0] = other.x;
            _values[1] = other.y;
            _values[2] = other.z;
        }


        public float this[int index]
        {
            get => index < _size ? _values[_size] : throw new System.IndexOutOfRangeException($"Invalid index '{index}' for VectorX with size '{_size}'");
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index < _size)
                    _values[_size] = value;
                else
                    throw new System.IndexOutOfRangeException($"Invalid index '{index}' for VectorX with size '{_size}'");
            }
        }
        public int GetSize() => _size;

        public void Resize(int newSize)
        {
            _values = new float[newSize];
            _size = newSize;
        }
        public void SetZero()
        {
            for (int i = 0; i < _size; ++i)
                _values[i] = 0.0f;
        }
        public void SetOnes()
        {
            for (int i = 0; i < _size; ++i)
                _values[i] = 1.0f;
        }


        /// <summary>
        ///     Computes the Dot product of two same-size VectorXs
        /// </summary>
        /// <exception cref="System.ArgumentException"> Exception if the VectorXs are of differing sizes.</exception>
        public static float Dot(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArgumentException($"Cannot compute the Dot product between a Vector{a.GetSize()} and Vector{b.GetSize()}");

            float dotProduct = 0.0f;
            for(int i = 0; i < a.GetSize(); ++i)
                dotProduct += a[i] * b[i];

            return dotProduct;
        }

        /// <summary>
        ///     Returns the maximum coefficient (Element) within the Vector.
        /// </summary>
        /// <param name="index"> The index where the max value was found.</param>
        public float GetMaxCoefficient(out int index)
        {
            // Initial values.
            index = -1;
            float maxValue = float.MinValue;

            // Find the max value and its index.
            for(int i = 0; i < _size; ++i)
            {
                if (_values[i] > maxValue)
                {
                    index = i;
                    maxValue = _values[i];
                }
            }

            return maxValue;
        }
        /// <inheritdoc cref="GetMaxCoefficient(int)"/>
        public float GetMaxCoefficient() => GetMaxCoefficient(_);



        /// <summary>
        ///     Returns a new vector of the last <paramref name="tailSize"/> elements in the vector.
        /// </summary>
        public VectorX GetTail(int tailSize)
        {
            VectorX tail = new VectorX(tailSize);
            for(int i = 0; i < tailSize; ++i)
                tail[i] = this[_size - (i + 1)];

            return tail;
        }



        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            return other switch
            {
                VectorX otherVectorX => Equals(otherVectorX),
                Vector2 otherVector2 => Equals(otherVector2),
                Vector3 otherVector3 => Equals(otherVector3),

                _ => false
            };
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(VectorX other)
        {
            if (_size != other._size)
                return false; // Sizes differ to the two cannot be equal.

            for (int i = 0; i < _size; ++i)
                if (_values[i] != other._values[i])
                    return false; // A value doesn't match.

            return true; // Size and all values match.
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2 other)
        {
            if (_size != 2)
                return false; // The size of the VectorX is incompatible with a Vector2.

            // Return true if both values match.
            return _values[0] == other.x && _values[1] == other.y;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector3 other)
        {
            if (_size != 3)
                return false; // The size of the VectorX is incompatible with a Vector3.

            // Return true if all values match.
            return _values[0] == other.x && _values[1] == other.y && _values[2] == other.z;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            int hashCode = _values[0].GetHashCode();
            for(int i = 1; i < _size; ++i)
                hashCode ^= _values[i].GetHashCode();
            return hashCode;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator +(VectorX a, float b)
        {
            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] + b;

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator -(VectorX a, float b)
        {
            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] - +b;

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator *(VectorX a, float b)
        {
            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] * b;

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator /(VectorX a, float b)
        {
            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] / b;

            return returnedVector;
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator +(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} with a Vector{b.GetSize()}");

            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] + b[i];

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator -(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} with a Vector{b.GetSize()}");

            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] - b[i];

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator *(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} with a Vector{b.GetSize()}");

            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] * b[i];

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator /(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} with a Vector{b.GetSize()}");

            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] / b[i];

            return returnedVector;
        }

        #region VectorX w/ Vector3

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(VectorX a, Vector3 b)
        {
            if (a.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot add a Vector{a.GetSize()} to a Vector3");

            return new Vector3(a[0] + b.x, a[1] + b.y, a[2] + b.z);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(Vector3 a, VectorX b)
        {
            if (b.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot add a Vector3 to a Vector{b.GetSize()}");

            return new Vector3(a.x + b[0], a.y + b[1], a.z + b[2]);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(VectorX a, Vector3 b)
        {
            if (a.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot subtract a Vector{a.GetSize()} by a Vector3");

            return new Vector3(a[0] - b.x, a[1] - b.y, a[2] - b.z);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 a, VectorX b)
        {
            if (b.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot subtract a Vector3 by a Vector{b.GetSize()}");

            return new Vector3(a.x - b[0], a.y - b[2], a.z - b[2]);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(VectorX a, Vector3 b)
        {
            if (a.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} by a Vector3");

            return new Vector3(a[0] * b.x, a[1] * b.y, a[2] * b.z);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 a, VectorX b)
        {
            if (b.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot multiply a Vector3 by a Vector{b.GetSize()}");

            return new Vector3(a.x * b[0], a.y * b[1], a.z * b[2]);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(VectorX a, Vector3 b)
        {
            if (a.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot divide a Vector{a.GetSize()} by a Vector3");

            return new Vector3(a[0] / b.x, a[1] / b.y, a[2] / b.z);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 a, VectorX b)
        {
            if (b.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot divide a Vector3 by a Vector{b.GetSize()}");

            return new Vector3(a.z / b[0], a.y / b[1], a.z / b[2]);
        }

        #endregion


        public Vector3 ToVector3()
        {
            #if DEBUG
            if (_size != 3)
                throw new System.InvalidCastException($"Cannot convert a VectorX of size {_size} to a Vector3");
            #endif
            return new Vector3(_values[0], _values[1], _values[2]);
        }
    }
    public static class Vector3Extensions
    {
        public static VectorX ToVectorX(this Vector3 vector) => new VectorX(vector);
    }
}