using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gameplay.Animations
{
    // A Jacobian method for solving a n-length IK target, based on Blender's source code.
    /* Reference Links:
     * - https://github.com/dfelinto/blender/blob/master/intern/iksolver/intern/IK_Solver.cpp#L386
     * - https://github.com/dfelinto/blender/blob/master/intern/iksolver/intern/IK_QJacobianSolver.cpp

     * Notes:
     * - Vector3d.norm() returns their magnitude (E.g. UnityEngine.Vector3.magnitude)
    */
    public class JacobianIKSolver
    {
        private IKJacobian _jacobian;
        private IKJacobian _jacobianSub;
        private bool _secondaryEnabled;

        private List<IKSegment> _segments;

        private Affine3 _rootMatrix;


        private Vector3 _goal;

        private bool _usePoleConstraint;
        private bool _getPoleAngle;

        private Vector3 _poleGoal;
        private float _poleAngle;
        private IKSegment _poleTip;


        /// <summary>
        ///     Setup the Jacobian Solver.<br/>    
        ///     Call Setup once before Solve. If Setup fails, don't call Solve.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="tasks"></param>
        /// <returns> True if we have any tasks to perform (Confirm).</returns>
        public bool Setup(IKSegment root, List<IKTask> tasks)
        {
            // Clear previous data.
            _segments.Clear();
            AddSegmentList(root);


            // Assign each segment a unique ID for the Jacobian algorithm.
            int numDegreesOfFreedom = 0;

            foreach(IKSegment segment in _segments)
            {
                segment.SetDoFId(numDegreesOfFreedom);
                numDegreesOfFreedom += segment.GetNumberOfDoF();
            }

            if (numDegreesOfFreedom == 0)
                return false;

            // Compute task ids and assign weights to each task.
            int primarySize = 0, primary = 0;
            int secondarySize = 0, secondary = 0;
            float primaryWeight = 0.0f, secondaryWeight = 0.0f;
            
            foreach(IKTask task in tasks)
            {
                if (task.GetIsPrimary())
                {
                    task.SetId(primarySize);
                    primarySize += task.GetSize();
                    primaryWeight += task.GetWeight();
                    ++primary;
                }
                else
                {

                    task.SetId(secondarySize);
                    secondarySize += task.GetSize();
                    secondaryWeight += task.GetWeight();
                    ++secondary;
                }
            }

            if (primarySize == 0 || IKMath.FuzzyZero(primaryWeight))
                return false;

            _secondaryEnabled = secondary > 0;


            // Rescale the weights of our tasks to total 1.
            float primaryRescale = 1.0f / primaryWeight;
            float secondaryRescale = IKMath.FuzzyZero(secondaryWeight) ? 0.0f : 1.0f / secondaryWeight;

            foreach(IKTask task in tasks)
            {
                if (task.GetIsPrimary())
                    task.SetWeight(task.GetWeight() * primaryRescale);
                else
                    task.SetWeight(task.GetWeight() * secondaryRescale);
            }


            // Set matrix sizes.
            _jacobian.ArmMatrices(numDegreesOfFreedom, primarySize);
            if (secondary > 0)
                _jacobianSub.ArmMatrices(numDegreesOfFreedom, secondarySize);

            // Set Degree Of Freedom weights.
            foreach(IKSegment segment in _segments)
            {
                for (int i = 0; i < segment.GetNumberOfDoF(); i++)
                {
                    _jacobian.SetDoFWeight(segment.GetDoFId() + i, segment.GetWeight(i));
                }
            }

            return true;
        }
        private void AddSegmentList(IKSegment root)
        {
            _segments.Add(root);

            for(IKSegment child = root.GetChild(); child != null; child = child.GetSibling())
                AddSegmentList(child);
        }


        public void SetPoleVectorConstraint(IKSegment tip, Vector3 goal, Vector3 poleGole, float poleAngle, bool getAngle)
        {
            _usePoleConstraint = true;
            _poleTip = tip;
            _goal = goal;
            _poleGoal = goal;
            _poleAngle = getAngle ? 0.0f : poleAngle;
            _getPoleAngle = getAngle;
        }
        public float GetPoleAngle() => _poleAngle;

        /// <remarks>
        ///     This function should be called before and after solving.<br/>
        ///     Calling before solving gives predictable solutions by rotating towards the solution.<br/>
        ///     Calling after solving ensures the solution is exact.
        /// </remarks>
        public void ConstrainPoleVector(IKSegment root, List<IKTask> tasks)
        {
            if (!_usePoleConstraint)
                return;

            // Disable the pole vector constraint if we have multiple position tasks.
            int positionTasks = 0;
            foreach (IKTask task in tasks)
                if (task.IsPositionTask())
                    ++positionTasks;

            if (positionTasks >= 2)
            {
                _usePoleConstraint = false;
                return;
            }


            // Get positions & rotations.
            root.UpdateTransform(_rootMatrix);

            Vector3 rootPos = root.GetGlobalStart();
            Vector3 endPos = root.GetGlobalEnd();
            Matrix3x3 rootBasis = root.GetGlobalTransform().linear();


            // Construct "LookAt" matrices based on a direction and an up vector.
            Vector3 dir = (endPos - rootPos).normalized;
            Vector3 rootX = rootBasis.GetColumn(0);
            Vector3 rootZ = rootBasis.GetColumn(2);
            Vector3 up = rootX * Mathf.Cos(_poleAngle) + rootZ * Mathf.Sin(_poleAngle);


            // In post (When _getPoleAngle is false), don't rotate towards the goal but only correct the pole's up.
            Vector3 poleDir = _getPoleAngle ? dir : (_goal - rootPos).normalized;
            Vector3 poleUp = (_poleGoal - rootPos).normalized;

            Matrix3x3 mat = new(), poleMat = new();

            mat.SetRow(0, Vector3.Cross(dir, up).normalized);
            mat.SetRow(1, Vector3.Cross(mat.GetRow(0), dir));
            mat.SetRow(2, -dir);

            poleMat.SetRow(0, Vector3.Cross(poleDir, poleUp).normalized);
            poleMat.SetRow(1, Vector3.Cross(poleMat.GetRow(0), poleDir));
            poleMat.SetRow(2, -poleDir);

            if (_getPoleAngle)
            {
                // Compute the pole angle to rotate towards the target.
                _poleAngle = Vector3.Angle(mat.GetRow(1), poleMat.GetRow(1));

                float dot = Vector3.Dot(rootZ, mat.GetRow(1) * Mathf.Cos(_poleAngle) + mat.GetRow(0) * Mathf.Sin(_poleAngle));
                if (dot > 0.0f)
                    _poleAngle = -_poleAngle;

                // Solve again, with the pole angle we just computed.
                _getPoleAngle = false;
                ConstrainPoleVector(root, tasks);
            }
            else
            {
                // Set the root matrix as the difference between the current and desired rotation based on the pole vector constraint.
                // Transpose is used rather than inverse because we have orthogonal matrices anyway, and inverse would cause a NAN for a singular matrix.
                Affine3 trans = new();
                trans.SetLinear(poleMat.transpose() * mat);
                trans.SetTranslation(new Vector3(0.0f, 0.0f, 0.0f));
                _rootMatrix = trans * _rootMatrix;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="norm"></param>
        /// <returns> True if another inner iteration is needed.</returns>
        private bool UpdateAngles(ref float norm)
        {
            // Assign each segment a unique id for the jacobian.
            IKSegment minSegment = null;
            float minAbsDelta = float.MaxValue, absDelta;
            Vector3 delta = Vector3.zero, minDelta = Vector3.zero;
            bool isLocked = false;
            bool[] clamp = new bool[3];
            int minDoFIndex = 0;

            foreach (IKSegment segment in _segments)
            {
                if (!segment.UpdateAngle(_jacobian, ref delta, ref clamp))
                    // Angle updated without violating any limits.
                    continue;

                // One of the angle limits was violated.
                // Angles where the clamped position is the same as before should be locked immediately.
                // Of other violations, the most violating angle is remembered.
                for (int i = 0; i < segment.GetNumberOfDoF(); i++)
                {
                    if (!clamp[i] || !segment.IsLocked(i))
                        continue;

                    absDelta = Mathf.Abs(delta[i]);
                    if (absDelta < IKMath.IK_EPSILON)
                        // Clamped position is the same as before. Lock this angle.
                    {
                        segment.Lock(i, _jacobian, delta);
                        isLocked = true;
                    }
                    else if (absDelta < minAbsDelta)
                        // Remember the most violating angle.
                    {
                        minAbsDelta = absDelta;
                        minDelta = delta;
                        minSegment = segment;
                        minDoFIndex = i;
                    }
                }
            }


            // Lock the most violating angle.
            if (minSegment != null)
            {
                minSegment.Lock(minDoFIndex, _jacobian, minDelta);
                isLocked = true;

                if (minAbsDelta > norm)
                    norm = minAbsDelta;
            }


            if (!isLocked)
            {
                foreach (IKSegment segment in _segments)
                {
                    segment.Unlock();
                    segment.ApplyAngleUpdates();
                }
            }

            return isLocked;
        }


        private float ComputeScale()
        {
            float length = 0.0f;

            foreach (IKSegment segment in _segments)
                length += segment.GetMaxExtension();

            return length == 0.0f ? 1.0f : 1.0f / length;
        }
        private void Scale(float scale, List<IKTask> tasks)
        {
            foreach (IKTask task in tasks)
                task.Scale(scale);

            foreach (IKSegment segment in _segments)
                segment.Scale(scale);

            _rootMatrix.SetTranslation(_rootMatrix.GetTranslation() * scale);
            _goal *= scale;
            _poleGoal *= scale;
        }


        /// <summary>
        ///     
        /// </summary>
        /// <param name="root"></param>
        /// <param name="tasks"></param>
        /// <param name=""></param>
        /// <param name="maxIterations"></param>
        /// <returns> True if we successfully solved the IK, False if we failed.</returns>
        public bool Solve(IKSegment root, List<IKTask> tasks, int maxIterations)
        {
            float scale = ComputeScale();
            bool isSolved = false;

            Scale(scale, tasks);

            ConstrainPoleVector(root, tasks);

            root.UpdateTransform(_rootMatrix);

            // Iterate.
            for(int iterations = 0; iterations < maxIterations; ++iterations)
            {
                // Update transform.
                root.UpdateTransform(_rootMatrix);

                // Compute jacobian.
                foreach(IKTask task in tasks)
                    task.ComputeJacobian(task.GetIsPrimary() ? _jacobian : _jacobianSub);
                
                float norm = 0.0f;
                do
                {
                    try
                    {
                        _jacobian.Invert();
                        if (_secondaryEnabled)
                            _jacobian.SubTask(_jacobianSub);
                    }
                    catch
                    {
                        Debug.LogError("IK Exception");
                        return false;
                    }

                    // Update angles and check limits.
                } while(UpdateAngles(ref norm));


                foreach(IKSegment segment in _segments)
                    segment.Unlock();
                   
                // Calculate the angle update norm.
                float maxNorm = _jacobian.AngleUpdateNorm();
                if (maxNorm > norm)
                    norm = maxNorm;

                // Check for convergence.
                if (norm < 1E-3 && iterations > 10)
                {
                    isSolved = true;
                    break;
                }
            }

            if (_usePoleConstraint)
                root.PrependBasis(_rootMatrix.GetLinear());

            // Reverse scale.
            Scale(1.0f / scale, tasks);

            return isSolved;
        }
    }


    public abstract class SVDBase
    {

    }
    public class JacobiSVD : SVDBase
    {
        private bool _isInitialised;
        private bool _isAllocated;

        private int _rows;
        private int _columns;
        private int _diagonalSize;
        private uint _computationOptions;

        private bool _computeFullU, _computeThinU;
        private bool _computeFullV, _computeThinV;

        private MatrixX _matrixU, _matrixV;
        private MatrixX _scaledMatrix, _workMatrix;



        public JacobiSVD(int rows, int columns, uint computationOptions = 0)
        {
            Allocate(rows, columns, computationOptions);
        }
        public JacobiSVD(Matrix3x3 matrix, uint computationOptions = 0)
        {
            Compute(matrix, computationOptions);
        }


        private void Allocate(int rows, int columns, uint computationOptions)
        {
            Debug.Assert(rows >= 0 && columns >= 0);

            if (_isAllocated && rows == _rows && columns == _columns && computationOptions == _computationOptions)
                return; // Already allocated with the desired parameters.

            _rows = rows;
            _columns = columns;
            _isInitialised = false;
            _isAllocated = true;
            _computationOptions = computationOptions;

            _computeFullU = (computationOptions & ComputeFullU) != 0;
            _computeThinU = (computationOptions & ComputeThinU) != 0;
            _computeFullV = (computationOptions & ComputeFullV) != 0;
            _computeThinV = (computationOptions & ComputeThinV) != 0;

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
        public JacobiSVD Compute(MatrixX matrix, uint computationOptions)
        {
            Allocate(matrix.GetRowCount(), matrix.GetColumnCount(), computationOptions);

            // Used to reduce iterations which would worsen the precision of U and V as more rotations are accumulated.
            const float precision = 2.0f * float.Epsilon;

            // Limit for denormal numbers to be considered zero in order to avoid infinite loops.
            const float considerAsZero = ;

            // Scaling factor to reduce over/under-flows.
            float scale = ;

            // Step 1 - R-SVD: Use a QR decomposition to reduce to the case of a square matrix.
            if (_rows != _columns)
            {
                _scaledMatrix = matrix / scale;
                _qrPrecondMoreColumns.Run(this, _scaledMatrix);
                _qrPrecondMoreRows.Run(this, _scaledMatrix);
            }
            else
            {
                _workMatrix = matrix.GetBlock(0, 0, _diagonalSize, _diagonalSize) / scale;
                if (_computeFullU)      { _matrixU.SetIdentity(_rows, _rows); }
                else if (_computeThinU) { _matrixU.SetIdentity(_rows, _diagonalSize); }
                if (_computeFullV)      { _matrixV.SetIdentity(_columns, _columns); }
                else if (_computeThinV) { _matrixV.SetIdentity(_columns, _diagonalSize); }
            }


            // Step 2: The main Jacobi SVD iteration.
            var maxDiagEntry = _workMatrix.cwiseAbs().diagonal().maxCoeff();

            bool finished = false;
            while(!finished)
            {
                finished = true;

                // Do a sweep: For all index pairs (p,q), perform SVD of the corresponding 2x2 sub-matrix.
                for (int p = 1; p < _diagonalSize; ++p)
                {
                    for (int q = 0; q < p; ++q)
                    {
                        float threshold = Mathf.Max(considerAsZero, precision * maxDiagEntry);
                        if (Mathf.Abs(_workMatrix.Coeff(p, q)) > threshold || Mathf.Abs(_workMatrix.Coeff(q, p)) > threshold)
                        {
                            finished = false;

                            // Perform SVD decomposition of the 2x2 sub-matrix corresponding to indicies p,q to make it diagonal.
                            // The complex to real operation returns true if the updated 2x2 block is not already diagonal.
                            if (Run(_workMatrix, this, p, q, maxDiagEntry))
                            {

                            }
                        }
                    }
                }
            }


            // Step 3: The work matrix is now diagonal, so ensure it's positive so its diagonal entries are the singular values.


            // Step 4: Sort singular values in descending order and compute the number of non-zero singular values.


            _isInitialised = true;
            return this;
        }


        // Aka: 'internal::svd_precondition_2x2_block_to_be_real<MatrixType, QRPreconditioner>::run'
        private bool Run()
        {

        }
    }


    public class IKJacobian
    {
        private int _dofCount, _taskSize;
        private bool _isTranspose;

        // The jacobian matrix and its null-space projector.
        private MatrixX _jacobian, _jacobianTemp;
        private MatrixX _nullspace;

        // The vector of intermediate betas.
        private VectorX _beta;

        // The vector of computed angle changes.
        private VectorX _dTheta;
        private VectorX _dNormWeight;

        // Space required for SVD computation.
        private VectorX _svdW;
        private MatrixX _svdV;
        private MatrixX _svdU;

        private VectorX _svdUBeta;


        // Space required for SDLS.
        private bool _useSDLS;
        private VectorX _norm;
        private VectorX _dThetaTemp;
        private float _minDamp;

        // Null space task vector.
        private VectorX _alpha;

        // Degree of Freedom weighting.
        private VectorX _weight;
        private VectorX _weightSqrt;


        public IKJacobian()
        {
            _useSDLS = true;
            _minDamp = 0.0f;
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
            _jacobian.SetZero();

            _alpha.Resize(dofCount);
            _alpha.SetZero();

            _nullspace.Resize(dofCount, dofCount);

            _dTheta.Resize(dofCount);
            _dThetaTemp.Resize(dofCount);
            _dNormWeight.Resize(dofCount);

            _norm.Resize(dofCount);
            _norm.SetZero();

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
            _weightSqrt[dofId] = Mathf.Sqrt(weight);
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
                JacobiSVD svd = new JacobiSVD(_jacobian.transpose(), JacobiSVD.ComputeThinU | JacobiSVD.ComputeThinV);

                _svdU = svd.MatrixV();
                _svdV = svd.MatrixU();
                _svdW = svd.SingularValues();
            }
            else
            {
                // SVD will decompose J into U * W * Vt, with U,V orthogonal and W diagonal.
                // So 'JInverse = V * WInverse * Ut'.
                JacobiSVD svd = new JacobiSVD(_jacobian, JacobiSVD.ComputeThinU | JacobiSVD.ComputeThinV);

                _svdU = svd.MatrixV();
                _svdV = svd.MatrixU();
                _svdW = svd.SingularValues();
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
            float maxAngleChange = Mathf.PI / 4.0f;
            float epsilon = 1e-10f;

            _dTheta.SetZero();
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

                    _norm[i] += Mathf.Sqrt(n);
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
                    n += Mathf.Sqrt(_svdU[j, i] * _svdU[j, i] + _svdU[j + 1, i] * _svdU[j + 1, i] + _svdU[j + 2, i] * _svdU[j + 2, i]);
                }
                alpha *= wInv;


                // Compute m, dTheta, and maxDTheta.
                float m = 0.0f, maxDTheta = 0.0f, absDTheta;
                for (int j = 0; j < _dTheta.GetSize(); ++j)
                {
                    float v = _svdV[j, i];
                    m += Mathf.Abs(v) * _norm[j];

                    // Compute temporary dThetas.
                    _dThetaTemp[j] = v * alpha;

                    // Find largest absolute dTheta, multiplying with weight to prevent unnecessary damping.
                    absDTheta = Mathf.Abs(_dThetaTemp[j]) * _weightSqrt[j];
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

                    float dofDamp = Mathf.Min(damp / _weight[j], 1.0f);
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
                absAngle = Mathf.Abs(_dTheta[i]);

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
            float xLength = Mathf.Sqrt(VectorX.Dot(_beta, _beta));

            float wMin = float.MaxValue;

            for (int i = 0; i < _svdW.GetSize(); ++i)
                if (_svdW[i] > epsilon && _svdW[i] < wMin)
                        wMin = _svdW[i];
            
            // Compute the lambda damping term.
            float d = xLength / maxAngleChange;
            float lambda = wMin <= d / 2.0f
                ? d / 2.0f
                : wMin < d
                    ? Mathf.Sqrt(wMin * (d - wMin))
                    : 0.0f;
            lambda = Mathf.Min(lambda * lambda, 10.0f);


            // Immediately multiply with Beta so we can do matrix * vector products,
            //  rather than matrix * matrix products.
            _svdUBeta = _svdU.transpose() * _beta;

            _dTheta.SetZero();
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
                absDTheta = Mathf.Abs(_dTheta[i] * _dNormWeight[i]);

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

            MatrixX basis = new MatrixX(_svdV.GetRowCount(), rank);
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

            _nullspace = basis * basis.transpose();
            for (int i = 0; i < _nullspace.GetRowCount(); i++)
                for (int j = 0; j < _nullspace.GetColumnCount(); j++)
                    _nullspace[i, j] = i == j ? 1.0f - _nullspace[i, j] : -_nullspace[i, j];

            return true;
        }

        public void Restrict(VectorX dTheta, MatrixX nullSpace)
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

    public struct Affine3
    {

    }
    public struct Matrix3x3
    {
        // Row 0.
        float m00;
        float m01;
        float m02;

        // Row 1.
        float m10;
        float m11;
        float m12;

        // Row 2.
        float m20;
        float m21;
        float m22;


        public Matrix3x3(float m00, float m01, float m02, float m10, float m11, float m12, float m20, float m21, float m22)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m02 = m02;

            this.m10 = m10;
            this.m11 = m11;
            this.m12 = m12;

            this.m20 = m20;
            this.m21 = m21;
            this.m22 = m22;
        }
        public static Matrix3x3 Identity => new Matrix3x3(1.0f, 0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f);


        public float this[int row, int column]
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get
            {
                return this[row + column * 3];
            }
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                this[row + column * 3] = value;
            }
        }
        public float this[int index]
        {
            get
            {
                return index switch
                {
                    0 => m00,
                    1 => m10,
                    2 => m20,

                    3 => m01,
                    4 => m11,
                    5 => m21,

                    6 => m02,
                    7 => m12,
                    8 => m22,

                    _ => throw new System.IndexOutOfRangeException("Invalid matrix index!")
                };
            }
            set
            {
                switch (index)
                {
                    case 0: m00 = value; break;
                    case 1: m10 = value; break;
                    case 2: m20 = value; break;

                    case 3: m01 = value; break;
                    case 4: m11 = value; break;
                    case 5: m21 = value; break;

                    case 6: m02 = value; break;
                    case 7: m12 = value; break;
                    case 8: m22 = value; break;

                    default: throw new System.IndexOutOfRangeException("Invalid matrix index!");
                }
            }
        }


        public Vector3 GetColumn(int columnIndex)
        {
            return columnIndex switch
            {
                0 => new Vector3(m00, m10, m20),
                1 => new Vector3(m01, m11, m21),
                2 => new Vector3(m02, m12, m22),

                _ => throw new System.IndexOutOfRangeException("Invalid Column Index")
            };
        }
        public void SetColumn(int columnIndex, Vector3 newValues) => SetColumn(columnIndex, newValues.x, newValues.y, newValues.z);
        public void SetColumn(int columnIndex, float row0, float row1, float row2)
        {
            switch(columnIndex)
            {
                case 0: m00 = row0; m10 = row1; m20 = row2; break;
                case 1: m01 = row0; m11 = row1; m21 = row2; break;
                case 2: m02 = row0; m12 = row1; m22 = row2; break;

                default:
                    throw new System.IndexOutOfRangeException("Invalid Column Index");
            }
        }


        public Vector3 GetRow(int rowIndex)
        {
            return rowIndex switch
            {
                0 => new Vector3(m00, m01, m02),
                1 => new Vector3(m10, m11, m12),
                2 => new Vector3(m20, m21, m22),

                _ => throw new System.IndexOutOfRangeException("Invalid Row Index")
            };
        }
        public void SetRow(int rowIndex, Vector3 newValues) => SetRow(rowIndex, newValues.x, newValues.y, newValues.z);
        public void SetRow(int rowIndex, float col0, float col1, float col2)
        {
            switch (rowIndex)
            {
                case 0: m00 = col0; m01 = col1; m02 = col2; break;
                case 1: m10 = col0; m11 = col1; m12 = col2; break;
                case 2: m20 = col0; m21 = col1; m22 = col2; break;

                default:
                    throw new System.IndexOutOfRangeException("Invalid Row Index");
            }
        }
    }
    public struct MatrixX
    {
        private float[] _values;
        private int _rowCount, _columnCount;
        private int _totalCount => _rowCount * _columnCount;

        public MatrixX(int rowCount, int columnCount)
        {
            _rowCount = rowCount;
            _columnCount = columnCount;
            _values = new float[rowCount * columnCount];

            SetZero();
        }

        public float this[int row, int column]
        {
            get => this[row + column * row];
            set => this[row + column * row] = value;
        }
        public float this[int index]
        {
            get => _values[index];
            set => _values[index] = value;
        }


        public void Resize(int rowCount, int columnCount)
        {
            _rowCount = rowCount;
            _columnCount = columnCount;
            _values = new float[_totalCount];
        }
        public void SetZero()
        {
            for (int i = 0; i < _totalCount; i++)
                _values[i] = 0.0f;
        }
        public void SetOnes()
        {
            for (int i = 0; i < _totalCount; i++)
                _values[i] = 1.0f;
        }

        public int GetRowCount() => _rowCount;
        public int GetColumnCount() => _columnCount;


        public static VectorX operator *(MatrixX a, VectorX b)
        {
            if (a.GetRowCount() != b.GetSize())
                throw new System.ArithmeticException($"Cannot Multiply a Matrix{a.GetRowCount()}x{a.GetColumnCount()} with a Vector{b.GetSize()}");

            // Create an empty vector to hold our values (Initial values are 0).
            VectorX returnedVector = new VectorX(b.GetSize());

            // Multiply the two matrices together (Vectors are 1xX matrices).
            for(int row = 0; row < a.GetRowCount(); ++row)
                for(int column = 0; column < a.GetColumnCount(); ++column)
                    returnedVector[row] += a[row, column] * b[row];
                
            return returnedVector;
        }
        public static MatrixX operator *(MatrixX a, MatrixX b)
        {
            if (a.GetRowCount() != b.GetColumnCount() || a.GetColumnCount() != b.GetRowCount())
                throw new System.ArithmeticException($"Cannot Multiply a Matrix{a.GetRowCount()}x{a.GetColumnCount()} with a Matrix{b.GetRowCount()}x{b.GetColumnCount()}");

            // Create an empty vector to hold our values (Initial values are 0).
            MatrixX returnedMatrix = new MatrixX(a.GetRowCount(), b.GetRowCount());

            // Multiply the two matrices together (Vectors are 1xX matrices).
            for (int row = 0; row < a.GetRowCount(); ++row)
                for (int column = 0; column < a.GetColumnCount(); ++column)
                    returnedMatrix[row] += a[row, column] * b[column, row];

            return returnedMatrix;
        }
    }
    public struct VectorX : System.IEquatable<VectorX>, System.IEquatable<UnityEngine.Vector2>, System.IEquatable<UnityEngine.Vector3>
    {
        private float[] _values;
        private int _size;

        public VectorX(int size)
        {
            _values = new float[size];
            _size = size;

            SetZero();
        }
        public VectorX(VectorX other)
            : this(other._size)
        {
            for(int i = 0; i < _size; ++i)
                _values[i] = other._values[i];
        }


        public float this[int index]
        {
            get => index < _size ? _values[_size] : throw new System.IndexOutOfRangeException($"Invalid index '{index}' for VectorX with size '{_size}'");
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index < _size)
                    _values[_size] = value;
                else
                    throw new System.IndexOutOfRangeException($"Invalid index '{index}' for VectorX with size '{_size}'");
            }
        }
        public int GetSize() => _size;

        public void Resize(int newSize)
        {
            _values = new float[newSize];
            _size = newSize;
        }
        public void SetZero()
        {
            for(int i = 0; i < _size; ++i)
                _values[i] = 0.0f;
        }
        public void SetOnes()
        {
            for (int i = 0; i < _size; ++i)
                _values[i] = 1.0f;
        }


        public static float Dot(VectorX a, VectorX b)
        {

        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other)
        {
            return other switch
            {
                VectorX otherVectorX => Equals(otherVectorX),
                Vector2 otherVector2 => Equals(otherVector2),
                Vector3 otherVector3 => Equals(otherVector3),

                _ => false
            };
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(VectorX other)
        {
            if (_size != other._size)
                return false; // Sizes differ to the two cannot be equal.

            for(int i = 0; i < _size; ++i)
                if (_values[i] != other._values[i])
                    return false; // A value doesn't match.

            return true; // Size and all values match.
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2 other)
        {
            if (_size != 2)
                return false; // The size of the VectorX is incompatible with a Vector2.

            // Return true if both values match.
            return _values[0] == other.x && _values[1] == other.y;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector3 other)
        {
            if (_size != 3)
                return false; // The size of the VectorX is incompatible with a Vector3.

            // Return true if all values match.
            return _values[0] == other.x && _values[1] == other.y && _values[2] == other.z;
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator +(VectorX a, float b)
        {
            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] + b;

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator -(VectorX a, float b)
        {
            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] -+ b;

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator *(VectorX a, float b)
        {
            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] * b;

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator /(VectorX a, float b)
        {
            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] / b;

            return returnedVector;
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator +(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} with a Vector{b.GetSize()}");

            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] + b[i];

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator -(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} with a Vector{b.GetSize()}");

            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] - b[i];

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator *(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} with a Vector{b.GetSize()}");

            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] * b[i];

            return returnedVector;
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static VectorX operator /(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} with a Vector{b.GetSize()}");

            VectorX returnedVector = new VectorX(a);
            for (int i = 0; i < a._size; ++i)
                returnedVector[i] = a[i] / b[i];

            return returnedVector;
        }
    }

    public class Bone
    {
        public readonly Transform Head;
        public readonly Transform Tail;

        // If true, then 'Tail' is the immediate child of 'Head' in the transform hierarchy.
        // This simplifies some calculations as position, rotation, and scale changes are automatically propogated by Unity.
        private readonly bool _tailIsChild;

        public readonly float Length;

        public Vector3 InitialPosition { get; private set; }
        public Quaternion InitialRotation { get; private set; }


        public Bone(Transform head, Transform tail)
        {
            Head = head;
            Tail = tail;
            _tailIsChild = tail.IsChildOf(head, checkDepth: 1);

            Length = CalculateHeadToTailVector().magnitude;

            UpdateInitialValues();
        }
        public void UpdateInitialValues() => UpdateInitialValues(Head.position, _tailIsChild ? Head.rotation : Quaternion.LookRotation(CalculateHeadToTailVector().normalized, Head.up));
        public void UpdateInitialValues(Vector3 newInitialPosition, Quaternion newInitialRotation)
        {
            InitialPosition = newInitialPosition;
            InitialRotation = newInitialRotation;
        }

        private Vector3 CalculateHeadToTailVector() => Tail.position - Head.position;


        public void SetPosition(Vector3 newPosition)
        {
            /*if (!_tailIsChild)
            {
                Vector3 cachedHeadToTail = CalculateHeadToTailVector();
                Head.position = newPosition;
                Tail.position = Head.position + cachedHeadToTail;
            }
            else*/
                Head.position = newPosition;
        }

        public void SetRotation(Quaternion newRotation)
        {
            /*if (!_tailIsChild)
            {
                Quaternion cachedRotationDifference = Quaternion.Inverse(Head.rotation) * Tail.rotation;    // Rotation applied to the head to get to the tail.
                Head.rotation = newRotation;
                Tail.rotation = Head.rotation * cachedRotationDifference;
            }
            else*/
                Head.rotation = newRotation;
        }

        public void AddRotation(Quaternion quaternion) => SetRotation(Head.rotation * quaternion);
        public void SubtractRotation(Quaternion quaternion) => SetRotation(Quaternion.Inverse(Head.rotation) * quaternion);

        public void SetAngleAxis(float angleDeg, Vector3 axis) => SetRotation(Quaternion.AngleAxis(angleDeg, axis));
    }
    public class IKChain
    {
        public int BoneCount { get; private set; }
        public Bone[] Bones { get; private set; }
        public float MaxLength { get; private set; }


        public IKChain(params Bone[] bones)
        {
            BoneCount = bones.Length;
            Bones = bones;

            // Calculate and set the max length.
            MaxLength = 0.0f;
            for(int i = 0; i < bones.Length; i++)
                MaxLength += bones[i].Length;
        }


        public Vector3 Start => Bones[0].Head.position;
        public float SqrMaxLength => MaxLength * MaxLength;
    }

    public static class IKSolver
    {
        /// <summary>
        ///     Aligns the first bone to the target axis, then resets rotation (And optionally position) of all sub-links from the root.
        /// </summary>
        /// <param name="chain"> The <see cref="IKChain"/> to affect.</param>
        /// <param name="target"> The Target Transform we're aiming towards.</param>
        /// <param name="resetPosition"> Should we reset the chain elements positions too?</param>
        public static void FullDirection(IKChain chain, Transform target, bool resetPosition = false)
        {
            // Point the chain in the target's direction.
            IKSolver.AimDirection(chain, target, true);

            // Reset rotation (And optionally position) on all bones except the root.
            for(int i = 1; i < chain.BoneCount; ++i)
            {
                chain.Bones[i].SetRotation(chain.Bones[i].InitialRotation);

                if (resetPosition)
                {
                    chain.Bones[i].SetPosition(chain.Bones[i].InitialPosition);
                }
            }
        }

        /// <summary>
        ///     Aligns the first link of the chain towards the target axis.
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="target"></param>
        /// <param name="doOffset"></param>
        public static void AimDirection(IKChain chain, Transform target, bool doOffset = false)
        {
            chain.Bones[0].SetRotation(target.rotation);

            if (doOffset)
            {
                Quaternion forwardToUp = Quaternion.AngleAxis(90.0f * Mathf.Rad2Deg, Vector3.left);
                chain.Bones[0].AddRotation(forwardToUp);
            }
        }
    }
}