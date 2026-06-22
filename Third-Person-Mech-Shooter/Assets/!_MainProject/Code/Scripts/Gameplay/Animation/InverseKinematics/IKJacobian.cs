using EigenPort;

namespace Gameplay.Animations
{
    public class IKJacobian
    {
        private int _dofCount, _taskSize;
        private bool _isTranspose;

        // The jacobian matrix and its null-space projector.
        private Matrix _jacobian, _jacobianTemp;
        private Matrix _nullspace;

        // The vector of intermediate betas.
        private Vector _beta;

        // The vector of computed angle changes.
        private Vector _dTheta;
        private Vector _dNormWeight;
        
        // Space required for SVD computation.
        private Vector _svdW;
        private Matrix _svdV;
        private Matrix _svdU;

        private Vector _svdUBeta;


        // Space required for SDLS.
        private bool _useSDLS;
        private Vector _norm;
        private Vector _dThetaTemp;
        private float _minDamp;

        // Null space task vector.
        private Vector _alpha;

        // Degree of Freedom weighting.
        private Vector _weight;
        private Vector _weightSqrt;


        public IKJacobian()
        {
            _jacobian = new();
            _jacobianTemp = new();
            _nullspace = new();

            _beta = new();
            _dTheta = new();
            _dNormWeight = new();

            _svdW = new();
            _svdV = new();
            _svdU = new();
            _svdUBeta = new();

            _useSDLS = true;
            _norm = new();
            _dThetaTemp = new();
            _minDamp = 0.0f;

            _alpha = new();

            _weight = new();
            _weightSqrt = new();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dofCount"></param>
        /// <param name="taskSize"></param>
        /// <remarks> Call once to initialise.</remarks>
        public void ArmMatrices(int dofCount, int taskSize)
        {
            _dofCount = dofCount;
            _taskSize = taskSize;

            _jacobian.Resize(taskSize, dofCount);
            _jacobian.SetZeros();

            _alpha.Resize(dofCount);
            _alpha.SetZeros();

            _nullspace.Resize(dofCount, dofCount);

            _dTheta.Resize(dofCount);
            _dThetaTemp.Resize(dofCount);
            _dNormWeight.Resize(dofCount);

            _norm.Resize(dofCount);
            _norm.SetZeros();

            _beta.Resize(taskSize);

            _weight.Resize(dofCount);
            _weightSqrt.Resize(dofCount);
            _weight.SetOnes();
            _weightSqrt.SetOnes();


            if (taskSize >= dofCount)
            {
                _isTranspose = false;

                _jacobianTemp.Resize(taskSize, dofCount);

                _svdU.Resize(taskSize, dofCount);
                _svdV.Resize(dofCount, dofCount);
                _svdW.Resize(dofCount);

                _svdUBeta.Resize(dofCount);
            }
            else
            {
                // Use the SVD of the transpose hacobian.
                // This works just as well as the original, and often allows us to use smaller matrices.
                _isTranspose = true;

                _jacobianTemp.Resize(dofCount, taskSize);

                _svdU.Resize(taskSize, taskSize);
                _svdV.Resize(dofCount, taskSize);
                _svdW.Resize(taskSize);

                _svdUBeta.Resize(taskSize);
            }
        }
        public void SetDoFWeight(int dofId, float weight)
        {
            _weight[dofId] = weight;
            _weightSqrt[dofId] = IKMath.Sqrt(weight);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <param name="v"></param>
        /// <remarks> Iteratively called.</remarks>
        public void SetBetas(int id, int size, Vector3 v)
        {
            _beta[id + 0] = v.x;
            _beta[id + 1] = v.y;
            _beta[id + 2] = v.z;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dofId"></param>
        /// <param name="v"></param>
        /// <param name="normWeight"></param>
        /// <remarks> Iteratively called.</remarks>
        public void SetDerivatives(int id, int dofId, Vector3 v, float normWeight)
        {
            _jacobian[id + 0, dofId] = v.x * _weightSqrt[dofId];
            _jacobian[id + 1, dofId] = v.y * _weightSqrt[dofId];
            _jacobian[id + 2, dofId] = v.z * _weightSqrt[dofId];

            _dNormWeight[dofId] = normWeight;
        }


        public void Invert()
        {
            if (_isTranspose)
            {
                // SVD will decompose Jt into V * W * Ut, with U,V orthogonal and W diagonal.
                // So 'J = U * W * Vt' and 'JInverse = V * WInverse * Ut'.
                JacobiSVD svd = new JacobiSVD(_jacobian.GetTranspose(), JacobiSVD.ComputationOptions.ComputeThinU | JacobiSVD.ComputationOptions.ComputeThinV);

                _svdU = svd.GetMatrixV();
                _svdV = svd.GetMatrixU();
                _svdW = svd.GetSingularValues();
            }
            else
            {
                // SVD will decompose J into U * W * Vt, with U,V orthogonal and W diagonal.
                // So 'JInverse = V * WInverse * Ut'.
                JacobiSVD svd = new JacobiSVD(_jacobian, JacobiSVD.ComputationOptions.ComputeThinU | JacobiSVD.ComputationOptions.ComputeThinV);

                _svdU = svd.GetMatrixV();
                _svdV = svd.GetMatrixU();
                _svdW = svd.GetSingularValues();
            }

            if (_useSDLS)
                InvertSDLS();
            else
                InvertDLS();
        }
        /// <summary>
        ///     Compute the 'dampeds' least squares (DLS) pseudo inverse of J.
        /// </summary>
        /// <remarks>
        ///     Since J is usually not invertible (Most of the time it isn't even squared), the pseudo-inverse is used.
        ///     This gives us the least squares solution.
        ///
        ///     This is fine when the J * Jt is of full rank.
        ///     When J * Jt is near to singular, the least squares inverse
        ///      tries to minimise | J(dTheta) - dX |
        ///      and doesn't try to minimize dTheta, resulting in erratic changes in angle.
        ///     The damped least squares (DLS) minimises | dTheta | to try and reduce this erratic behaviour.
        ///
        ///     The selectively damped least squares (SDLS) is used here instead of the DLS.
        ///     The SDLS damps individual singular values, instead of using a single damping term.
        /// </remarks>
        private void InvertSDLS()
        {
            float maxAngleChange = IKMath.PI / 4.0f;
            float epsilon = 1e-10f;

            _dTheta.SetZeros();
            _minDamp = 1.0f;

            for (int i = 0; i < _dofCount; ++i)
            {
                _norm[i] = 0.0f;

                for (int j = 0; j < _taskSize; j += 3)
                {
                    float n =
                        _jacobian[j, i] * _jacobian[j, i]
                        + _jacobian[j + 1, i] * _jacobian[j + 1, i]
                        + _jacobian[j + 2, i] * _jacobian[j + 2, i];

                    _norm[i] += IKMath.Sqrt(n);
                }
            }


            for (int i = 0; i < _svdW.GetSize(); ++i)
            {
                if (_svdW[i] <= epsilon)
                    continue;

                float wInv = 1.0f / _svdW[i];
                float alpha = 0.0f;
                float n = 0.0f;

                // Compute alpha and n.
                for (int j = 0; j < _svdU.GetRowCount(); j += 3)
                {
                    _alpha += _svdU[j, i] * _beta[j] + _svdU[j + 1, i] * _beta[j + 1] + _svdU[j + 2, i] * _beta[j + 2];

                    // Note: For 1 end effector, 'n' will always be 1 since U is orthogonal.
                    //  We could then optimise this to account for this fact, if desired.
                    n += IKMath.Sqrt(_svdU[j, i] * _svdU[j, i] + _svdU[j + 1, i] * _svdU[j + 1, i] + _svdU[j + 2, i] * _svdU[j + 2, i]);
                }
                alpha *= wInv;


                // Compute m, dTheta, and maxDTheta.
                float m = 0.0f, maxDTheta = 0.0f, absDTheta;
                for (int j = 0; j < _dTheta.GetSize(); ++j)
                {
                    float v = _svdV[j, i];
                    m += IKMath.Abs(v) * _norm[j];

                    // Compute temporary dThetas.
                    _dThetaTemp[j] = v * alpha;

                    // Find largest absolute dTheta, multiplying with weight to prevent unnecessary damping.
                    absDTheta = IKMath.Abs(_dThetaTemp[j]) * _weightSqrt[j];
                    if (absDTheta > maxDTheta)
                        maxDTheta = absDTheta;
                }

                m *= wInv;

                // Compute damping term and damp the DThetas.
                float gamma = maxAngleChange;
                if (n < m)
                    gamma *= n / m;

                float damp = (gamma < maxDTheta) ? gamma / maxDTheta : 1.0f;

                for (int j = 0; j < _dTheta.GetSize(); ++j)
                {
                    // We multiply by 0.8 so that even if there is some oscillation the system can converge (For joint limits).
                    // Also, its better to go a little to slow than to far.

                    float dofDamp = IKMath.Min(damp / _weight[j], 1.0f);
                    _dTheta[j] += 0.8f * dofDamp * _dThetaTemp[j];
                }

                if (damp < _minDamp)
                    _minDamp = damp;
            }


            // Apply weighting & prevent from doing angle updates with angles outside the max angle change.
            float maxAngle = 0.0f, absAngle;
            for (int i = 0; i < _dofCount; ++i)
            {
                _dTheta[i] *= _weight[i];
                absAngle = IKMath.Abs(_dTheta[i]);

                if (absAngle > maxAngle)
                    maxAngle = absAngle;
            }

            if (maxAngle > maxAngleChange)
            {
                float damp = maxAngleChange / (maxAngleChange + maxAngle);

                for (int j = 0; j < _dofCount; ++j)
                    _dTheta[j] *= damp;
            }
        }
        /// <summary>
        ///     Computes the damped least suqres (DLS) inverse of the pseudo inverse.
        ///     Computes the damping term lambda.
        /// </summary>    
        /// <remarks>
        ///     Note that when lambda is zero this is equivalent to the least squares solution.
        ///     This is fine when the JJT is of full rank.
        ///     
        ///     When JJT is near to singular, the least squares inverse tries to minimise | J(dTheta) - DX |
        ///      and doesn't try to minimize dTheta.
        ///      
        ///     This results in erratic changes in angle.
        ///     Damped least suqre minimises | dTheta| to try and reduce this erratic behaviour.
        ///     
        ///     We don't want to use the damped solution everywhere so we only increase lambda from zero as we approach a singularity.
        /// </remarks>    
        private void InvertDLS()
        {
            // Find the smallest non-zero W value.
            // Anything below epsilon is treated as zero.
            float epsilon = 1e-10f;
            float maxAngleChange = 0.1f;
            float xLength = IKMath.Sqrt(Vector.Dot(_beta, _beta));

            float wMin = float.MaxValue;

            for (int i = 0; i < _svdW.GetSize(); ++i)
                if (_svdW[i] > epsilon && _svdW[i] < wMin)
                        wMin = _svdW[i];
            
            // Compute the lambda damping term.
            float d = xLength / maxAngleChange;
            float lambda = wMin <= d / 2.0f
                ? d / 2.0f
                : wMin < d
                    ? IKMath.Sqrt(wMin * (d - wMin))
                    : 0.0f;
            lambda = IKMath.Min(lambda * lambda, 10.0f);


            // Immediately multiply with Beta so we can do matrix * vector products,
            //  rather than matrix * matrix products.
            _svdUBeta = _svdU.GetTranspose() * _beta;

            _dTheta.SetZeros();
            for (int i = 0; i < _svdW.GetSize(); i++)
            {
                if (_svdW[i] <= epsilon)
                    continue;

                float wInv = _svdW[i] / (_svdW[i] * _svdW[i] + lambda);

                // Compute V * WInverse * Ut * Beta
                _svdUBeta[i] *= wInv;
                for (int j = 0; j < _dTheta.GetSize(); ++j)
                    _dTheta[j] += _svdV[j, i] * _svdUBeta[i];
            }

            // Apply weighting.
            for (int i = 0; i < _dTheta.GetSize(); ++i)
                _dTheta *= _weight[i];
        }


        public float AngleUpdate(int dofId) => _dTheta[dofId];
        public float AngleUpdateNorm()
        {
            float maxValue = 0.0f, absDTheta;
            for (int i = 0; i < _dTheta.GetSize(); i++)
            {
                absDTheta = IKMath.Abs(_dTheta[i] * _dNormWeight[i]);

                if (absDTheta > maxValue)
                    maxValue = absDTheta;
            }

            return maxValue;
        }


        /// <summary>
        ///     Degree of Freedom locking for the inner clamping loop.
        /// </summary>
        /// <param name="dofId"></param>
        /// <param name="delta"></param>
        public void Lock(int dofId, float delta)
        {
            for (int i = 0; i < _taskSize; i++)
            {
                _beta[i] -= _jacobian[i, dofId] * delta;
                _jacobian[i, dofId] = 0.0f;
            }

            _norm[dofId] = 0.0f;
            _dTheta[dofId] = 0.0f;
        }


        // Secondary Task.
        public bool ComputeNullProjection()
        {
            const float epsilon = 1e-10f;

            // Compute null space projection based on v;
            int rank = 0;
            for (int i = 0; i < _svdW.GetSize(); ++i)
                if (_svdW[i] > epsilon)
                    ++rank;

            if (rank < _taskSize)
                return false;

            Matrix basis = new Matrix(_svdV.GetRowCount(), rank);
            int b = 0;
            for (int i = 0; i < _svdW.GetSize(); ++i)
            {
                if (_svdW[i] > epsilon)
                {
                    for (int j = 0; j < _svdV.GetRowCount(); j++)
                        basis[j, b] = _svdV[j, i];
                
                    ++b;
                }
            }

            _nullspace = basis * basis.GetTranspose();
            for (int i = 0; i < _nullspace.GetRowCount(); i++)
                for (int j = 0; j < _nullspace.GetColumnCount(); j++)
                    _nullspace[i, j] = i == j ? 1.0f - _nullspace[i, j] : -_nullspace[i, j];

            return true;
        }

        public void Restrict(Vector dTheta, Matrix nullSpace)
        {
            // Subtract the part already moved by a higher task from beta.
            _beta = _beta - _jacobian * dTheta;

            // Note: Should we be using the magnitude of the unrestricted jacobian for SDLS?
            // Project jacobian onto the nullspace of the higher priority task.
            _jacobian = _jacobian * nullSpace;
        }
        public void SubTask(IKJacobian jacobian)
        {
            if (!ComputeNullProjection())
                return;

            // Restrict lower priority jacobian.
            jacobian.Restrict(_dTheta, _nullspace);

            // Add angle update from the lower priority.
            jacobian.Invert();

            // Damps secondary angles with minimum damping value from the SDLS
            //  to avoid shasking when the primary task is near a singularity.
            for (int i = 0; i < _dTheta.GetSize(); i++)
                _dTheta[i] = _dTheta[i] + /*_minDamp * */ jacobian.AngleUpdate(i);
        }
    }
}