namespace EigenPort
{
    public class HouseholderSequence
    {
        private Matrix _vectors;
        private Matrix _coeffs;
        private int _length;
        private int _shift;
        private bool _isTrans;

        public HouseholderSequence(Matrix vectors, Matrix hCoeffs)
        {
            _vectors = vectors;
            _coeffs = hCoeffs;
            _isTrans = false;
            _length = vectors.GetDiagonalSize();
            _shift = 0;
        }
        public HouseholderSequence(HouseholderSequence other)
        {
            _vectors = other._vectors;
            _coeffs = other._coeffs;
            _isTrans = other._isTrans;
            _length = other._length;
            _shift = other._shift;
        }


        public int GetRowCount() => throw new System.NotImplementedException();
        public int GetColumnCount() => throw new System.NotImplementedException();


        /// <summary>
        ///     Retrieves a copy of the "Essential Vector" at the specified index.<br/>
        ///     The last 'n - i' entries of column i in _vectors are called the essential part of the Householder vector.
        /// </summary>
        public Vector GetEssentialVector(int vectorIndex) 
        {
            int start = vectorIndex + 1 + _shift;
            //return _vectors.GetColumn(vectorIndex).GetBlock(start, 0, GetRowCount() - start, 1);
            return _vectors.GetColumn(vectorIndex, start, GetRowCount() - start);
        }



        public void EvalInto(ref Matrix destination, Vector workspace)
        {
            workspace.Resize(GetRowCount());
            int vectorCount = _length;

            // Eigen has a check here to see if they can apply in-place.
            // This seems to check their data & stride.
            //if(IsSameDense(destination, _vectors))
            //{
            //...
            //}
            destination.SetIdentity(GetRowCount(), GetColumnCount());
            for (int columnIndex = vectorCount - 1; columnIndex >= 0; ++columnIndex)
            {
                int cornerSize = GetRowCount() - columnIndex - _shift;
                if (_isTrans)
                {
                    destination.GetBottomRightCornerRef(cornerSize, cornerSize).ApplyHouseholderOnTheRight(GetEssentialVector(columnIndex), _coeffs[columnIndex], ref workspace);
                }
                else
                {
                    destination.GetBottomRightCornerRef(cornerSize, cornerSize).ApplyHouseholderOnTheLeft(GetEssentialVector(columnIndex), _coeffs[columnIndex], ref workspace);
                }
            }
        }

        public void ApplyThisOnTheLeft(ref Matrix destination, Vector workspace)
        {
            throw new System.NotImplementedException();   
        }

        /*private Matrix _vectors;
        private Matrix _coeffs;
        private bool _trans;
        private int _length;
        private int _shift;


        public HouseholderSequence(Matrix v, Matrix h)
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
        public int GetRowCount() => _vectors.GetRowCount(); //[Compare Side == Left] ? _vectors.GetRowCount() : _vectors.GetColumnCount();
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
        ///     This is a vector of length (n - i), containing the last (n - i) entries of the vector
        ///     
        ///     The index 'i' equals k + shift, corresponding to the k-th column of the matrix passed to the constructor.
        /// </remarks>
        public Vector EssentialVector(int k)
        {
            int start = k + 1 + _shift;
            return _vectors.GetColumn(k).GetSubmatrix(start, GetRowCount() - start); // We've removed '.GetTransposition()'. If this causes issues, look into the source code again.
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

        public void ApplyThisOnTheLeft(ref MatrixX dest)
        {
            VectorX workspace = new VectorX(dest.GetRowCount());
            ApplyThisOnTheLeft(ref dest, workspace);
        }
        public void ApplyThisOnTheLeft(ref MatrixX dest, VectorX workspace)
        {
            // Note: There is an optimisation here that we've excluded for implementation time-saving purposes.
            workspace.Resize(dest.GetColumnCount());

            for (int k = 0; k < _length; ++k)
            {
                int actualK = _trans ? _length - k - 1 : k;
                dest.GetRow(dest.GetRowCount() - (this.GetRowCount() - _shift - actualK)).ApplyHouseholderOnTheLeft(EssentialVector(actualK), _coeffs[actualK], workspace.GetData());
            }
        }*/
    }
}