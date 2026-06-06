namespace EigenPort
{
    /// <summary>
    ///     Expression of a diagonal/subdiagonal/superdiagonal in a matrix.
    /// </summary>
    public class Diagonal : Matrix
    {
        /// <summary>
        ///     Creates a new Diagonal from a source matrix.<br/>
        ///     
        ///     The matrix is not required to be square.
        /// </summary>
        public Diagonal(Matrix source)
            : base(source.GetRowCount(), source.GetColumnCount())
        {
            for (int i = 0; i < source.GetDiagonalSize(); ++i)
                this[i, i] = source[i, i];
        }
        public Diagonal(int size)
            : base(size, size)
        { }


        public override void Resize(int rowCount, int columnSize) => throw new System.Exception("You cannot call Resize(int, int) on a Diagonal. Use Resize(int) instead");
        public void Resize(int newSize) => base.Resize(newSize, newSize);
    }
}