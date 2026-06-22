using System.Collections.Generic;
using EigenPort;

namespace Gameplay.Animations
{
    // A Jacobian method for solving a n-length IK target, based on Blender's source code.
    /* Reference Links:
     * - https://github.com/dfelinto/blender/blob/master/intern/iksolver/intern/IK_Solver.cpp#L386
     * - https://github.com/dfelinto/blender/blob/master/intern/iksolver/intern/IK_QJacobianSolver.cpp

     * Notes:
     * - Vector3d.norm() returns their magnitude (E.g. UnityEngine.Vector3.magnitude)
    */
    public class IKJacobianSolver
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


        public IKJacobianSolver()
        {
            _jacobian = new();
            _jacobianSub = new();
            _secondaryEnabled = false;

            _segments = new List<IKSegment>();

            _rootMatrix = new();
            _goal = new();
            _poleGoal = new();
            _poleTip = null;
        }


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
            Matrix3x3 rootBasis = root.GetGlobalTransform().GetLinear();


            // Construct "LookAt" matrices based on a direction and an up vector.
            Vector3 dir = (endPos - rootPos).normalized;
            Vector3 rootX = rootBasis.GetColumn(0);
            Vector3 rootZ = rootBasis.GetColumn(2);
            Vector3 up = rootX * IKMath.Cos(_poleAngle) + rootZ * IKMath.Sin(_poleAngle);


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

                float dot = Vector3.Dot(rootZ, mat.GetRow(1) * IKMath.Cos(_poleAngle) + mat.GetRow(0) * IKMath.Sin(_poleAngle));
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
                trans.SetLinear(poleMat.GetTranspose() * mat);
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

                    absDelta = IKMath.Abs(delta[i]);
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
                    //try
                    //{
                        _jacobian.Invert();
                        if (_secondaryEnabled)
                            _jacobian.SubTask(_jacobianSub);
                    /*}
                    catch (System.Exception e)
                    {
                        UnityEngine.Debug.LogAssertion("IK Exception\n" + e.Message);
                        return false;
                    }*/

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
}