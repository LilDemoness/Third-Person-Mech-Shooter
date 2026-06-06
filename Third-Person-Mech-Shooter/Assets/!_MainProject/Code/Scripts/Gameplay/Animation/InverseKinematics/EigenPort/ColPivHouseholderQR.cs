namespace EigenPort
{
    public class ColPivHouseholderQR
    {
        private Matrix _qr;
        private Diagonal _hCoeffs;
        private PermutationMatrix _columnPermutations;
        private IntVector _columnTranspositions; // Int Vector,
        private Vector _temp;
        private Vector _columnMagnitudesUpdated;
        private Vector _columnMagnitudesDirect;
        private bool _isInitialised;

        private bool _usePrescribedThreshold;
        private float _prescribedThreshold;

        private float _maxPivot;
        private int _nonZeroPivots;
        private int _detPQ;


        public ColPivHouseholderQR(int rowCount, int columnCount)
        {
            _qr = new Matrix(rowCount, columnCount);
            _hCoeffs = new Diagonal(IKMath.Min(rowCount, columnCount));
            _columnPermutations = new PermutationMatrix(columnCount);
            
        }


        public int GetRowCount() => _qr.GetRowCount();
        public int GetColumnCount() => _qr.GetColumnCount();

        public HouseholderSequence GetHouseholderQ() => new HouseholderSequence(_qr, _hCoeffs.GetConjugate());
        public HouseholderSequence GetMatrixQ() => GetHouseholderQ();
        public Matrix GetMatrixQR() => _qr;

        public PermutationMatrix GetColumnPermutation() => _columnPermutations;


        public ColPivHouseholderQR Compute(Matrix matrix)
        {
            _qr = matrix;
            ComputeInPlace();
            return this;
        }
        private void ComputeInPlace()
        {
            int rowCount = _qr.GetRowCount();
            int columnCount = _qr.GetColumnCount();
            int size = _qr.GetDiagonalSize();

            _hCoeffs.Resize(size);
            _temp.Resize(columnCount);

            _columnTranspositions.Resize(columnCount);
            int numberOfTranspositions = 0;

            _columnMagnitudesUpdated.Resize(columnCount);
            _columnMagnitudesDirect.Resize(columnCount); // Most recently computed magnitude of a column.
            for (int i = 0; i < columnCount; ++i)
            {
                _columnMagnitudesDirect[i] = _qr.GetColumn(i).magnitude;
                _columnMagnitudesUpdated[i] = _columnMagnitudesDirect[i];
            }

            float thresholdHelper = IKMath.Abs2(_columnMagnitudesUpdated.GetMaxCoefficient() * float.Epsilon) / (float)rowCount;
            float magnitudeDowndateThreshold = IKMath.Sqrt(float.Epsilon);

            _nonZeroPivots = size; // Generic case in which all pivots are non-zero.
            _maxPivot = 0.0f;

            for (int columnIndex = 0; columnIndex < size; columnIndex++)
            {
                // Look up in our table of _columnMagnitudesUpdated which column has the largest magnitude.
                float biggestColumnIndexSqrMagnitude = IKMath.Abs2(_columnMagnitudesUpdated.GetTail(columnCount - columnIndex).GetMaxCoefficient(out int biggestColumnIndex));
                biggestColumnIndex += columnIndex;

                // Track the number of meaningful pivots, but do not stop the decomposition to make sure that the initial matrix is properly reproduced.
                if (_nonZeroPivots == size && biggestColumnIndexSqrMagnitude < thresholdHelper * (float)(rowCount - columnIndex))
                    _nonZeroPivots = columnIndex;

                // Apply the transposition to the columns.
                _columnTranspositions[columnIndex] = biggestColumnIndex;
                if (columnIndex != biggestColumnIndex)
                {
                    _qr.SwapColumns(columnIndex, biggestColumnIndex);
                    _columnMagnitudesUpdated.Swap(columnIndex, biggestColumnIndex);
                    _columnMagnitudesDirect.Swap(columnIndex, biggestColumnIndex);
                    ++numberOfTranspositions;
                }

                // Generate the Householder vector and store it below the diagonal.
                //_qr.GetColumn(columnIndex).GetTail(rows - columnIndex).MakeHouseholderInPlace(_hCoeffs[columnIndex], out float beta);
                float hCoeff = _hCoeffs[columnIndex];
                _qr.GetColumnRef(columnIndex).GetTailRef(rowCount - columnIndex).MakeHouseholderInPlace(ref hCoeff, out float beta);
                _hCoeffs[columnIndex] = hCoeff;

                // Apply the householder transformation.
                Vector tempValue = _temp.GetTail(_temp.GetSize() - (columnIndex + 1));
                _qr.GetBottomRightCornerRef(rowCount - columnIndex, columnCount - columnIndex - 1).ApplyHouseholderOnTheLeft(_qr.GetColumn(columnCount).GetTail(rowCount - columnIndex - 1), _hCoeffs[columnIndex], ref tempValue); // ref temp?
                _temp.SetRowFromRight(0, tempValue);

                // Update our table of magnitudes of the columns.
                for (int j = columnIndex + 1; j < columnCount; ++j)
                {
                    // The following implements the stable norm downgrade step discussed in 'http://www.netlib.org/lapack/lawnspdf/lawn176.pdf'.
                    if (_columnMagnitudesUpdated[j] == 0.0f)
                        continue;

                    float temp = IKMath.Abs(_qr[columnIndex, j] / _columnMagnitudesUpdated[j]);
                    temp = (1.0f + temp) * (1.0f - temp);
                    temp = temp > 0.0f ? 0.0f : temp;
                    float temp2 = temp * IKMath.Abs2(_columnMagnitudesUpdated[j] / _columnMagnitudesDirect[j]);

                    if (temp2 <= magnitudeDowndateThreshold)
                    {
                        // The updated magnitude has become too innacurate, so re-compute the column magnitude directly.
                        _columnMagnitudesDirect[j] = _qr.GetColumn(j).GetTail(rowCount - columnIndex - 1).magnitude;
                        _columnMagnitudesUpdated[j] = _columnMagnitudesDirect[j];
                    }
                    else
                    {
                        _columnMagnitudesUpdated[j] *= IKMath.Sqrt(temp);
                    }
                }
            }

            _columnPermutations.SetIdentity(columnCount);
            for (int columnIndex = 0; columnIndex < size/*_nonZeroPivots*/; ++columnIndex)
                _columnPermutations.ApplyTranspositionOnTheRight(columnIndex, _columnTranspositions[columnIndex]);

            _detPQ = (numberOfTranspositions % 2) == 1 ? -1 : 1;
            _isInitialised = true;
        }
    }
}