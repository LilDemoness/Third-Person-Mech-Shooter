namespace EigenPort
{
    public class PermutationMatrix
    {
        private int[] _indicies;


        public PermutationMatrix(int size)
        {
            _indicies = new int[size];
        }


        /// <summary>
        ///     Returns the number of rows.
        /// </summary>
        public int GetRowCount() => _indicies.Length;
        /// <summary>
        ///     Returns the number of columns.
        /// </summary>
        public int GetColumnCount() => _indicies.Length;
        /// <summary>
        ///     Returns the size of a side of the respective square matrix.
        /// </summary>
        public int GetSize() => _indicies.Length;


        /// <summary>
        ///     Resizes to the given size.
        /// </summary>
        public void Resize(int newSize) => System.Array.Resize<int>(ref _indicies, newSize);

        
        /// <summary>
        ///     Sets this to be the identity permutation matrix.
        /// </summary>
        public void SetIdentity()
        {
            for (int i = 0; i < GetSize(); ++i)
                _indicies[i] = i;
        }
        /// <summary>
        ///     Sets this to be the identity permutation matrix of the given size.
        /// </summary>
        public void SetIdentity(int newSize)
        {
            Resize(newSize);
            SetIdentity();
        }


        public void ApplyTranspositionOnTheRight(int i, int j)
        {
            if (i < 0 || j < 0 || i >= GetSize() || j >= GetSize())
                throw new System.IndexOutOfRangeException($"The value of i ({i}) or j ({j}) exceeds the size of the permutation matrix ({GetSize()})");

            var temp = _indicies[i];
            _indicies[i] = _indicies[j];
            _indicies[j] = temp;
        }


        public void EvalInto(Matrix matrix)
        {
            matrix.SetZeros();
            for (int i = 0; i < GetRowCount(); ++i)
                matrix[_indicies[i], i] = 1.0f;
        }
    }
}