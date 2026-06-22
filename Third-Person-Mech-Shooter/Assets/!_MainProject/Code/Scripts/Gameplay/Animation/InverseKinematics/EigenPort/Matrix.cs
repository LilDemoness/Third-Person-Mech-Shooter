using NUnit.Framework.Internal;

namespace EigenPort
{
    public class Matrix
    {
        protected float[] _values;
        protected int _rowCount;
        protected int _columnCount;

        public Matrix()
        { }
        public Matrix(int rowCount, int columnCount)
        {
            _rowCount = rowCount;
            _columnCount = columnCount;
            _values = new float[rowCount * columnCount];

            SetZeros();
        }
        public Matrix(Matrix other)
        {
            _rowCount = other._rowCount;
            _columnCount = other._columnCount;

            // Arrays are reference types in C#, so to break the reference we need to create a new array and separately copy in the values
            _values = new float[other.GetSize()];
            for (int i = 0; i < other.GetSize(); i++)
                _values[i] = other[i];
        }
        public Matrix(PermutationMatrix other)
            : this(other.GetRowCount(), other.GetColumnCount())
        {
            other.EvalInto(this);
        }

        public float this[int row, int column]
        {
            get => this[row + column * _rowCount];
            set => this[row + column * _rowCount] = value;
        }
        public float this[int index]
        {
            get => _values[index];
            set => _values[index] = value;
        }

        public void Set(Matrix matrix)
        {
            _rowCount = matrix._rowCount;
            _columnCount = matrix._columnCount;

            // Arrays are reference types in C#, so to break the reference we need to create a new array and separately copy in the values
            _values = new float[matrix.GetSize()];
            for (int i = 0; i < matrix.GetSize(); i++)
                _values[i] = matrix[i];
        }

        public void SetBottomRightCorner(Matrix matrix)
        {
            int rowOffset = GetRowCount() - matrix.GetRowCount();
            int columnOffset = GetColumnCount() - matrix.GetColumnCount();

            if (rowOffset < 0 || columnOffset < 0)
                throw new System.ArgumentException($"You are trying to set the bottom right corner of a {GetRowCount()}x{GetColumnCount()} matrix with a {matrix.GetRowCount()}x{matrix.GetColumnCount()} matrix.\nThe latter must be smaller or the same size as the former.");

            for (int row = 0; row < matrix.GetRowCount(); ++row)
                for (int column = 0; column < matrix.GetColumnCount(); ++column)
                    this[row + rowOffset, column + columnOffset] = matrix[row, column];
        }
        public void SetBottomRightCorner(Block block)
        {
            int rowOffset = GetRowCount() - block.GetRowCount();
            int columnOffset = GetColumnCount() - block.GetColumnCount();

            if (rowOffset < 0 || columnOffset < 0)
                throw new System.ArgumentException($"You are trying to set the bottom right corner of a {GetRowCount()}x{GetColumnCount()} matrix with a {block.GetRowCount()}x{block.GetColumnCount()} block.\nThe latter must be smaller or the same size as the former.");

            for (int row = 0; row < block.GetRowCount(); ++row)
                for (int column = 0; column < block.GetColumnCount(); ++column)
                    this[row + rowOffset, column + columnOffset] = block[row, column];
        }
        public Block GetBottomRightCornerRef(int rowCount, int columnCount) => new Block(this, GetRowCount() - rowCount, GetColumnCount() - columnCount, rowCount, columnCount);


        public int GetRowCount() => _rowCount;
        public int GetColumnCount() => _columnCount;
        public int GetSize() => _rowCount * _columnCount;
        public int GetDiagonalSize() => IKMath.Min(_rowCount, _columnCount);

        /// <summary>
        ///     Resizes the matrix to the desired dimensions, if it isn't already the desired size.<br/>
        ///     Sets all values to 0.
        /// </summary>
        /// <param name="rowCount"></param>
        /// <param name="columnCount"></param>
        public virtual void Resize(int rowCount, int columnCount)
        {
            if (_rowCount != rowCount || _columnCount != columnCount)
                // Only resize if we're not the desired size.
            {
                _rowCount = rowCount;
                _columnCount = columnCount;
                _values = new float[_rowCount * _columnCount];
            }

            SetZeros();
        }


        public Vector GetRow(int rowIndex)
        {
            Vector row = new Vector(GetColumnCount());
            for (int i = 0; i < GetColumnCount(); ++i)
                row[i] = this[rowIndex, i];

            return row;
        }
        public VectorBlock GetRowRef(int rowIndex) => VectorBlock.GetForRow(this, rowIndex, 0, GetColumnCount());
        public void SetRow(int rowIndex, Vector newValues)
        {
            if (GetRowCount() >= rowIndex)
                throw new System.IndexOutOfRangeException($"You are trying to assign a value to a row outside of the matrix's range ({rowIndex} vs {GetRowCount()}x{GetColumnCount()})");
            if (newValues.GetSize() != GetColumnCount())
                throw new System.ArgumentException($"You cannot set the values of a row in a {GetRowCount()}x{GetColumnCount()} matrix with a {newValues.GetSize()}d vector.\nUse a {GetColumnCount()}d vector.");

            for (int i = 0; i < GetColumnCount(); ++i)
                this[rowIndex, i] = newValues[i];
        }
        public void SetRowFromLeft(int rowIndex, Vector newValues)
        {
            if (GetRowCount() >= rowIndex)
                throw new System.IndexOutOfRangeException($"You are trying to assign a value to a row outside of the matrix's range ({rowIndex} vs {GetRowCount()}x{GetColumnCount()})");
            if (newValues.GetSize() > GetColumnCount())
                throw new System.ArgumentException($"You are trying to assign more values to a Matrix's row than there are elements in said row (Size: {GetColumnCount()} | Attempt: {newValues.GetSize()}.");

            for (int i = 0; i < newValues.GetSize(); ++i)
                this[rowIndex, i] = newValues[i];
        }
        public void SetRowFromRight(int rowIndex, Vector newValues)
        {
            if (GetRowCount() >= rowIndex)
                throw new System.IndexOutOfRangeException($"You are trying to assign a value to a row outside of the matrix's range ({rowIndex} vs {GetRowCount()}x{GetColumnCount()})");
            if (newValues.GetSize() > GetColumnCount())
                throw new System.ArgumentException($"You are trying to assign more values to a Matrix's row than there are elements in said row (Size: {GetColumnCount()} | Attempt: {newValues.GetSize()}.");

            for (int i = 0; i < newValues.GetSize(); ++i)
                this[rowIndex, GetColumnCount() - i - 1] = newValues[i];
        }

        /// <summary>
        ///     Retrieves a copy of a column from the matrix.
        /// </summary>
        public Vector GetColumn(int columnIndex) => GetColumn(columnIndex, 0, GetRowCount());
        /// <summary>
        ///     Retrieves a copy of a section of a column from the matrix.
        /// </summary>
        public Vector GetColumn(int columnIndex, int startIndex, int blockSize)
        {
            Vector column = new Vector(blockSize);
            for (int i = 0; i < blockSize; ++i)
                column[i] = this[startIndex + i, columnIndex];

            return column;
        }
        public VectorBlock GetColumnRef(int columnIndex) => VectorBlock.GetForColumn(this, columnIndex, 0, GetRowCount());
        public void SetColumn(int columnIndex, Vector newValues)
        {
            if (GetColumnCount() >= columnIndex)
                throw new System.IndexOutOfRangeException($"You are trying to assign a value to a column outside of the matrix's range ({columnIndex} vs {GetRowCount()}x{GetColumnCount()})");
            if (newValues.GetSize() != GetRowCount())
                throw new System.ArgumentException($"You cannot set the values of a coluimn in a {GetRowCount()}x{GetColumnCount()} matrix with a {newValues.GetSize()}d vector.\nUse a {GetRowCount()}d vector.");

            for (int i = 0; i < GetRowCount(); ++i)
                this[i, columnIndex] = newValues[i];
        }
        public void SetColumnFromTop(int columnIndex, Vector newValues)
        {
            if (GetColumnCount() >= columnIndex)
                throw new System.IndexOutOfRangeException($"You are trying to assign a value to a column outside of the matrix's range ({columnIndex} vs {GetRowCount()}x{GetColumnCount()})");
            if (newValues.GetSize() > GetRowCount())
                throw new System.ArgumentException($"You are trying to assign more values to a Matrix's column than there are elements in said column (Size: {GetRowCount()} | Attempt: {newValues.GetSize()}.");

            for (int i = 0; i < newValues.GetSize(); ++i)
                this[i, columnIndex] = newValues[i];
        }
        public void SetColumnFromBottom(int columnIndex, Vector newValues)
        {
            if (GetColumnCount() >= columnIndex)
                throw new System.IndexOutOfRangeException($"You are trying to assign a value to a column outside of the matrix's range ({columnIndex} vs {GetRowCount()}x{GetColumnCount()})");
            if (newValues.GetSize() > GetRowCount())
                throw new System.ArgumentException($"You are trying to assign more values to a Matrix's column than there are elements in said column (Size: {GetRowCount()} | Attempt: {newValues.GetSize()}.");

            for (int i = 0; i < newValues.GetSize(); ++i)
                this[GetRowCount() - i - 1, columnIndex] = newValues[i];
        }


        /// <summary>
        ///     Swaps the position of two elements in the matrix.
        /// </summary>
        public void Swap(int rowA, int columnA, int rowB, int columnB) => Swap(rowA + columnA * _rowCount, rowB + columnB * _rowCount);
        /// <summary>
        ///     Swaps the position of two elements in the matrix.
        /// </summary>
        public void Swap(int indexA, int indexB)
        {
            float temp = this[indexA];
            this[indexA] = this[indexB];
            this[indexB] = temp;
        }
        /// <summary>
        ///     Swaps the coefficients of two rows in the matrix.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="Swap(int, int, int, int)"/> as opposed to <see cref="SetRow(int, Vector)"/>.
        /// </remarks>
        public void SwapRows(int rowAIndex, int rowBIndex)
        {
            for (int column = 0; column < GetColumnCount(); ++column)
                Swap(rowAIndex, column, rowBIndex, column);
        }
        /// <summary>
        ///     Swaps the coefficients of two columns in the matrix.
        /// </summary>
        /// <remarks>
        ///     Uses <see cref="Swap(int, int, int, int)"/> as opposed to <see cref="SetColumn(int, Vector)"/>.
        /// </remarks>
        public void SwapColumns(int columnAIndex, int columnBIndex)
        {
            for (int row = 0; row < GetRowCount(); ++row)
                Swap(row, columnAIndex, row, columnBIndex);
        }


        /// <summary>
        ///     Returns a new Matrix where all values have been Absoluted.<br/>
        ///     Handled via creating a <see cref="CWiseAbsOp"/> with this matrix as the source.
        /// </summary>
        public CWiseAbsOp GetCoefficientWiseAbs() => new CWiseAbsOp(this);

        /// <summary>
        ///     Returns the largest coefficient in the matrix.
        /// </summary>
        public float GetMaxCoefficient() => GetMaxCoefficient(out int _);
        /// <summary>
        ///     Returns the largest coefficient in the matrix.
        /// </summary>
        /// <param name="coefficientRow"> The row index of the largest coefficient in the matrix.</param>
        /// <param name="coefficientColumn"> The column index of the largest coefficient in the matrix.</param>
        public float GetMaxCoefficient(out int coefficientRow, out int coefficientColumn) 
        {
            float maxValue = this[0];
            coefficientRow = 0;
            coefficientColumn = 0;

            for (int row = 0; row < GetRowCount(); ++row)
            {
                for (int column = 0; column < GetColumnCount(); ++column)
                {
                    if (this[row, column] > maxValue)
                    {
                        maxValue = this[row, column];
                        coefficientRow = row;
                        coefficientColumn = column;
                    }
                }
            }

            return maxValue;
        }
        /// <summary>
        ///     Returns the largest coefficient in the matrix.
        /// </summary>
        /// <param name="coefficientIndex"> The index of the largest coefficient in the matrix.</param>
        public float GetMaxCoefficient(out int coefficientIndex)
        {
            float maxValue = this[0];
            coefficientIndex = 0;
            
            for (int i = 1; i < GetSize(); ++i)
            {
                if (this[i] > maxValue)
                {
                    maxValue = this[i];
                    coefficientIndex = i;
                }
            }

            return maxValue;
        }

        /// <summary>
        ///     Returns a new Matrix flipped over the primary diagonal.<br/>
        ///     E.g. [0,1] becomes [1,0]
        /// </summary>
        public Matrix GetTranspose()
        {
            Matrix result = new Matrix(_columnCount, _rowCount);
            for (int row = 0; row < _rowCount; ++row)
                for (int column = 0; column < _columnCount; column++)
                    result[column, row] = this[row, column];

            return result;
        }
        public Matrix GetAdjoint() => GetTranspose();
        public Matrix GetConjugate() => GetTranspose();

        /// <summary>
        ///     Returns a copy of the inverse of this matrix.<br/>
        ///     This matrix MUST be square (Equal number of rows and columns).<br/>
        /// </summary>
        /// <remarks>
        ///     Assumes that this matrix has an inverse.
        ///     Multiplying this matrix by its inverse results in the identity matrix.
        /// </remarks>
        public Matrix GetInverse()
        {
            if (this.GetRowCount() != this.GetColumnCount())
                throw new System.Exception($"You can only get the inverse of square matrices, but you are trying to retrieve it for a {GetRowCount()}x{GetColumnCount()} matrix");

            Matrix result = new Matrix(this);
            
            Matrix lum; // Combined upper and lower;
            int[] perm;
            int toggle = this.DecomposeMatrix(out lum, out perm);

            float[] b = new float[this.GetRowCount()];
            for (int i = 0; i < this.GetRowCount(); ++i)
            {
                for (int j = 0; j < GetRowCount(); ++j)
                    b[j] = i == perm[j] ? 1.0f : 0.0f;
                
                float[] x = Helper(lum, b);
                for (int j = 0; j < GetRowCount(); ++j)
                    result[j, i] = x[j];
            }

            return result;
        }
        private int DecomposeMatrix(out Matrix lum, out int[] perm)
        {
            int toggle = +1; // +1 for Even Row Permutations. -1 for Odd.

            // Copy this matrix into lum.
            lum = new Matrix(this);

            // Make perm[]
            perm = new int[this.GetRowCount()];
            for(int i = 0; i < this.GetRowCount(); ++i)
                perm[i] = i;

            for (int j = 0; j < this.GetRowCount() - 1; ++j)
            {
                float max = IKMath.Abs(lum[j,j]);
                int pivotIndex = j;

                // Find the pivot index.
                for (int i = j + 1; i < this.GetRowCount(); ++i)
                {
                    float xij = IKMath.Abs(lum[i,j]);

                    if (xij > max)
                    {
                        max = xij;
                        pivotIndex  = i;
                    }
                }

                if (pivotIndex != j)
                {
                    // Swap the rows 'j' and 'pivotIndex'
                    lum.SwapRows(j, pivotIndex);

                    // Swap perm elements.
                    int temp = perm[pivotIndex];
                    perm[pivotIndex] = perm[j];
                    perm[j] = temp;

                    toggle = -toggle;
                }

                float xjj = lum[j,j];
                if (xjj != 0.0f)
                {
                    for (int i = j + 1; i < this.GetRowCount(); ++i)
                    {
                        float xij = lum[i, j] / xjj;
                        lum[i, j] = xij;
                        for (int k = j + 1; k < this.GetRowCount(); ++k)
                            lum[i, k] -= xij * lum[j, k];
                    }
                }
            }

            return toggle;
        }
        private float[] Helper(Matrix luMatrix, float[] b)
        {
            float[] x = new float[this.GetRowCount()];
            for (int i = 0; i < this.GetRowCount(); ++i)
                x[i] = b[i];

            for (int i = 1; i < this.GetRowCount(); ++i)
            {
                float sum = x[i];
                for (int j = 0; j < i; ++j)
                    sum -= luMatrix[i, j] * x[j];
                x[i] = sum;
            }

            x[this.GetRowCount() - 1] /= luMatrix[this.GetRowCount() - 1, this.GetRowCount() - 1];
            for (int i = this.GetRowCount() - 2; i >= 0; --i)
            {
                float sum = x[i];
                for (int j = i + 1; j < this.GetRowCount(); ++j)
                    sum -= luMatrix[i, j] * x[j];
                x[i] = sum / luMatrix[i, i];
            }

            return x;
        }

        public Block GetBlock(int startRow, int startColumn, int blockRows, int blockColumns) => new Block(this, startRow, startColumn, blockRows, blockColumns);
        public Diagonal GetDiagonal() => new Diagonal(this);
        public TriangularView GetTriangularView(TriangularViewMode triangularViewMode) => new TriangularView(this, triangularViewMode);


        /// <summary>
        ///     Sets the value of every coefficient to 0.0f.
        /// </summary>
        public void SetZeros()
        {
            for (int i = 0; i < GetSize(); ++i)
                _values[i] = 0.0f;
        }
        /// <summary>
        ///     Sets the value of every coefficient to 1.0f.
        /// </summary>
        public void SetOnes()
        {
            for (int i = 0; i < GetSize(); ++i)
                _values[i] = 1.0f;
        }
        /// <summary>
        ///     Resizes the matrix and makes in an identity matrix.
        /// </summary>
        public void SetIdentity(int rowCount, int columnCount)
        {
            Resize(rowCount, columnCount);
            for (int i = 0; i < GetDiagonalSize(); i++)
                this[i, i] = 1.0f;
        }
        /// <summary>
        ///     Sets the values in the matrix to make it an identity matrix.
        /// </summary>
        public void SetIdentity()
        {
            SetZeros();
            for (int i = 0; i < GetDiagonalSize(); i++)
                this[i, i] = 1.0f;
        }
        public static Matrix GetIdentity(int size)
        {
            Matrix result = new Matrix(size, size);
            result.SetIdentity();
            return result;
        }


        public float magnitude => IKMath.Sqrt(sqrMagnitude);
        public float sqrMagnitude
        {
            get
            {
                float total = 0.0f;
                for (int i = 0; i < GetSize(); ++i)
                    total += this[i] * this[i];
                return total;
            }
        }


        public void ApplyOnTheLeft(int p, int q, JacobiRotation j)
        {
            VectorBlock pVector = GetRowRef(p);
            VectorBlock qVector = GetRowRef(q);
            j.ApplyRotationInThePlane(ref pVector, ref qVector);
        }
        public void ApplyOnTheRight(int p, int q, JacobiRotation j)
        {
            VectorBlock pVector = GetRowRef(p);
            VectorBlock qVector = GetRowRef(q);
            j.GetTransposition().ApplyRotationInThePlane(ref pVector, ref qVector);
        }


        #region Householder Functions

        /// <summary>
        ///     Computes the householder transformation/elementary reflector H such that:
        ///     - H * this = [beta 0 ... 0]^T
        ///     where the Transformation H is:
        ///     - H = I - tau v v^*
        ///     and the vector v is
        ///     - v^T = [1 essential^T]
        /// </summary>
        /// <param name="essentialPart"> The essential part of the vector v.</param>
        /// <param name="tau"> The scaling factor of the Householder transformation.</param>
        /// <param name="beta"> The result of H * this</param>
        public void MakeHouseholder(ref Vector essentialPart, ref float tau, out float beta)
        {
            VectorBlock tail = new VectorBlock(this, 1, GetSize() - 1);

            float tailSqrMagnitude = GetSize() == 1 ? 0.0f : tail.sqrMagnitude;
            float c0 = this[0];

            // We've removed a part of the function which had a check with imaginary numbers.
            // This may need to be inverted (Having the code that was in if the function evaluated true).
            //tau = 0.0f;
            //beta = c0;
            //essential.SetZeros();

            beta = IKMath.Sqrt(IKMath.Abs2(c0) + tailSqrMagnitude);
            if (c0 > 0.0f)
                beta = -beta;
            essentialPart.Set(tail / (c0 - beta));
            tau = (beta - c0) / beta;
        }
        public void MakeHouseholderInPlace(ref float tau, out float beta)
        {
            Vector essentialPart = this.GetColumn(0).GetTail(GetSize() - 1); // Not a reference.
            MakeHouseholder(ref essentialPart, ref tau, out beta);
            this.SetColumnFromBottom(0, essentialPart); // Propogate changes from the essentialPart to this matrix.
        }

        /// <summary>
        ///     Apply the householder transformation/elementary reflector H given by
        ///     - H = I - tau v v^*
        ///     with
        ///     v&T = [1 essential^T]
        ///     from the left to a vector or matrix.
        /// </summary>
        /// <param name="essentialPart"> The essential part of the vector v.</param>
        /// <param name="tau"> The scaling factor of the Householder transformation.</param>
        /// <param name="workspace"> A working space with at last this.GetColumnCount() * <paramref name="essentialPart"/>.GetSize() entries.</param>
        public void ApplyHouseholderOnTheLeft(Vector essentialPart, float tau, ref Vector workspace)
        {
            if (GetRowCount() == 1)
            {
                this.Multiply(1.0f - tau);
                return;
            }
            if (tau == 0.0f)
                return;

            Block bottom = new Block(this, 1, 0, GetRowCount() - 1, GetColumnCount());
            workspace.Set(essentialPart.GetAdjoint() * bottom);
            workspace += this.GetRow(0);
            this.SetRow(0, this.GetRow(0) - (tau * workspace));
            bottom = new Block(bottom - (tau * essentialPart * workspace), 0, 0, bottom.GetRowCount(), bottom.GetColumnCount());
        }
        /// <summary>
        ///     Apply the householder transformation/elementary reflector H given by
        ///     - H = I - tau v v^*
        ///     with
        ///     v&T = [1 essential^T]
        ///     from the right to a vector or matrix.
        /// </summary>
        /// <param name="essentialPart"> The essential part of the vector v.</param>
        /// <param name="tau"> The scaling factor of the Householder transformation.</param>
        /// <param name="workspace"> A working space with at last this.GetColumnCount() * <paramref name="essentialPart"/>.GetSize() entries.</param>
        public void ApplyHouseholderOnTheRight(Vector essentialPart, float tau, ref Vector workspace)
        {
            if (GetColumnCount() == 1)
            {
                this.Multiply(1.0f - tau);
                return;
            }
            if (tau == 0.0f)
                return;

            Block right = new Block(this, 0, 1, GetRowCount(), GetColumnCount() - 1);
            workspace.Set(right * essentialPart.GetConjugate());
            workspace += this.GetColumn(0);
            this.SetColumn(0, this.GetColumn(0) - (tau * workspace));
            right = new Block(right - (tau * workspace * essentialPart.GetTranspose()), 0, 0, right.GetRowCount(), right.GetColumnCount());
        }

        #endregion


        #region Operator Overloading

        public static Matrix operator -(Matrix matrix)
        {
            Matrix result = new Matrix(matrix.GetRowCount(), matrix.GetColumnCount());
            for (int i = 0; i < matrix.GetSize(); i++)
                result[i] = -matrix[i];

            return result;
        }

        #region Matrix

        public static Matrix operator +(Matrix lhs, Matrix rhs)
        {
            if (lhs.GetRowCount() != rhs.GetRowCount() || lhs.GetColumnCount() != rhs.GetRowCount())
                throw new System.ArithmeticException($"You cannot add a {lhs.GetRowCount()}x{rhs.GetColumnCount()} matrix with a {rhs.GetRowCount()}x{rhs.GetColumnCount()} matrix.\nSizes must match.");

            Matrix result = new Matrix(lhs.GetRowCount(), rhs.GetColumnCount());
            for(int i = 0; i < lhs.GetSize(); ++i)
                result[i] = lhs[i] + rhs[i];

            return result;
        }
        public static Matrix operator -(Matrix lhs, Matrix rhs)
        {
            if (lhs.GetRowCount() != rhs.GetRowCount() || lhs.GetColumnCount() != rhs.GetRowCount())
                throw new System.ArithmeticException($"You cannot subtract a {lhs.GetRowCount()}x{rhs.GetColumnCount()} matrix by a {rhs.GetRowCount()}x{rhs.GetColumnCount()} matrix.\nSizes must match.");

            Matrix result = new Matrix(lhs.GetRowCount(), rhs.GetColumnCount());
            for (int i = 0; i < lhs.GetSize(); ++i)
                result[i] = lhs[i] - rhs[i];

            return result;
        }
        public static Matrix operator *(Matrix lhs, Matrix rhs)
        {
            if (lhs.GetColumnCount() != rhs.GetRowCount())
                throw new System.ArithmeticException($"You cannot multiple a {lhs.GetRowCount()}x{rhs.GetColumnCount()} matrix by a {rhs.GetRowCount()}x{rhs.GetColumnCount()} matrix.\nColumn count of left must match Row count of right");

            // Multiply the two matrices together.
            Matrix result = new Matrix(lhs.GetRowCount(), rhs.GetColumnCount());
            for (int row = 0; row < result.GetRowCount(); ++row)
                for (int column = 0; column < result.GetColumnCount(); ++column)
                    for (int i = 0; i < rhs.GetColumnCount(); i++)
                        result[row, column] = lhs[row, i] * rhs[i, column];

            return result;
        }

        #endregion

        #region Float

        public static Matrix operator +(Matrix matrix, float value)
        {
            Matrix result = new Matrix(matrix.GetRowCount(), matrix.GetColumnCount());
            for (int i = 0; i < matrix.GetSize(); ++i)
                result[i] = matrix[i] + value;

            return result;
        }
        public static Matrix operator +(float value, Matrix matrix)
        {
            Matrix result = new Matrix(matrix.GetRowCount(), matrix.GetColumnCount());
            for (int i = 0; i < matrix.GetSize(); ++i)
                result[i] = value + matrix[i];

            return result;
        }

        public static Matrix operator -(Matrix matrix, float value)
        {
            Matrix result = new Matrix(matrix.GetRowCount(), matrix.GetColumnCount());
            for (int i = 0; i < matrix.GetSize(); ++i)
                result[i] = matrix[i] - value;

            return result;
        }
        public static Matrix operator -(float value, Matrix matrix)
        {
            Matrix result = new Matrix(matrix.GetRowCount(), matrix.GetColumnCount());
            for (int i = 0; i < matrix.GetSize(); ++i)
                result[i] = value - matrix[i];

            return result;
        }

        public static Matrix operator *(Matrix matrix, float value)
        {
            Matrix result = new Matrix(matrix.GetRowCount(), matrix.GetColumnCount());
            for (int i = 0; i < matrix.GetSize(); ++i)
                result[i] = matrix[i] * value;

            return result;
        }
        public static Matrix operator *(float value, Matrix matrix)
        {
            Matrix result = new Matrix(matrix.GetRowCount(), matrix.GetColumnCount());
            for (int i = 0; i < matrix.GetSize(); ++i)
                result[i] = value * matrix[i];

            return result;
        }

        public static Matrix operator /(Matrix matrix, float value)
        {
            Matrix result = new Matrix(matrix.GetRowCount(), matrix.GetColumnCount());
            for (int i = 0; i < matrix.GetSize(); ++i)
                result[i] = matrix[i] / value;

            return result;
        }
        public static Matrix operator /(float value, Matrix matrix)
        {
            Matrix result = new Matrix(matrix.GetRowCount(), matrix.GetColumnCount());
            for (int i = 0; i < matrix.GetSize(); ++i)
                result[i] = value / matrix[i];

            return result;
        }

        #endregion

        #endregion

        #region Arithmetic Assignment Replacements

        // We cannot overload the arithmetic assignment operators (*=, +=, etc) in the version of C# Unity uses.
        // Instead, we'll use these functions.

        public void Add(float valueToAdd)
        {
            for (int i = 0; i < GetSize(); ++i)
                this[i] += valueToAdd;
        }
        public void Subtract(float valueToSubtract)
        {
            for (int i = 0; i < GetSize(); ++i)
                this[i] -= valueToSubtract;
        }
        public void Multiply(float valueToMultiplyBy)
        {
            for (int i = 0; i < GetSize(); ++i)
                this[i] *= valueToMultiplyBy;
        }
        public void Devide(float valueToDivideBy)
        {
            for (int i = 0; i < GetSize(); ++i)
                this[i] /= valueToDivideBy;
        }

        #endregion
    }
}