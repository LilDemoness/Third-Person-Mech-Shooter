namespace EigenPort
{
    /// <summary>
    ///     A block containing the reference to a sub-area within an existing matrix.
    ///     Modifying values in the block propogates the changes to the source matrix.
    /// </summary>
    public class Block
    {
        protected Matrix _source;
        protected int _startRow;
        protected int _startColumn;
        protected int _blockRows;
        protected int _blockColumns;

        public Block(Matrix source, int startRow, int startColumn, int blockRows, int blockColumns)
        {
            // Index Checks.
            if (startRow + blockRows > source.GetRowCount())
                throw new System.IndexOutOfRangeException($"You are trying to retrieve more rows than there are in the source matrix.\nStart Row '{startRow}' + Size {blockRows} = '{startRow + blockRows}', exceeding Source Row Count '{source.GetRowCount()}'");
            if (startColumn + blockColumns > source.GetColumnCount())
                throw new System.IndexOutOfRangeException($"You are trying to retrieve more rows than there are in the source matrix.\nStart Row '{startColumn}' + Size {blockColumns} = '{startColumn + blockColumns}', exceeding Source Row Count '{source.GetColumnCount()}'");

            // Set block values.
            _source = source;
            _startRow = startRow;
            _startColumn = startColumn;
            _blockRows = blockRows;
            _blockColumns = blockColumns;
        }

        public float this[int row, int column]
        {
            get
            {
                if (row > _blockRows) throw new System.IndexOutOfRangeException($"Row Index ({row}) exceeds the row-size of the block {_blockRows}");
                if (column > _blockColumns) throw new System.IndexOutOfRangeException($"Column Index ({column}) exceeds the column-size of the block {_blockColumns}");
                return _source[row + _startRow, column + _startColumn];
            }
            set
            {
                if (row > _blockRows) throw new System.IndexOutOfRangeException($"Row Index ({row}) exceeds the row-size of the block {_blockRows}");
                if (column > _blockColumns) throw new System.IndexOutOfRangeException($"Column Index ({column}) exceeds the column-size of the block {_blockColumns}");
                _source[row + _startRow, column + _startColumn] = value; 
            }
        }


        public int GetRowCount() => _blockRows;
        public int GetColumnCount() => _blockColumns;
        public int GetSize() => GetRowCount() * GetColumnCount();

        public float magnitude => GetEffectiveMatrix().magnitude;
        public float sqrMagnitude => GetEffectiveMatrix().sqrMagnitude;

        public float GetMaxCoefficient() => GetEffectiveMatrix().GetMaxCoefficient();
        public float GetMaxCoefficient(out int index) => GetEffectiveMatrix().GetMaxCoefficient(out index);


        public TriangularView GetTriangularView(TriangularViewMode triangularViewMode) => GetEffectiveMatrix().GetTriangularView(triangularViewMode);
        public Matrix GetTranspose() => _source.GetTranspose();


        /// <summary>
        ///     Returns a copy of the effective matrix that this block represents.
        /// </summary>
        public Matrix GetEffectiveMatrix()
        {
            Matrix effectiveSource = new Matrix(_blockRows, _blockColumns);
            for (int row = 0; row < GetRowCount(); ++row)
                for (int column = 0; column < GetColumnCount(); ++column)
                    effectiveSource[row, column] = _source[row + _startRow, column + _startColumn];

            return effectiveSource;
        }

        /// <inheritdoc cref="Matrix.MakeHouseholderInPlace(ref float, out float)"/>
        public void MakeHouseholderInPlace(ref float tau, out float beta) => _source.MakeHouseholderInPlace(ref tau, out beta);
        public void ApplyHouseholderOnTheLeft(Vector essentialPart, float tau, ref Vector workspace) => _source.ApplyHouseholderOnTheLeft(essentialPart, tau, ref workspace);
        public void ApplyHouseholderOnTheRight(Vector essentialPart, float tau, ref Vector workspace) => _source.ApplyHouseholderOnTheRight(essentialPart, tau, ref workspace);


        #region Operator Overloading

        #region Block x Matrix / Matrix x Block / Block x Block

        public static Matrix operator +(Matrix lhs, Block rhs) => lhs + rhs.GetEffectiveMatrix();
        public static Matrix operator +(Block lhs, Matrix rhs) => lhs.GetEffectiveMatrix() + rhs;
        public static Matrix operator +(Block lhs, Block rhs) => lhs.GetEffectiveMatrix() + rhs.GetEffectiveMatrix();

        public static Matrix operator -(Matrix lhs, Block rhs) => lhs - rhs.GetEffectiveMatrix();
        public static Matrix operator -(Block lhs, Matrix rhs) => lhs.GetEffectiveMatrix() - rhs;
        public static Matrix operator -(Block lhs, Block rhs) => lhs.GetEffectiveMatrix() - rhs.GetEffectiveMatrix();

        public static Matrix operator *(Matrix lhs, Block rhs) => lhs * rhs.GetEffectiveMatrix();
        public static Matrix operator *(Block lhs, Matrix rhs) => lhs.GetEffectiveMatrix() * rhs;
        public static Matrix operator *(Block lhs, Block rhs) => lhs.GetEffectiveMatrix() * rhs.GetEffectiveMatrix();

        #endregion

        #region Block x float

        public static Matrix operator +(Block lhs, float rhs) => lhs.GetEffectiveMatrix() + rhs;
        public static Matrix operator -(Block lhs, float rhs) => lhs.GetEffectiveMatrix() - rhs;
        public static Matrix operator *(Block lhs, float rhs) => lhs.GetEffectiveMatrix() * rhs;
        public static Matrix operator /(Block lhs, float rhs) => lhs.GetEffectiveMatrix() / rhs;

        #endregion

        #endregion
    }
}