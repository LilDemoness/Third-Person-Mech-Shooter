using UnityEngine;

namespace Gameplay.Animations
{
    public struct MatrixX : IMatrixBase
    {
        private float[] _values;
        private int _rowCount, _columnCount;
        private int _totalCount => _rowCount * _columnCount;

        public MatrixX(int rowCount, int columnCount)
        {
            _rowCount = rowCount;
            _columnCount = columnCount;
            _values = new float[rowCount * columnCount];

            SetZeros();
        }
        public MatrixX(MatrixX other)
        {
            _rowCount = other._rowCount;
            _columnCount = other._columnCount;
            _values = other._values;
        }
        public MatrixX(MatrixX other, int rowStart, int columnStart, int rowCount, int columnCount)
        {
            if (rowStart + rowCount > other.GetRowCount())
                throw new System.ArgumentException($"Row Indicies (Start {rowStart}, End {rowStart + rowCount} exceeds the row count of the passed matrix ({other.GetRowCount()})");
            if (columnStart + columnCount > other.GetColumnCount())
                throw new System.ArgumentException($"Column Indicies (Start {columnStart}, End {columnStart + columnCount} exceeds the column count of the passed matrix ({other.GetColumnCount()})");


            _rowCount = rowCount;
            _columnCount = columnCount;
            _values = new float[rowCount * columnCount];

            for(int row = 0; row < rowCount; ++row)
                for(int column = 0; column < columnCount; ++column)
                    this[row, column] = other[row + rowStart, column + columnStart];
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
        public float GetCoefficient(int row, int column) => this[row, column];


        public void Resize(int rowCount, int columnCount)
        {
            if (_rowCount == rowCount && _columnCount == columnCount)
                return; // Already the correct size.

            _rowCount = rowCount;
            _columnCount = columnCount;
            _values = new float[_totalCount];
        }
        public void SetZeros()
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


        public void ApplyHouseholderOnTheLeft(VectorX essential, float tau, ref VectorX workspace);
        /// <summary>
        ///     Applies the householder transformation (Alt. Elementary reflector) given by:
        ///     - H = I - tau * v * v.GetConjugate()
        ///     with
        ///     - 
        /// </summary>
        /// <param name="essential"> The essential part of the vector v.</param>
        /// <param name="tau"> The scaling factor of the householder transformation.</param>
        /// <param name="workspace"> A reference to a working space with at least this.GetColumnCount() * essential.GetSize()</param>
        public void ApplyHouseholderOnTheRight(VectorX essential, float tau, ref VectorX workspace)
        {
            if (GetColumnCount() == 1)
                this *= 1.0f - tau;
            else if (tau != 0.0f)
            {
                MatrixX right = new MatrixX(this, 0, 1, GetRowCount(), GetColumnCount() - 1);
                workspace = right * essential.GetConjugate();
                workspace += this.GetColumn(0);
                this.SetColumn(0, this.GetColumn(0) - workspace * tau);
                right -= essential.GetTransposition() * (workspace * tau);
                this.SetSubmatrix(right, 0, 1);
            }
        }

        public void ApplyOnTheLeft(int p, int q, JacobiRotation j);
        public void ApplyOnTheRight(int p, int q, JacobiRotation j);


        #region Operator Overloading

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

        #endregion


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
}