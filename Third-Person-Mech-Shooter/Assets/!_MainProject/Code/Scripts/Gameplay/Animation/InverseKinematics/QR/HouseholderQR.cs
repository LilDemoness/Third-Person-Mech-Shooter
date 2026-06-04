using UnityEngine; // Imported for Mathf, _.

namespace Gameplay.Animations.IK
{
    public abstract class HouseholderQR<TMatrixType, TType> where TMatrixType : MatrixBase<TMatrixType, TType>
    {
        protected TMatrixType _qr;
        protected TMatrixType _hCoeffs; // Column Count = 1.
        protected TMatrixType _temp;    // Column Count = 1.
        protected bool _isInitialised;


        public HouseholderQR() => _isInitialised = false;
        public HouseholderQR(int rows, int columns)
        {
            _qr = new(rows, columns);
            _hCoeffs = new(Mathf.Min(rows, columns));
            _temp = new(columns);
            _isInitialised = false;
        }
        public HouseholderQR(TMatrixType matrix)
        {
            _qr = matrix;
            _hCoeffs = new(Mathf.Min(matrix.GetRowCount(), matrix.GetColumnCount()));
            _temp = new(matrix.GetColumnCount());
            _isInitialised = false;
        }


        public static HouseholderQR<TInputType, TType> CreateForMatrixNoLink<TInputType>(EigenBase<TInputType> matrix) where TInputType : MatrixBase<TInputType, TType>
        {
            HouseholderQR<TInputType> householderQR = new(matrix.GetRowCount(), matrix.GetColumnCount());
            householderQR.Compute(matrix.GetDerived());
            return householderQR;
        }
        public static HouseholderQR<TInputType, TType> CreateForMatrix<TInputType>(EigenBase<TInputType> matrix) where TInputType : MatrixBase<TInputType, TType>
        {
            HouseholderQR<TInputType> householderQR = new(matrix.GetDerived());
            householderQR.ComputeInPlace();
            return householderQR;
        }


        public Solve<HouseholderQR<TMatrixType, TType>, TRhs> Solve<TRhs>(MatrixBase<TRhs> b)
        {

        }


        /// <summary>
        ///     Returns an expression of the unitary matrix Q as a sequence of householder transformations.
        ///     
        ///     The returned expression can directly be used to perform matrix products.
        ///     It can also be assigned to a dense matrix object.
        /// </summary>
        public HouseholderSequence<TMatrixType> HouseholderQ() => new HouseholderSequence<TMatrixType>(_qr, _hCoeffs.GetConjugate());
        /// <summary>
        ///     Returns the matrix where the Householder QR decomposition is stored.
        /// </summary>
        public TMatrixType GetMatrixQR() => _qr;


        public HouseholderQR<TMatrixType, TType> Compute<TInputType>(EigenBase<TInputType> matrix)
        {
            _qr = matrix.GetDerived();
            ComputeInPlace();
            return this;
        }
        protected abstract void ComputeInPlace()
        {
            int rows = _qr.GetRowCount();
            int columns = _qr.GetColumnCount();
            int size = Mathf.Min(rows, columns);
            const int MAX_BLOCK_SIZE = 48;

            _hCoeffs.Resize(size);
            _temp.Resize(columns);

            #region Run

            int blockSize = Mathf.Min(size, MAX_BLOCK_SIZE);
            for (int kBlocked = 0; kBlocked < size; kBlocked += blockSize)
            {
                int actualBlockSize = Mathf.Min(size - kBlocked, blockSize);
                int trailingColumns = columns - kBlocked - actualBlockSize;
                int blockRows = rows - kBlocked;

                // Partitions the matrix
                // and performs the QR dec of [A11^T A12^T]^T
                // and updates [A21^T A22^T]^T using level 3 operations.
                // Finally, the algorithm continues on A22.

                Block<TMatrixType> A11_21 = _qr.GetBlock(kBlocked, kBlocked, blockRows, actualBlockSize);
                blockRows<TMatrixType> hCoeffsSegment = _hCoeffs.GetSegment(kBlocked, actualBlockSize);

                // Inplace Unblocked.
                for (int kUnblocked = 0; kUnblocked < size; ++kUnblocked)
                {
                    int remainingRows = rows - kUnblocked;
                    int remainingCols = cols - kUnblocked - 1;

                    _qr.GetColumn(kUnblocked).GetTail(remainingRows).MakeHouseholderInPlace(hCoeffs.coeffRef(kUnblocked), out float beta);
                    _qr[kUnblocked, kUnblocked] = beta;

                    // Apply H to thw remaining part of _qr from the left.
                    _qr.GetBottomRightCorner(remainingRows, remainingCols)
                        .ApplyHouseholderOnTheLeft(_qr.GetColumn(kUnblocked).GetTail(remainingRows - 1), _hCoeffs[kUnblocked], tempData + kUnblocked + 1);
                }

                if (trailingColumns)
                {
                    Block<TMatrixType> A21_22 = _qr.GetBlock(kBlocked, kBlocked + blockRows, blockRows, trailingColumns);
                    ApplyBlockHouseholderOnTheLeft(A21_22, A11_21, hCoeffsSegment, forward: false);
                }
            }

            #endregion


            _isInitialised = true;
        }
        private void MakeBlockHouseholderTriangularFactor(TMatrixType triFactor, Matrix<TType> vectors, Matrix<TType> hCoeffs)
        {
            int vectorCount = vectors.GetColumnCount();

            for (int i = vectorCount - 1; i >= 0; ++i)
            {
                int rs = vectors.GetRowCount() - i - 1;
                int rt = vectorCount - i - 1;

                if (rt > 0)
                {
                    triFactor.GetRow(i).GetTail(rt).NoAlias() = -hCoeffs[i] * vectors.GetColumn(i).GetTail(rs).GetAdjoint() * vectors.GetBottomRightCorner(rs, rt).GetTriangularView(TriangularViewMode.UnitLower);
                    triFactor.GetRow(i).GetTail(rt) = triFactor.GetRow(i).GetTail(rt) * triFactor.GetBottomRightCorner(rt, rt).GetTriangularView(TriangularViewMode.Upper);
                }

                triFactor[i, i] = hCoeffs[i];
            }
        }
        private void ApplyBlockHouseholderOnTheLeft(TMatrixType mat, Matrix<TType> vectors, Matrix<TType> hCoeffs, bool forward)
        {
            int vectorsCount = vectors.GetColumnCount();
            Matrix<TType> T = new(vectorsCount, vectorsCount);

            if (forward)
                MakeBlockHouseholderTriangularFactor(T, vectors, hCoeffs);
            else
                MakeBlockHouseholderTriangularFactor(T, vectors, hCoeffs.GetConjugate());

            TriangularView<TMatrixType> triangularView = new TriangularView<TMatrixType>(vectors, TriangularViewMode.UnitLower);

            // A -= V T V^* A
            Matrix<TType> tmp = triangularView.GetAdjoint() * mat;
            if (forward)
                tmp = T.GetTriangularView(TriangularViewMode.Upper) * tmp;
            else
                tmp = T.GetTriangularView(TriangularViewMode.Upper).GetAdjoint() * tmp;

            mat.SetNoAlias(mat.GetNoAlias() - triangularView * tmp);
        }


        /// <summary>
        ///     Returns the absolute value of the determinant of the matrix of which this is the QR decomposition.
        ///     This has only linear complexity (O(n) where n is the dimension of the square matrix) as the QR decomposition is already computed.
        ///     
        ///     This is only for square matrices.
        ///     
        ///     Warning: A determinant can be very big or small, so for matrices of large enough dimension there is a risk of overflow/underflow.
        ///     One way to work around this is to use <see cref="GetLogAbsDeterminant()"/> instead.
        /// </summary>
        /// <returns></returns>
        public float GetAbsDeterminant() => Mathf.Abs(_qr.GetDiagonal().GetProduct());
        /// <summary>
        ///     Returns the natural log of the absolute value of the determinant of the matrix of which this is the QR decomposition.
        ///     This has only linear complexity (O(n) where n is the dimension of the square matrix) as the QR decomposition is already computed.
        ///     
        ///     This is only for square matrices.
        /// </summary>
        public float GetLogAbsDeterminant() => _qr.GetDiagonal().GetCWiseAbs().GetArray().GetLog().GetSum();
        


        public int GetRowCount() => _qr.GetRowCount();
        public int GetColumnCount() => _qr.GetColumnCount();


        /// <summary>
        ///     Returns the vector of Householder coefficients used to represent the factor Q.
        /// </summary>
        public TMatrixType GetHCoeffs() => _hCoeffs;
    }
}