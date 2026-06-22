namespace EigenPort
{
    /// <summary>
    ///     A block containing the reference to a sub-area within an existing vector/matrixmatrix.
    ///     Modifying values in the block propogates the changes to the source vector/matrix.
    /// </summary>
    public class VectorBlock : Block
    {
        private bool _isVertical => _blockRows > 1; // If true, then the values are stored vertically. Otherwise, values are stored horizontally.

        public VectorBlock(Matrix source, int startIndex, int size)
            : base(source, startIndex, 0, size, 1)
        { }
        public VectorBlock(Vector source, int startIndex, int size)
            : base(source, startIndex, 0, size, 1)
        { }

        private VectorBlock(Matrix source, int rowIndex, int columnIndex, int rowSize, int columnSize)
            : base(source, rowIndex, columnIndex, rowSize, columnSize)
        { }
        public static VectorBlock GetForRow(Matrix source, int rowIndex, int startIndex, int blockSize)
            => new VectorBlock(source, rowIndex, startIndex, 1, blockSize);
        public static VectorBlock GetForColumn(Matrix source, int columnIndex, int startIndex, int blockSize)
            => new VectorBlock(source, startIndex, columnIndex, blockSize, 1);


        public float this[int index]
        {
            get => this[index, 0];
            set => this[index, 0] = value;
        }


        public VectorBlock GetTailRef(int size)
        {
            if (_isVertical)
                UnityEngine.Debug.Log($"Vertical\nStart Row: {_startRow}\nBlock Rows: {_blockRows}\nDesired Size: {size}\nTail Start: {_startRow + (_blockRows - size)}\nTail End{(_startRow + (_blockRows - size)) + size}");
            else
                UnityEngine.Debug.Log($"Horizontal\nStart Row: {_startColumn}\nBlock Rows: {_blockColumns}\nDesired Size: {size}\nTail Start: {_startColumn + (_blockColumns - size)}\nTail End{(_startColumn + (_blockColumns - size)) + size}");

            // If vertical, create a vertical block of the desired size & start index from this's end row index. If horizontal, do so for columns.
            return _isVertical
                ? new VectorBlock(_source, _startRow + (_blockRows - size), _startColumn, size, 1)
                : new VectorBlock(_source, _startRow, _startColumn + (_blockColumns - size), 1, size);
        }
    }
}