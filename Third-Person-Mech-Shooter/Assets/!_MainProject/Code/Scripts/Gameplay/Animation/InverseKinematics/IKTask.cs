using EigenPort;

namespace Gameplay.Animations
{
    public abstract class IKTask
    {
        protected int _id;
        protected int _size;
        protected bool _isPrimary;
        protected bool _isActive;
        protected IKSegment _segment;
        protected float _weight;


        public int GetId() => _size;
        public void SetId(int id) => _id = id;

        public int GetSize() => _size;
        public bool GetIsPrimary() => _isPrimary;
        public bool GetIsActive() => _isActive;

        public float GetWeight() => _weight;
        public void SetWeight(float newWeight) => _weight = newWeight;


        public IKTask(int size, bool isPrimary, bool isActive, IKSegment segment)
        {
            _size = size;
            _isPrimary = isPrimary;
            _isActive = isActive;
            _segment = segment;
            _weight = 1.0f;
        }


        public abstract void ComputeJacobian(IKJacobian jacobian);
        public abstract float GetDistance();

        public virtual bool IsPositionTask() => false;
        public virtual void Scale(float newScale) { }
    }


    public class IKPositionTask : IKTask
    {
        private Vector3 _goal;
        private float _clampLength;

        public IKPositionTask(bool isPrimary, IKSegment segment, Vector3 goal)
            : base(3, isPrimary, true, segment)
        {
            _goal = goal;

            // Compute the clamp length.
            int segmentCount = 0;
            _clampLength = 0.0f;

            for(IKSegment parent = _segment; parent != null; parent = parent.GetParent())
            {
                _clampLength += parent.GetMaxExtension();
                ++segmentCount;
            }

            _clampLength /= 2.0f * segmentCount;
        }


        public override void ComputeJacobian(IKJacobian jacobian)
        {
            // Compute beta.
            Vector3 pos = _segment.GetGlobalEnd();

            Vector3 dPos = _goal - pos;

            // Clamp the vector to our max length.
            float length = dPos.magnitude;
            if (length > _clampLength)
                dPos = (_clampLength / length) * dPos;

            jacobian.SetBetas(_id, _size, _weight * dPos);

            // Compute Derivatives.
            for(IKSegment parent = _segment; parent != null; parent = parent.GetParent())
            {
                Vector3 p = parent.GetGlobalStart() - pos;

                for(int i = 0; i < parent.GetNumberOfDoF(); ++i)
                {
                    Vector3 axis = parent.Axis(i) * _weight;

                    if (parent.IsTranslationalSegment())
                    {
                        jacobian.SetDerivatives(_id, parent.GetDoFId() + i, axis, 1e2f);
                    }
                    else
                    {
                        Vector3 pa = Vector3.Cross(p, axis);
                        jacobian.SetDerivatives(_id, parent.GetDoFId() + i, pa, 1e0f);
                    }
                }
            }
        }
        public override float GetDistance()
        {
            Vector3 pos = _segment.GetGlobalEnd();
            Vector3 dPos = _goal - pos;
            return dPos.magnitude;
        }


        public override bool IsPositionTask() => true;
        public override void Scale(float newScale)
        {
            _goal *= newScale;
            _clampLength *= newScale;
        }
    }


    public class IKOrientationTask : IKTask
    {
        private Matrix3x3 _goal;
        private float _distance;

        public IKOrientationTask(bool isPrimary, IKSegment segment, Matrix3x3 goal)
            : base(3, isPrimary, true, segment)
        {
            _goal = goal;
            _distance = 0.0f;
        }


        public override void ComputeJacobian(IKJacobian jacobian)
        {
            // Compute Betas.
            Matrix3x3 rot = _segment.GetGlobalTransform().GetLinear();

            Matrix3x3 dRotMatrix = Matrix3x3.TryCreateFromMatrix((_goal * rot.GetTranspose()).GetTranspose());

            Vector3 dRot = -0.5f * new Vector3(dRotMatrix[2, 1] - dRotMatrix[1, 2], dRotMatrix[0, 2] - dRotMatrix[2, 0], dRotMatrix[1, 0] - dRotMatrix[0, 1]);
            _distance = dRot.magnitude;

            jacobian.SetBetas(_id, _size, _weight * dRot);


            // Compute Derivatives.
            for (IKSegment segment = _segment; segment != null; segment = segment.GetParent())
            {
                for (int i = 0; i < segment.GetNumberOfDoF(); i++)
                {
                    if (segment.IsTranslationalSegment())
                    {
                        jacobian.SetDerivatives(_id, segment.GetDoFId() + i, new Vector3(0.0f, 0.0f, 0.0f), 1e2f);
                    }
                    else
                    {
                        Vector3 axis = segment.Axis(i) * _weight;
                        jacobian.SetDerivatives(_id, segment.GetDoFId() + i, axis, 1e0f);
                    }
                }
            }
        }
        public override float GetDistance() => _distance;
    }


    // Implementation not finished.
    public class IKCentreOfMassTask : IKTask
    {
        private Vector3 _goalCentre;
        float _totalMassInverse;
        float _distance;


        public IKCentreOfMassTask(bool isPrimary, IKSegment segment, Vector3 goalCentre)
            : base(3, isPrimary, true, segment)
        {
            _goalCentre = goalCentre;

            _totalMassInverse = ComputeTotalMass(segment);
            if (!IKMath.FuzzyZero(_totalMassInverse))
                _totalMassInverse = 1.0f / _totalMassInverse;
        }
        private float ComputeTotalMass(IKSegment segment)
        {
            float mass = /*segment.GetMass()*/1.0f;

            for(IKSegment child = segment.GetChild(); child != null; child = child.GetSibling())
                mass += ComputeTotalMass(child);

            return mass;
        }


        public override void ComputeJacobian(IKJacobian jacobian)
        {
            Vector3 centre = ComputeCentre(_segment) * _totalMassInverse;

            // Compute beta.
            Vector3 dPos = _goalCentre - centre;
            _distance = dPos.magnitude;

            #if false
            if (_distance > _clampLength)
                dPos = (_clampLength / _distance) * dPos;
            #endif

            jacobian.SetBetas(_id, _size, _weight * dPos);


            // Compute Derivatives.
            JacobianSegment(jacobian, centre, _segment);
        }
        private Vector3 ComputeCentre(IKSegment segment)
        {
            Vector3 centre = /*segment.GetMass * */segment.GetGlobalStart();

            for (IKSegment child = segment.GetChild(); child != null; child = child.GetSibling())
                centre += ComputeCentre(child);

            return centre;
        }
        private void JacobianSegment(IKJacobian jacobian, Vector3 centre, IKSegment segment)
        {
            Vector3 p = centre - _segment.GetGlobalStart();

            for (int i = 0; i < _segment.GetNumberOfDoF(); ++i)
            {
                Vector3 axis = _segment.Axis(i) * _weight;
                axis *= /*_segment.GetMass() * */_totalMassInverse;

                if (segment.IsTranslationalSegment())
                {
                    jacobian.SetDerivatives(_id, segment.GetDoFId() + i, axis, 1e2f);
                }
                else
                {
                    Vector3 pa = Vector3.Cross(axis, p);
                    jacobian.SetDerivatives(_id, segment.GetDoFId() + i, pa, 1e0f);
                }
            }


            for (IKSegment child = segment.GetChild(); child != null; child = child.GetSibling())
                JacobianSegment(jacobian, centre, child);
        }


        public override float GetDistance() => _distance;
        public override void Scale(float newScale)
        {
            _goalCentre *= newScale;
            _distance *= newScale;
        }
    }
}