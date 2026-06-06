namespace EigenPort
{
    public class TriangularView : Matrix
    {
        public TriangularView(Matrix source, TriangularViewMode mode)
            : base(source.GetRowCount(), source.GetColumnCount())
        {
            switch(mode)
            {
                case TriangularViewMode.Upper:          Initialise_Upper(source);   break;
                case TriangularViewMode.UnitUpper:      Initialise_UnitUpper(source);   break;
                case TriangularViewMode.StrictlyUpper:  Initialise_StrictlyUpper(source);   break;

                case TriangularViewMode.Lower:          Initialise_Lower(source);   break;
                case TriangularViewMode.UnitLower:      Initialise_UnitLower(source);   break;
                case TriangularViewMode.StrictlyLower:  Initialise_StrictlyLower(source);   break;

                default: throw new System.NotImplementedException();
            }
        }

        #region Initialisation Methods

        // Note: All initialisation methods assume that the values in the matrix are 0, such as when the matrix is created.

        private void Initialise_Upper(Matrix source)
        {
            // Only assign the elements that are on or above the diagonal.
            // (Exclude below).
            for (int row = 0; row < GetRowCount(); ++row)
                for (int column = 0; column < GetColumnCount(); ++column)
                    if (column >= row)
                        this[row, column] = source[row, column];
        }
        private void Initialise_UnitUpper(Matrix source)
        {
            // Only assign the elements that are on or above the diagonal, and make the diagonal values 1.
            // (Exclude below, Unit Diagonal).
            for (int row = 0; row < GetRowCount(); ++row)
            {
                for (int column = 0; column < GetColumnCount(); ++column)
                {
                    if (column > row)
                    {
                        this[row, column] = source[row, column];
                    }
                    else if (column == row)
                    {
                        this[row, column] = 1.0f;
                    }
                }
            }
        }
        private void Initialise_StrictlyUpper(Matrix source)
        {
            // Only assign the elements that are above the diagonal.
            // (Exclude below and diagonal).
            for (int row = 0; row < GetRowCount(); ++row)
                for (int column = 0; column < GetColumnCount(); ++column)
                    if (column > row)
                        this[row, column] = source[row, column];
        }



        private void Initialise_Lower(Matrix source)
        {
            // Only assign the elements that are on or below the diagonal.
            // (Exclude above).
            for (int row = 0; row < GetRowCount(); ++row)
                for (int column = 0; column < GetColumnCount(); ++column)
                    if (row >= column)
                        this[row, column] = source[row, column];
        }
        private void Initialise_UnitLower(Matrix source)
        {
            // Only assign the elements that are on or below the diagonal, and make the diagonal values 1.
            // (Exclude above, Unit Diagonal).
            for (int row = 0; row < GetRowCount(); ++row)
            {
                for (int column = 0; column < GetColumnCount(); ++column)
                {
                    if (row > column)
                    {
                        this[row, column] = source[row, column];
                    }
                    else if (row == column)
                    {
                        this[row, column] = 1.0f;
                    }
                }
            }
        }
        private void Initialise_StrictlyLower(Matrix source)
        {
            // Only assign the elements that are below the diagonal.
            // (Exclude above and diagonal).
            for (int row = 0; row < GetRowCount(); ++row)
                for (int column = 0; column < GetColumnCount(); ++column)
                    if (row > column)
                        this[row, column] = source[row, column];
        }

        #endregion
    }

    public enum TriangularViewMode
    {
        /// <summary> All elements below the diagonal are 0.</summary>
        Upper,
        /// <summary> All elements above the diagonal are 0.</summary>
        Lower,
        /// <summary> All elements below the diagonal are 0, and the diagonal is all 1.</summary>
        UnitUpper,
        /// <summary> All elements above the diagonal are 0, and the diagonal is all 1.</summary>
        UnitLower,
        /// <summary> All elements below and including the diagonal are 0.</summary>
        StrictlyUpper,
        /// <summary> All elements above and including the diagonal are 0.</summary>
        StrictlyLower
    }

}