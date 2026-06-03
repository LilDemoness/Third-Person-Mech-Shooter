using UnityEngine;

namespace Gameplay.Animations
{
    public abstract class SVDBase
    {

    }

    public class JacobiSVD : SVDBase
    {
        [System.Serializable, System.Flags]
        public enum ComputationOptions
        {
            ComputeThinU = 1 << 0,
            ComputeThinV = 1 << 1,
            ComputeFullU = 1 << 2,
            ComputeFullV = 1 << 3,
        }


        private bool _isInitialised;
        private bool _isAllocated;

        private int _rows;
        private int _columns;
        private int _diagonalSize;
        private ComputationOptions _computationOptions;

        private bool _computeThinU, _computeThinV;
        private bool _computeFullU, _computeFullV;

        private MatrixX _matrixU;
        private MatrixX _matrixV;
        private MatrixX _scaledMatrix;
        private MatrixX _workMatrix;

        private VectorX _singularValues;
        private int _nonZeroSingularValues;             // The number of singular values that are not exactly 0.

        private QRPreconditioner _qrPrecondMoreRows, _qrPrecondMoreColumns;


        public MatrixX GetMatrixU() => _matrixU;
        public MatrixX GetMatrixV() => _matrixV;
        public VectorX GetSingularValues() => _singularValues;

        public bool ShouldComputeU() => _computeFullU || _computeThinU;
        public bool ShouldComputeV() => _computeFullV || _computeThinV;


        public JacobiSVD(int rows, int columns, ComputationOptions computationOptions = 0)
        {
            Allocate(rows, columns, computationOptions);
        }
        public JacobiSVD(MatrixX matrix, ComputationOptions computationOptions = 0)
        {
            Compute(matrix, computationOptions);
        }


        private void Allocate(int rows, int columns, ComputationOptions computationOptions)
        {
            Debug.Assert(rows >= 0 && columns >= 0);

            if (_isAllocated && rows == _rows && columns == _columns && computationOptions == _computationOptions)
                return; // Already allocated with the desired parameters.

            _rows = rows;
            _columns = columns;
            _isInitialised = false;
            _isAllocated = true;
            _computationOptions = computationOptions;

            _computeFullU = (computationOptions & ComputationOptions.ComputeFullU) != 0;
            _computeThinU = (computationOptions & ComputationOptions.ComputeThinU) != 0;
            _computeFullV = (computationOptions & ComputationOptions.ComputeFullV) != 0;
            _computeThinV = (computationOptions & ComputationOptions.ComputeThinV) != 0;

            Debug.Assert(!(_computeFullU && _computeThinU), "You can't ask for both Full and Thin U");
            Debug.Assert(!(_computeFullV && _computeThinV), "You can't ask for both Full and Thin V");

            _diagonalSize = Mathf.Min(_rows, _columns);
            _singularValues.Resize(_diagonalSize);
            _matrixU.Resize(_rows, _computeFullU ? _rows
                                   : _computeThinU ? _diagonalSize
                                   : 0);
            _matrixV.Resize(_columns, _computeFullV ? _columns
                                     : _computeThinV ? _diagonalSize
                                     : 0);
            _workMatrix.Resize(_diagonalSize, _diagonalSize);

            if (_columns > _rows) _qrPrecondMoreColumns.Allocate(this);
            if (_rows > _columns) _qrPrecondMoreRows.Allocate(this);
            if (_rows != _columns) _scaledMatrix.Resize(rows, columns);
        }

        public JacobiSVD Compute(MatrixX matrix) => Compute(matrix, _computationOptions);
        public JacobiSVD Compute(MatrixX matrix, ComputationOptions computationOptions)
        {
            Allocate(matrix.GetRowCount(), matrix.GetColumnCount(), computationOptions);

            // Used to reduce iterations which would worsen the precision of U and V as more rotations are accumulated.
            const float precision = 2.0f * float.Epsilon;

            // Limit for denormal numbers to be considered zero in order to avoid infinite loops.
            const float considerAsZero = float.Epsilon;

            // Scaling factor to reduce over/under-flows.
            float scale = matrix.GetCoefficientWiseAbs().GetMaxCoefficient();
            if (scale == 0) 
                scale = 1;

            // Step 1 - R-SVD: Use a QR decomposition to reduce to the case of a square matrix.
            if (_rows != _columns)
            {
                _scaledMatrix = matrix / scale;
                _qrPrecondMoreColumns.Run(this, _scaledMatrix);
                _qrPrecondMoreRows.Run(this, _scaledMatrix);
            }
            else
            {
                _workMatrix = matrix.GetSubmatrix(_diagonalSize, _diagonalSize, 0, 0) / scale;
                if (_computeFullU) { _matrixU.SetIdentity(_rows, _rows); }
                else if (_computeThinU) { _matrixU.SetIdentity(_rows, _diagonalSize); }
                if (_computeFullV) { _matrixV.SetIdentity(_columns, _columns); }
                else if (_computeThinV) { _matrixV.SetIdentity(_columns, _diagonalSize); }
            }


            // Step 2: The main Jacobi SVD iteration.
            float maxDiagEntry = _workMatrix.GetCoefficientWiseAbs().GetDiagonal().GetMaxCoefficient();

            bool finished = false;
            while (!finished)
            {
                finished = true;

                // Do a sweep: For all index pairs (p,q), perform SVD of the corresponding 2x2 sub-matrix.
                for (int p = 1; p < _diagonalSize; ++p)
                {
                    for (int q = 0; q < p; ++q)
                    {
                        float threshold = Mathf.Max(considerAsZero, precision * maxDiagEntry);
                        if (Mathf.Abs(_workMatrix[p, q]) > threshold || Mathf.Abs(_workMatrix[q, p]) > threshold)
                        {
                            finished = false;

                            // Perform SVD decomposition of the 2x2 sub-matrix corresponding to indicies p,q to make it diagonal.
                            // The complex to real operation returns true if the updated 2x2 block is not already diagonal.
                            if (RunSVDPrecondition_2x2BlockToBeReal(_workMatrix, p, q, maxDiagEntry))
                            {
                                Real2x2JacobiSvd(_workMatrix, p , q, out JacobiRotation jLeft, out JacobiRotation jRight);

                                // Accumulate resulting Jacobi rotations.
                                _workMatrix.ApplyOnTheLeft(p, q, jLeft);
                                if (ShouldComputeU())
                                    _matrixU.ApplyOnTheRight(p, q, jLeft.GetTransposition());

                                _workMatrix.ApplyOnTheLeft(p, q, jRight);
                                if (ShouldComputeV())
                                    _matrixU.ApplyOnTheRight(p, q, jRight);


                                // Keep track of the largest diagonal coefficient.
                                maxDiagEntry = Mathf.Max(maxDiagEntry, Mathf.Abs(_workMatrix[p,p]), Mathf.Abs(_workMatrix[q, q]));
                            }
                        }
                    }
                }
            }


            // Step 3: The work matrix is now diagonal, so ensure it's positive so its diagonal entries are the singular values.
            for (int i = 0; i < _diagonalSize; ++i)
            {
                // For a complex matrix, some diagonal coefficients might not have been treated by 'RunSVDPrecondition_2x2BlockToBeReal'
                // However, we shouldn't have any complex numbers, and so can skip this check.
                /*if (IS_COMPLEX && Mathf.Abs(_workMatrix[i, i].GetImaginary() > considerAsZero)
                {
                    // Ensure the imaginary part of all diagonal entries is null.
                    float a = Mathf.Abs(_workMatrix[i,i]);
                    _singularValues[i] = Mathf.Abs(a);
                    if (ShouldComputeU())
                        _matrixU.SetColumn(i, _matrixU.GetColumn(i) * _workMatrix[i,i] / a);
                }
                else*/
                {
                    float a = _workMatrix[i,i];
                    _singularValues[i] = Mathf.Abs(a);
                    if (ShouldComputeU() && a < 0.0f)
                        _matrixU.SetColumn(i, _matrixU.GetColumn(i) * -1);
                }
            }
            _singularValues *= scale;


            // Step 4: Sort singular values in descending order and compute the number of non-zero singular values.
            _nonZeroSingularValues = _diagonalSize;
            for (int i = 0; i < _diagonalSize; ++i)
            {
                float maxRemainingSingularValue = _singularValues.GetTail(_diagonalSize - i).GetMaxCoefficient(out int maxIndex);
                if (maxRemainingSingularValue == 0.0f)
                {
                    _nonZeroSingularValues = i;
                    break;
                }
                if (maxIndex != -1)
                {
                    maxIndex += 1;

                    float temp = _singularValues[i];
                    _singularValues[i] = _singularValues[maxIndex];
                    _singularValues[maxIndex] = temp;

                    if (ShouldComputeU()) _matrixU.SwapColumns(maxIndex, i);
                    if (ShouldComputeV()) _matrixV.SwapColumns(maxIndex, i);
                }
            }


            _isInitialised = true;
            return this;
        }


        // Aka: 'internal::svd_precondition_2x2_block_to_be_real<MatrixType, QRPreconditioner>::run'
        /// <summary>
        ///     Performs SVD decomposition of a 2x2 sub-matrix corresponding to the indicies p,q to make it diagonal.
        ///     The complex to real operation returns true if the updated 2x2 block is not already diagonal.
        /// </summary>
        /// <remarks>
        ///     As float is not a complex number, we don't need any calculations and this always returns true.
        /// </remarks>
        private bool RunSVDPrecondition_2x2BlockToBeReal(MatrixX workMatrix, int p, int q, float maxDiagonalEntry) => true;
        /*{
            float z;
            JacobiRotation<float> rot;
            float n = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(workMatrix[p,p]), 2) + Mathf.Pow(Mathf.Abs(workMatrix[q, p]), 2));

            const float considerAsZero = float.Epsilon;
            const float precision = float.Epsilon;

            if (n == 0)
            {
                // Ensure the first column is zero.
                workMatrix[p, p] = 0.0f;
                workMatrix[q, p] = 0.0f;

                if (Mathf.Abs(workMatrix[p, q]) > considerAsZero)
                {
                    z = Mathf.Abs(workMatrix[p, q]) / workMatrix[p, q];
                    workMatrix.SetRow(workMatrix.GetRow(p) * z);
                    if (ShouldComputeU())
                        _matrixU.SetColumn(_matrixU.GetColumn(q) * conj(z));
                }
            }
        }*/

        private void Real2x2JacobiSvd(MatrixX matrix, int p, int q, out JacobiRotation jLeft, out JacobiRotation jRight)
        {
            MatrixX m = new MatrixX(2,2);
            m[0,0] = matrix[p,p];
            m[0,1] = matrix[p,q];
            m[1,0] = matrix[q,p];
            m[1,1] = matrix[q,q];

            JacobiRotation rot1 = new();
            float t = m[0,0] + m[1,1];
            float d = m[1,0] - m[0,1];

            if (Mathf.Abs(d) < float.Epsilon)
            {
                rot1.SetS(0.0f);
                rot1.SetC(1.0f);
            }
            else
            {
                // If d != 0, then t / d cannot overfloat because the magnitude of
                // the entries forming d are not too small compared to those forming t.
                float u = t / d;
                float temp = Mathf.Sqrt(1.0f + Mathf.Pow(Mathf.Abs(u), 2));
                rot1.SetS(1.0f / temp);
                rot1.SetC(u / temp);
            }

            m.ApplyOnTheLeft(0, 1, rot1);
            jRight = new JacobiRotation().MakeJacobi(m, 0, 1);
            jLeft = rot1 * jRight.GetTransposition();
        }


        #region QR Preconditioners

        /// <summary>
        ///     QRPreconditioners reduce the problem of computing the SVD to the case of a square matrix.
        ///     
        ///     This approach, known as R-SVD, is an optimisation for rectangular-enough matrices, and is
        ///     a requirement for our SacobiSVD implementation which by itself only works on square matrices.
        /// </summary>
        public abstract class QRPreconditioner
        {
            public enum QRPreconditionerType
            {
                ColPivHouseholder,
                FullPivHouseholder,
                Householder,
                None,
            }
            public static QRPreconditioner GetInstanceForType(QRPreconditionerType type, bool moreRowsThanCols)
            {
                return type switch
                {
                    QRPreconditionerType.None => null,

                    QRPreconditionerType.FullPivHouseholder => moreRowsThanCols ? new QRPreconditionerMoreRowsThanCols_FullPivHouseholder() : new QRPreconditionerMoreColsThanRows_FullPivHouseholder(),
                    QRPreconditionerType.ColPivHouseholder => moreRowsThanCols ? new QRPreconditionerMoreRowsThanCols_ColPivHouseholder() : new QRPreconditionerMoreColsThanRows_ColPivHouseholder(),
                    QRPreconditionerType.Householder => moreRowsThanCols ? new QRPreconditionerMoreRowsThanCols_Householder() : new QRPreconditionerMoreColsThanRows_Householder(),

                    _ => throw new System.NotImplementedException($"No implementation for type: {type.ToString()}")
                };
            }


            protected VectorX _workspace;

            public abstract void Allocate(JacobiSVD svd);    
            public abstract bool Run(JacobiSVD svd, MatrixX matrix);    
        }

        
        #region Full Piv Householder QR Preconditioners

        /// <inheritdoc cref="QRPreconditioner"/>
        public abstract class FullPivHouseholderQRPreconditioner : QRPreconditioner
        {
            protected FullPivHouseholderQR _qr;
        }

        public class QRPreconditionerMoreRowsThanCols_FullPivHouseholder : FullPivHouseholderQRPreconditioner
        {
            public override void Allocate(JacobiSVD svd)
            {
                if (svd._rows != _qr.GetRowCount() || svd._columns != _qr.GetColumnCount())
                    _qr = new MatrixX(svd._rows, svd._columns);

                if (svd._computeFullU)
                    _workspace.Resize(svd._rows);
            }
            public override bool Run(JacobiSVD svd, MatrixX matrix)
            {
                if (matrix.GetRowCount() <= matrix.GetColumnCount())
                    return false; // Matrix is invalid for this QR Preconditioner.

                _qr.Compute(matrix);
                svd._workMatrix = _qr.MatrixQR().GetSubmatrix(matrix.GetColumnCount(), matrix.GetColumnCount(), 0, 0).GetTriangularViewUpper();

                if (svd._computeFullU)
                    _qr.MatrixQ().EvalTo(svd._matrixU, _workspace);

                if (svd.ShouldComputeV())
                    svd._matrixV = _qr.GetColsPermutation();

                return true;
            }
        }
        public class QRPreconditionerMoreColsThanRows_FullPivHouseholder : FullPivHouseholderQRPreconditioner
        {
            private MatrixX _adjoint;

            public override void Allocate(JacobiSVD svd)
            {
                if (svd._rows != _qr.GetRowCount() || svd._columns != _qr.GetColumnCount())
                    _qr = new MatrixX(svd._rows, svd._columns);

                if (svd._computeFullV)
                    _workspace.Resize(svd._columns);

                _adjoint.Resize(svd._columns, svd._rows);
            }
            public override bool Run(JacobiSVD svd, MatrixX matrix)
            {
                if (matrix.GetColumnCount() <= matrix.GetRowCount())
                    return false; // Matrix is invalid for this QR Preconditioner.

                _adjoint = matrix.GetAdjoint();
                _qr.Compute(_adjoint);

                svd._workMatrix = _qr.MatrixQR().GetSubmatrix(matrix.GetRowCount(), matrix.GetRowCount(), 0, 0).GetTriangularViewUpper().GetAdjoint();
                if (svd._computeFullV)
                    _qr.MatrixQ().EvalTo(svd._matrixV, _workspace);

                if (svd.ShouldComputeU())
                    svd._matrixU = _qr.ColsPermutation();

                return true;
            }
        }


        public class FullPivHouseholderQR
        {
            private MatrixX _qr;
            private MatrixX _horizontalCoefficients;

            private MatrixX _rowsTranspositions;
            private MatrixX _colsTranspositions;

            private x _colsPermutation;

            private MatrixX _temp;


            private bool _isInitialised, _usePrescribedThreshold;
        }

        #endregion

        // Default:
        #region Col Piv Householder QR Preconditioners

        /// <inheritdoc cref="QRPreconditioner"/>
        public abstract class ColPivHouseholderQRPreconditioner : QRPreconditioner
        {
            protected ColPivHouseholderQR _qr;
        }

        public class QRPreconditionerMoreRowsThanCols_ColPivHouseholder : ColPivHouseholderQRPreconditioner
        {
            public override void Allocate(JacobiSVD svd)
            {
                if (svd._rows != _qr.GetRowCount() || svd._columns != _qr.GetColumnCount())
                    _qr = new MatrixX(svd._rows, svd._columns);

                if (svd._computeFullU)
                    _workspace.Resize(svd._rows);
                else if (svd._computeThinU)
                    _workspace.Resize(svd._columns);
            }
            public override bool Run(JacobiSVD svd, MatrixX matrix)
            {
                if (matrix.GetColumnCount() <= matrix.GetRowCount())
                    return false; // Matrix is invalid for this QR Preconditioner.

                _qr.Compute(matrix);

                svd._workMatrix = _qr.MatrixQR().GetSubmatrix(matrix.GetColumnCount(), matrix.GetColumnCount(), 0, 0).GetTriangularViewUpper();
                if (svd._computeFullU)
                    _qr.HouseholderQ().EvalTo(svd._matrixU, _workspace);
                else if (svd._computeThinU)
                {
                    svd._matrixU.SetIdentity(matrix.GetRowCount(), matrix.GetColumnCount());
                    _qr.HouseholderQ().ApplyThisOnTheLeft(svd._matrixU, _workspace);
                }

                if (svd.ShouldComputeV())
                    svd._matrixV = _qr.ColsPermutation();

                return true;
            }
        }
        public class QRPreconditionerMoreColsThanRows_ColPivHouseholder : ColPivHouseholderQRPreconditioner
        {
            private MatrixX _adjoint;

            public override void Allocate(JacobiSVD svd)
            {
                if (svd._rows != _qr.GetRowCount() || svd._columns != _qr.GetColumnCount())
                    _qr = new MatrixX(svd._rows, svd._columns);

                if (svd._computeFullV)
                    _workspace.Resize(svd._columns);
                else if (svd._computeThinV)
                    _workspace.Resize(svd._rows);

                _adjoint.Resize(svd._columns, svd._rows);
            }
            public override bool Run(JacobiSVD svd, MatrixX matrix)
            {
                if (matrix.GetColumnCount() <= matrix.GetRowCount())
                    return false; // Matrix is invalid for this QR Preconditioner.

                _adjoint = matrix.GetAdjoint();
                _qr.Compute(_adjoint);

                svd._workMatrix = _qr.MatrixQR().GetSubmatrix(matrix.GetRowCount(), matrix.GetRowCount(), 0, 0).GetTriangularViewUpper().GetAdjoint();
                if (svd._computeFullV)
                    _qr.HouseholderQ().EvalTo(svd._matrixV, _workspace);
                else if (svd._computeThinV)
                {
                    svd._matrixV.SetIdentity(matrix.GetColumnCount(), matrix.GetRowCount());
                    _qr.HouseholderQ().ApplyThisOnTheLeft(svd._matrixV, _workspace);
                }

                if (svd.ShouldComputeU())
                    svd._matrixU = _qr.ColsPermutation();

                return true;
            }
        }


        /// <summary>
        ///     Householder rank-revealing QR decomposition of a matrix with column pivoting.
        ///     
        ///     This class performs a rank-revealing QR decomposition of a matrix 'A', into matrices, 'P', 'Q', and 'R', such that
        ///     'A / P = Q / R' by using Householder transformations.
        ///     Here, P is a permutation matrix, Q a unitary matrix, and R an upper triangular matrix
        ///     
        ///     This decomposition performs column pivoting in order to be rank-revealing and improve numberical stability.
        ///     It is slower than <see cref="HouseholderQR"/> and faster than <see cref="FullPivHouseholderQR"/>
        /// 
        /// </summary>
        public class ColPivHouseholderQR
        {
            protected MatrixX _qr;
            protected MatrixX _householderCoefficients; //HCoeffsType > XprHelper.h #609 | Maybe use VectorX instead?
            protected PermutationMatrix _colsPermutation;
            protected MatrixX _colsTranspositions;
            protected MatrixX _temp;
            protected MatrixX _colNormsUpdated;
            protected MatrixX _colNormsDirect;

            protected bool _isInitialised;
            protected bool _usePrescribedThreshold;

            protected float _prescribedThreshold;
            protected float _maxPivot;

            protected int _nonzeroPivots;
            protected int _detPQ;


            public ColPivHouseholderQR()
            {

            }


            public HouseholderSequence HouseholderQ() => new HouseholderSequence(_qr, _householderCoefficients.GetConjugate());
            public HouseholderSequence MatrixQ() => HouseholderQ();
            public MatrixX MatrixQR() => _qr;

            /// <summary>
            ///     Returns the matrix where the result of the Householder QR is stored.<br/>
            ///     
            ///     The struct lower part of this matrix contains internal values.
            ///     Only the upper triangular part should be referenced. To get it, use:
            ///         GetMatrixR().GetTriangularViewUpper()
            ///     
            ///     For rank-deficient matrices, use:
            ///         GetMatrixR().TopLeftCorner(GetRank(), GetRank()).GetTriangularViewUpper()
            /// </summary>
            public MatrixX MatrixR() => _qr;

            /// <summary>
            ///     Returns the column permutation matrix.
            /// </summary>
            public PermutationMatrix ColsPermutation() => _colsPermutation;


            /// <summary>
            ///     Returns the absolute value of the determinant of the matrix of which this is the QR decomposition.
            ///     
            ///     The determinant can be very big or small, so for matrices
            ///     of large enough dimensions there is a risk of overflow/underflow.
            ///     One way to work around this is to use <see cref="GetLogAbsDeterminant()"/> instead
            /// </summary>
            /// <remarks>
            ///     Linear complexity (O(n) where n is the dimension of the square matrix)
            ///     as the QR decomposition has already been computed.
            /// </remarks>
            public float GetAbsDeterminant() => Mathf.Abs(_qr.GetDiagonal().GetProduct());

            public float GetLogAbsDeterminant() => _qr.GetDiagonal().CoefficientWiseAbs().Array().Log().Sum();


            /// <summary>
            ///     Returns the rank of the matrix of which this is the QR decomposition.<br/>
            ///     
            ///     This method has to determine which pivots should be considered non-zero.<br/>
            ///     You can set the threshold value used by calling <see cref="SetThreshold(float)"/>.
            /// </summary>
            public int GetRank()
            {
                float premultipliedThreshold = Mathf.Abs(_maxPivot) * GetThreshold();
                int result = 0;
                for (int i = 0; i < _nonzeroPivots; ++i)
                    result += Mathf.Abs(_qr[i, i]) > premultipliedThreshold ? 1 : 0;

                return result;
            }

            /// <summary>
            ///     Returns the dimension of the kernel of the matrix of which this is the QR decomposition.<br/>
            ///     
            ///     This method has to determine which pivots should be considered non-zero.<br/>
            ///     You can set the threshold value used by calling <see cref="SetThreshold(float)"/>.
            /// </summary>
            public int GetDimensionOfKernel() => GetColumnCount() - GetRank();

            /// <summary>
            ///     Returns true if the matrix of which this is the QR decomposition represents an injective
            ///     linear map (I.e. Has a trivial kernel), otherwise false.<br/>
            ///     
            ///     This method has to determine which pivots should be considered non-zero.<br/>
            ///     You can set the threshold value used by calling <see cref="SetThreshold(float)"/>.
            /// </summary>
            public bool IsInjective() => GetRank() == GetColumnCount();

            /// <summary>
            ///     Returns true if the matrix of which this is the QR decomposition represents a surjective
            ///     linear map, otherwise false.<br/>
            ///     
            ///     This method has to determine which pivots should be considered non-zero.<br/>
            ///     You can set the threshold value used by calling <see cref="SetThreshold(float)"/>.
            /// </summary>
            public bool IsSurjective() => GetRank() == GetRowCount();

            /// <summary>
            ///     Returns true if the matrix of which this is the QR decomposition is invertible
            ///     
            ///     This method has to determine which pivots should be considered non-zero.<br/>
            ///     You can set the threshold value used by calling <see cref="SetThreshold(float)"/>.
            /// </summary>
            public bool IsInvertible() => IsInjective() && IsSurjective();

            /// <summary>
            ///     Returns the inverse of the matrix of which this is the QR decomposition.
            ///     
            ///     If the matrix is not invertible, the returned matrix has undefined coefficients.
            ///     Use <see cref="IsInvertible()"/> to first determine whether this matrix is invertible.
            /// </summary>
            public MatrixX GetInverse()
            {
                
            }


            public int GetRowCount() => _qr.GetRowCount();
            public int GetColumnCount() => _qr.GetColumnCount();


            /// <summary>
            ///     Returns the vector of Householder Coefficients used to represent the factor Q.
            /// </summary>
            /// <returns></returns>
            public MatrixX GetHouseholderCoefficients() => _householderCoefficients;


            public ColPivHouseholderQR SetThreshold(float threshold)
            {
                _usePrescribedThreshold = true;
                _prescribedThreshold = threshold;
                return this;
            }
            public ColPivHouseholderQR ResetThreshold()
            {
                _usePrescribedThreshold = false;
                return this;
            }
            public float GetThreshold() => _usePrescribedThreshold ? _prescribedThreshold : float.Epsilon * (float)_qr.GetDiagonalSize();


            /// <summary>
            ///     Returns the number of nonzero pivots in the QR composition.
            ///     
            ///     Here, nonzero is meant in the exact sense, not in a fuzzy sense.
            /// </summary>
            public int GetNonzeroPivotsCount() => _nonzeroPivots;

            /// <summary>
            ///     Returns the absolute value of the largest pivot
            ///     (I.e. The largest diagonal coefficient of R).
            /// </summary>
            public float GetMaxPivot() => _maxPivot;


            /// <summary>
            ///     Finds a solution to 'x' to the equation Ax=b,
            ///     where A is the matrix of which is is the QR decomposition, if any exists.
            /// </summary>
            /// <param name="b"> The right-hand-side of the equation to solve</param>
            public void Solve(MatrixX b)
            {

            }


            public ColPivHouseholderQR Compute(MatrixX matrix)
            {
                _qr = matrix.GetDerived();
                ComputeInPlace();
                return this;
            }

            protected void ComputeInPlace()
            {

            }
        }

        #endregion

        /// <summary>
        ///     Represents a permutation matrix, internally stored as a vector of integers.
        /// </summary>
        public class PermutationMatrix
        {
            VectorX _indices;


            public PermutationMatrix(int size)
            {
                _indices = new VectorX(size);
            }
            public PermutationMatrix(PermutationMatrix other)
            {
                _indices = other._indices;
            }
            public PermutationMatrix(VectorX indices)
            {
                _indices = indices;
            }


            public VectorX GetIndices() => _indices;
            public void SetIndices(VectorX newValue) => _indices = newValue;


            public int GetRowCount() => _indices.GetSize();
            public int GetColumnCount() => 1;
        }

        /// <summary>
        ///     Represents a product sequence of Householder reflections where:
        ///     - The first Householder reflection acts on the whole space
        ///     - The second Householder reflection leaves the 1D subspace spanned by the first unit vector invariant
        ///     - The third Householder reflection leaves the 2D subspace spanned by the first two unit vectors invariant
        ///     - And so on up to the last reflection which leaves all but 1 dimensions invariant and acts only on the last dimension.
        ///     
        ///     Such sequences of Householder reflections are used in several algorithms to zero out certain parts of a matrix.
        /// </summary>
        public class HouseholderSequence
        {
            private MatrixX _vectors;
            private MatrixX _coeffs;
            private bool _trans;
            private int _length;
            private int _shift;


            public HouseholderSequence(MatrixX v, MatrixX h)
            {
                _vectors = v;
                _coeffs = h;
                _trans = false;
                _length = v.GetDiagonalSize();
                _shift = 0;
            }
            public HouseholderSequence(HouseholderSequence other)
            {
                _vectors = other._vectors;
                _coeffs = other._coeffs;
                _trans = other._trans;
                _length = other._length;
                _shift = other._shift;
            }


            /// <summary>
            ///     Returns the number of rows of transformation viewed as a matrix.
            ///     This equals the dimension of the space that the transformation acts on.
            /// </summary>
            public int GetRowCount() => _vectors.GetRowCount(); /*[Compare Side == Left] ? _vectors.GetRowCount() : _vectors.GetColumnCount();*/
            /// <summary>
            ///     Returns the number of columns of transformation viewed as a matrix.
            ///     This equals the dimension of the space that the transformation acts on.
            /// </summary>
            public int GetColumnCount() => _vectors.GetRowCount();


            public HouseholderSequence GetTransposition() => new HouseholderSequence(this).SetTrans(!_trans);
            public HouseholderSequence GetConjugate() => new HouseholderSequence(_vectors.GetConjugate(), _coeffs.GetConjugate()).SetTrans(_trans).SetLength(_length).SetShift(_shift);
            public HouseholderSequence GetAdjoint() => GetConjugate().SetTrans(!_trans);
            public HouseholderSequence GetInverse() => GetAdjoint();

            /// <summary>
            ///     Returns the essential part of a Householder vector.
            /// </summary>
            /// <param name="k"> Index of householder reflection.</param>
            /// <returns> A Vector containing non-trivial entries of the k-th Householder vector.</returns>
            /// <remarks>
            ///     This function returns the non-essential part of the Householder Vector v_i.
            ///     This is a vector of length (n - 1), containing the last (n - 1) entries of the vector
            ///     
            ///     The index 'i' equals k + shift, corresponding to the k-th column of the matrix passed to the constructor.
            /// </remarks>
            public VectorX EssentialVector(int k)
            {
                int start = k + 1 + _shift;
                return _vectors.GetSubmatrix(1, GetRowCount() - start, k, start).GetTransposition();
            }


            public HouseholderSequence SetTrans(bool newTrans)
            {
                this._trans = newTrans;
                return this;
            }
            public HouseholderSequence SetLength(int newLength)
            {
                this._length = newLength;
                return this;
            }
            public HouseholderSequence SetShift(int newShift)
            {
                this._shift = newShift;
                return this;
            }


            public void ApplyThisOnTheRight(ref MatrixX dest)
            {
                VectorX workspace = new VectorX(dest.GetRowCount());
                ApplyThisOnTheRight(ref dest, workspace);
            }
            public void ApplyThisOnTheRight(ref MatrixX dest, VectorX workspace)
            {
                workspace.Resize(dest.GetRowCount());

                for (int k = 0; k < _length; ++k)
                {
                    int actualK = _trans ? _length - k - 1 : k;
                    dest.GetColumn(dest.GetColumnCount() - (this.GetRowCount() - _shift - actualK)).ApplyHouseholderOnTheRight(EssentialVector(actualK), _coeffs[actualK], workspace.GetData());
                }
            }
        }


        #region Householder QR Preconditioners

        public abstract class HouseholderQRPreconditioner : QRPreconditioner
        {
            protected MatrixX _qr; // Change Type.
        }

        public class QRPreconditionerMoreRowsThanCols_Householder : HouseholderQRPreconditioner
        {
            public override void Allocate(JacobiSVD svd)
            {
                if (svd._rows != _qr.GetRowCount() || svd._columns != _qr.GetColumnCount())
                    _qr = new MatrixX(svd._rows, svd._columns);

                if (svd._computeFullU)
                    _workspace.Resize(svd._rows);
                else if (svd._computeThinU)
                    _workspace.Resize(svd._columns);
            }
            public override bool Run(JacobiSVD svd, MatrixX matrix)
            {
                if (matrix.GetRowCount() <= matrix.GetColumnCount())
                    return false; // Matrix is invalid for this QR Preconditioner.

                _qr.Compute(matrix);
                svd._workMatrix = _qr.MatrixQR().GetSubmatrix(matrix.GetColumnCount(), matrix.GetColumnCount(), 0, 0).GetTriangularViewUpper();

                if (svd._computeFullU)
                    _qr.HouseholderQ().EvalTo(svd._matrixU, _workspace);
                else if (svd._computeFullU)
                {
                    svd._matrixU.SetIdentity(matrix.GetRowCount(), matrix.GetColumnCount());
                    _qr.HouseholderQ().ApplyThisOnTheLeft(svd._matrixU, _workspace);
                }

                if (svd.ShouldComputeV())
                    svd._matrixV.SetIdentity(matrix.GetColumnCount(), matrix.GetColumnCount());

                return true;
            }
        }
        public class QRPreconditionerMoreColsThanRows_Householder : HouseholderQRPreconditioner
        {
            private MatrixX _adjoint;

            public override void Allocate(JacobiSVD svd)
            {
                if (svd._rows != _qr.GetRowCount() || svd._columns != _qr.GetColumnCount())
                    _qr = new MatrixX(svd._rows, svd._columns);

                if (svd._computeFullV)
                    _workspace.Resize(svd._columns);
                else if (svd._computeThinV)
                    _workspace.Resize(svd._rows);

                _adjoint.Resize(svd._columns, svd._rows);
            }
            public override bool Run(JacobiSVD svd, MatrixX matrix)
            {
                if (matrix.GetColumnCount() <= matrix.GetRowCount())
                    return false; // Matrix is invalid for this QR Preconditioner.

                _adjoint = matrix.GetAdjoint();
                _qr.Compute(_adjoint);

                svd._workMatrix = _qr.MatrixQR().GetSubmatrix(matrix.GetRowCount(), matrix.GetRowCount(), 0, 0).GetTriangularViewUpper().GetAdjoint();
                if (svd._computeFullV)
                    _qr.HouseholderQ().EvalTo(svd._matrixV, _workspace);
                else if (svd._computeThinV)
                {
                    svd._matrixV.SetIdentity(matrix.GetColumnCount(), matrix.GetRowCount());
                    _qr.HouseholderQ().ApplyThisOnTheLeft(svd._matrixV, _workspace);
                }

                if (svd.ShouldComputeU())
                    svd._matrixU.SetIdentity(matrix.GetRowCount(), matrix.GetRowCount());

                return true;
            }
        }

#endregion


        #endregion
    }
}