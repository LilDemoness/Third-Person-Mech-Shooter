namespace EigenPort
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///     Blender Source Code Link: .
    /// </remarks>
    public class JacobiSVD
    {
        [System.Serializable, System.Flags]
        public enum ComputationOptions
        {
            None = 0,

            ComputeFullU = 1 << 0,
            ComputeFullV = 1 << 1,
            ComputeThinU = 1 << 2,
            ComputeThinV = 1 << 3,
        }
        private bool _isInitialised;
        private bool _isAllocated;


        private int _rowCount;
        private int _columnCount;
        private int _diagonalSize;
        private int _nonZeroSingularValues;


        private ComputationOptions _computationOptions;
        private bool _computeFullU;
        private bool _computeFullV;
        private bool _computeThinU;
        private bool _computeThinV;


        private Matrix _matrixU;
        private Matrix _matrixV;
        private Matrix _scaledMatrix;
        private Matrix _workMatrix;
        private Vector _singularValues;

        private ColPivHouseholderQRPrecondition_MoreRows _qrPreconditionMoreRows;
        private ColPivHouseholderQRPrecondition_MoreColumns _qrPreconditionMoreColumns;

        public int GetRowCount() => _rowCount;
        public int GetColumnCount() => _columnCount;

        public Matrix GetMatrixU() => _matrixU;
        public Matrix GetMatrixV() => _matrixV;
        public Vector GetSingularValues() => _singularValues;



        public JacobiSVD(Matrix matrix, ComputationOptions computationOptions = ComputationOptions.None)
        {
            Compute(matrix, computationOptions);
        }

        public void Allocate(int rowCount, int columnCount, ComputationOptions computationOptions)
        {
            if (_isAllocated && _rowCount == rowCount && _columnCount == columnCount && _computationOptions == computationOptions)
                return; // Already allocated for the passed parameters.

            _isInitialised = false;
            _isAllocated = true;

            
            _rowCount = rowCount;
            _columnCount = columnCount;


            _computationOptions = computationOptions;
            _computeFullU = (computationOptions & ComputationOptions.ComputeFullU) != 0;
            _computeFullV = (computationOptions & ComputationOptions.ComputeFullV) != 0;
            _computeThinU = (computationOptions & ComputationOptions.ComputeThinU) != 0;
            _computeThinV = (computationOptions & ComputationOptions.ComputeThinV) != 0;
            UnityEngine.Debug.Assert(!(_computeFullU && _computeThinU), "You cannot ask for both Full and Thin U");
            UnityEngine.Debug.Assert(!(_computeFullV && _computeThinV), "You cannot ask for both Full and Thin V");


            _diagonalSize = IKMath.Min(_rowCount, _columnCount);
            _singularValues.Resize(_diagonalSize);
            _matrixU.Resize(_rowCount, _computeFullU ? _rowCount : (_computeThinU ? _diagonalSize : 0));
            _matrixV.Resize(_rowCount, _computeFullV ? _rowCount : (_computeThinV ? _diagonalSize : 0));
            _workMatrix.Resize(_diagonalSize, _diagonalSize);

            if (_rowCount > _columnCount) _qrPreconditionMoreRows.Allocate(this);
            if (_columnCount > _rowCount) _qrPreconditionMoreRows.Allocate(this);
            if (_rowCount != _columnCount) _scaledMatrix.Resize(_rowCount, _columnCount);
        }


        /// <summary>
        ///     Performs the decomposition of the given matrix using custom options
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="computationOptions"></param>
        /// <remarks>
        ///     Thin unitaries are only available if your matrix type has a dynamic number of columns (For example, MatrixXf).
        ///     They are also not available with the (non-default) FullPivHouseholderQR preconditioner.
        ///     Our implementation *should* feature neither of these.
        /// </remarks>
        public JacobiSVD Compute(Matrix matrix, ComputationOptions computationOptions)
        {
            Allocate(matrix.GetRowCount(), matrix.GetColumnCount(), computationOptions);

            // Currently we stop when we reach precision (2*epsilon) as the last bit of precision can require an unreasonable
            // number of iterations, only worsening the precision of U and V as we accumulate more rotations.
            const float precision = 2.0f * float.Epsilon;

            // Limit for denormal numbers to be considered zero in order to avoid infinite loops.
            const float considerAsZero = float.Epsilon;

            // Scaling factor to reduce overflows/underflows.
            float scale = matrix.GetCoefficientWiseAbs().GetMaxCoefficient();


            // Step 1m The R-SVD step: We use a QR decomposition to reduce to the case of a square matrix.
            if (_rowCount != _columnCount)
            {
                _scaledMatrix = matrix / scale;
                _qrPreconditionMoreColumns.Run(this, _scaledMatrix);
                _qrPreconditionMoreRows.Run(this, _scaledMatrix);
            }
            else
            {
                _workMatrix = matrix.GetBlock(0, 0, _diagonalSize, _diagonalSize) / scale;
                if (_computeFullU) _matrixU.SetIdentity(_rowCount, _rowCount);
                if (_computeFullV) _matrixV.SetIdentity(_columnCount, _columnCount);
                if (_computeThinU) _matrixU.SetIdentity(_rowCount, _diagonalSize);
                if (_computeThinV) _matrixV.SetIdentity(_columnCount, _diagonalSize);
            }


            // Step 2. The main Jacobi SVD iteration.
            float maxDiagonalEntry = _workMatrix.GetCoefficientWiseAbs().GetDiagonal().GetMaxCoefficient();

            bool finished = false;
            while(!finished)
            {
                finished = true;

                // Do a sweep: For all index pairs (p,q), perform a SVD of the corresponding 2x2 sub-matrix.
                for (int p = 0; p < _diagonalSize; ++p)
                {
                    for (int q = 0; q < p; ++q)
                    {
                        // If the 2x2 sub-matrix is not diagonal already...
                        // Notice that this comparison will evaluate to false if any NaN is involved,
                        // ensuring that NaN's don't keep us iterating forever
                        // Similarly, small denormal numbers are considered zero.
                        float threshold = IKMath.Max(considerAsZero, precision * maxDiagonalEntry);
                        if (IKMath.Abs(_workMatrix[p, q]) <= threshold || IKMath.Abs(_workMatrix[q, p]) <= threshold)
                            continue; // Values are approx. zero.
                        
                        // Outside our threshold.
                        finished = false;

                        // Ensure that the 2x2 sub-matrix is comprised of only real numbers.
                        // This always evaluates to true in our implementation as floats & ints are always real in C#, so we're skipping the implemenation.
                        /*if (RunSVDPrecondition2x2BlockToBeReal())*/
                        //  continue; // Non-real and our precondition couldn't convert to real.

                        // Perform SVD decomposition of the 2x2 sub-matrix corresponding to the indicies p, q to make it diagonal.
                        Real2x2JacobiSvd(_workMatrix, p, q, out JacobiRotation jLeft, out JacobiRotation jRight);

                        // Accumulate resulting jacobi rotation.
                        _workMatrix.ApplyOnTheLeft(p, q, jLeft);
                        if (ShouldComputeU()) { _matrixU.ApplyOnTheRight(p, q, jLeft.GetTransposition()); }

                        _workMatrix.ApplyOnTheRight(p, q, jRight);
                        if (ShouldComputeV()) { _matrixU.ApplyOnTheRight(p, q, jRight); }

                        // Keep track of the largest diagonal coefficient for our threshold.
                        maxDiagonalEntry = IKMath.Max(maxDiagonalEntry, IKMath.Abs(_workMatrix[p,p]), IKMath.Abs(_workMatrix[q,q]));
                    }
                }
            }


            // Step 3. The work matrix is now diagonal, so ensure that it is positive so that its diagonal entries are the singular values.
            for (int i = 0; i < _diagonalSize; ++i)
            {
                float a = _workMatrix[i, i];
                _singularValues[i] = IKMath.Abs(a);

                if (ShouldComputeU() && a < 0.0f)
                    _matrixU.SetColumn(i, -_matrixU.GetColumn(i));
            }

            _singularValues *= scale;


            // Step 4. Sort singular values in descending order and compute the number of nonzero singular values.
            _nonZeroSingularValues = _diagonalSize;
            for (int i = 0; i < _diagonalSize; ++i)
            {
                float maxRemainingSingularValue = _singularValues.GetTail(_diagonalSize - i).GetMaxCoefficient(out int pos);

                if (maxRemainingSingularValue == 0.0f)
                {
                    _nonZeroSingularValues = i;
                    break;
                }

                if (pos != 0)
                {
                    pos += i;
                    _singularValues.Swap(i, pos);
                    if(ShouldComputeU()) {  _matrixU.SwapColumns(pos, i); }
                    if(ShouldComputeV()) {  _matrixV.SwapColumns(pos, i); }
                }
            }

            _isInitialised = true;
            return this;
        }


        public bool ShouldComputeU() => _computeFullU || _computeThinU;
        public bool ShouldComputeV() => _computeFullV || _computeThinV;


        private void Real2x2JacobiSvd(Matrix workMatrix, int p, int q, out JacobiRotation jLeft, out JacobiRotation jRight)
        {
            Matrix m = new Matrix(2, 2);
            m[0,0] = workMatrix[p, p]; m[0, 1] = workMatrix[p, q];
            m[1,0] = workMatrix[q, p]; m[1, 1] = workMatrix[q, q];

            JacobiRotation rot1;
            float t = m[0,0] - m[1,1];
            float d = m[1,0] - m[0,1];

            if (IKMath.Abs(d) < float.Epsilon)
            {
                rot1 = new JacobiRotation(1.0f, 0.0f);
            }
            else
            {
                // If d != 0, then t/d cannot overflow as the magnitude of the
                // entries forming d are not too small compared to those forming t.
                float u = t / d;
                float tmp = IKMath.Sqrt(1.0f + IKMath.Abs2(u));
                rot1 = new JacobiRotation(u / tmp, 1.0f / tmp);
            }

            m.ApplyOnTheLeft(0, 1, rot1);
            jRight = new JacobiRotation();
            jRight.MakeJacobi(m, 0, 1);
            jLeft = rot1 * jRight.GetTransposition();
        }



        #region Householder Preconditions

        public class ColPivHouseholderQRPrecondition_MoreRows
        {
            private ColPivHouseholderQR _qr;
            private Vector _workspace; // Change to ColumnVector?

            public void Allocate(JacobiSVD svd) 
            {
                if (svd.GetRowCount() != _qr.GetRowCount() || svd.GetColumnCount() != _qr.GetColumnCount())
                    _qr = new ColPivHouseholderQR(svd.GetRowCount(), svd.GetColumnCount());

                if (svd._computeFullU) _workspace.Resize(svd.GetRowCount());
                else if (svd._computeThinU) _workspace.Resize(svd.GetColumnCount());
            }
            public bool Run(JacobiSVD svd, Matrix matrix)
            {
                if (matrix.GetRowCount() <= matrix.GetColumnCount())
                    return false;   // Invalid situation for the HouseholderQR Preconditioner to run.

                _qr.Compute(matrix);
                svd._workMatrix = _qr.GetMatrixQR().GetBlock(0, 0, matrix.GetColumnCount(), matrix.GetColumnCount()).GetTriangularView(TriangularViewMode.Upper);

                if (svd._computeFullU)
                {
                    _qr.GetHouseholderQ().EvalInto(ref svd._matrixU, _workspace);
                }
                else if (svd._computeThinU)
                {
                    svd._matrixU.SetIdentity(matrix.GetRowCount(), matrix.GetColumnCount());
                    _qr.GetHouseholderQ().ApplyThisOnTheLeft(ref svd._matrixU, _workspace);
                }

                if (svd.ShouldComputeV())
                    svd._matrixV = new Matrix(_qr.GetColumnPermutation()); 

                return true;
            }
        }
        public class ColPivHouseholderQRPrecondition_MoreColumns
        {
            private ColPivHouseholderQR _qr;
            private Matrix _adjoint;
            private Vector _workspace;

            public void Allocate(JacobiSVD svd)
            {
                if (svd.GetColumnCount() != _qr.GetRowCount() || svd.GetRowCount() != _qr.GetColumnCount())
                    _qr = new ColPivHouseholderQR(svd.GetColumnCount(), svd.GetRowCount());

                if (svd._computeFullV)
                    _workspace.Resize(svd.GetColumnCount());
                else if (svd._computeThinV)
                    _workspace.Resize(svd.GetRowCount());

                _adjoint.Resize(svd.GetColumnCount(), svd.GetRowCount());
            }
            public bool Run(JacobiSVD svd, Matrix matrix)
            {
                if (matrix.GetColumnCount() <= matrix.GetRowCount())
                    return false;   // Invalid situation for the HouseholderQR Preconditioner to run.

                _adjoint = matrix.GetAdjoint();
                _qr.Compute(_adjoint);
                svd._workMatrix = _qr.GetMatrixQR().GetBlock(0, 0, matrix.GetRowCount(), matrix.GetRowCount()).GetTriangularView(TriangularViewMode.Upper).GetAdjoint();

                if (svd._computeFullV)
                {
                    _qr.GetHouseholderQ().EvalInto(ref svd._matrixV, _workspace);
                }
                else if (svd._computeThinV)
                {
                    svd._matrixV.SetIdentity(matrix.GetColumnCount(), matrix.GetRowCount());
                    _qr.GetHouseholderQ().ApplyThisOnTheLeft(ref svd._matrixV, _workspace);
                }

                if (svd.ShouldComputeU())
                    svd._matrixU = new Matrix(_qr.GetColumnPermutation());

                return true;
            }
        }

        #endregion
    }
}