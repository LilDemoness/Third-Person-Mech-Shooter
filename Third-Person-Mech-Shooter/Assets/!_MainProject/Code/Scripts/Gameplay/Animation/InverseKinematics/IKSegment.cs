using System.Data.SqlTypes;
using EigenPort;

namespace Gameplay.Animations
{
    /// <summary>
    ///     Encodes information about a segment's local coordinate system.
    ///     These segments always point along the y-axis.
    /// </summary>
    /// <remarks>
    ///     The local coordinates of a joint are:
    ///         local_transform = translate(tr1) * rotation(A) * rotation(q) * translate(0, length, 0)
    ///     This can be read as:
    ///         - Translate by (0, length, 0)
    ///         - Multiply by the rotation matrix derived from the current angle parameterization 'q'.
    ///         - Multiply byu our user defined matrix representing the rest position of the bone.
    ///         - Translate by the user defined translation.
    ///     The ordering of these transformations is vital, and you must use exactly the same transformations when displaying the segments.
    /// </remarks>
    public abstract class IKSegment
    {
        // Tree Structure Variables.
        protected IKSegment _parent;
        protected IKSegment _child;
        protected IKSegment _sibling;
        protected IKSegment _composite;


        // Full Transform = _start * _restBasis * _basis * translation
        protected Vector3 _start;
        protected Matrix3x3 _restBasis;
        protected Matrix3x3 _basis;
        protected Vector3 _translation;

        // Original values.
        protected Matrix3x3 _originalBasis;
        protected Vector3 _originalTranslation;


        // The maximum extension of this segment.
        protected float _maxExtension;


        // Accumulated maximum extension of this segment.
        protected Vector3 _globalStart;
        protected Affine3 _globalTransform;


        // Degrees of Freedom information.
        protected int _numberOfDoF;
        protected int _dofId;

        protected bool[] _locked = new bool[3];
        protected bool _isTranslationalSegment;
        protected float[] _weight = new float[3];


        /// <param name="numDoF"> The number of degrees of freedom.</param>
        protected IKSegment(int numDoF, bool isTranslationalSegment)
        {
            _parent = null;
            _child = null;
            _sibling = null;
            _composite = null;

            _numberOfDoF = numDoF;
            _isTranslationalSegment = isTranslationalSegment;

            _locked[0] = _locked[1] = _locked[2] = false;
            _weight[0] = _weight[1] = _weight[2] = 1.0f;

            _maxExtension = 0.0f;


            _start = new Vector3(0.0f, 0.0f, 0.0f);
            _restBasis = Matrix3x3.Identity;
            _basis = Matrix3x3.Identity;
            _translation = new Vector3(0.0f, 0.0f, 0.0f);

            _originalBasis = _basis;
            _originalTranslation = _translation;

            _globalStart = new();
            _globalTransform = new();
        }
        ~IKSegment()
        {
            if (_parent != null)
                _parent.RemoveChild(this);

            for (IKSegment segment = _child; segment != null; segment = segment.GetSibling())
                segment._parent = null;
        }
        public void Reset()
        {
            // Reset locked state.
            _locked[0] = _locked[1] = _locked[2] = false;

            // Reset translation.
            _basis = _originalBasis;
            _translation = _originalTranslation;
            SetBasis(_basis);

            // Reset our children.
            for (IKSegment segment = _child; segment != null; segment = segment.GetSibling())
                segment.Reset();
        }

        /// <summary></summary>
        /// <param name="start"> A user defined translation.</param>
        /// <param name="restBasis"> A user defined rotation.</param>
        /// <param name="basis"> A user defined rotation.</param>
        /// <param name="length"> Length of this segment.</param>
        public void SetTransform(Vector3 start, Matrix3x3 restBasis, Matrix3x3 basis, float length)
        {
            _maxExtension = start.magnitude + length;

            _start = start;
            _restBasis = restBasis;

            _originalBasis = basis;
            SetBasis(basis);

            _translation = new Vector3(0.0f, length, 0.0f);
            _originalTranslation = _translation;
        }


        public IKSegment GetParent() => _parent;
        public IKSegment GetChild() => _child;
        public IKSegment GetSibling() => _sibling;
        public IKSegment GetComposite() => _composite;

        public void SetParent(IKSegment newParent)
        {
            if (_parent == newParent)
                return;

            if (_parent != null)
                _parent.RemoveChild(this);

            if (newParent != null)
            {
                _sibling = newParent._child;
                newParent._child = this;
            }

            _parent = newParent;
        }
        protected void RemoveChild(IKSegment child)
        {
            if (_child == null)
                return;
                
            if (_child == child)
            {
                _child = _child._sibling;
            }
            else
            {
                IKSegment segment = _child;

                // Find the child pointing to the child we wish to remove.
                while(segment != null && segment._sibling != child)
                    segment = segment._sibling;

                // Remove the child by overriting the sibling reference to it.
                if (child == segment._sibling)
                    segment._sibling = child._sibling;
            }
        }
        protected void SetComposite(IKSegment segment) => _composite = segment;


        /// <summary>
        ///     Returns the number of Degrees of Freedom.
        /// </summary>
        public int GetNumberOfDoF() => _numberOfDoF;

        /// <summary>
        ///     Returns the unique ID of this segment.
        /// </summary>
        public int GetDoFId() => _dofId;

        public void SetDoFId(int dofId) => _dofId = dofId;


        /// <summary>
        ///     Returns the max distance of the end of this bone from the local origin.
        /// </summary>
        public float GetMaxExtension() => _maxExtension;


        // The change in rotation and translation from the rest pose.
        public Matrix3x3 GetBasisChange() => Matrix3x3.TryCreateFromMatrix(_originalBasis.GetTranspose() * _basis);
        public Vector3 GetTranslationChange() => _translation - _originalTranslation;


        public Vector3 GetGlobalStart() => _globalStart;
        public Vector3 GetGlobalEnd() => _globalTransform.GetTranslation();


        public Affine3 GetGlobalTransform() => _globalTransform;


        public bool IsTranslationalSegment() => _isTranslationalSegment;

        public bool IsLocked(int dof) => _locked[dof];
        public void Unlock()
        {
            _locked[0] = _locked[1] = _locked[2] = false;
        }
        public virtual void Lock(int dof, IKJacobian jacobian, Vector3 delta) { }


        public float GetWeight(int axis) => _weight[axis];
        public void ScaleWeight(int axis, float scale) => _weight[axis] *= scale;
        // Set Joint Weights (Per Axis).
        public virtual void SetWeight(int axis, float newWeight) => _weight[axis] = newWeight;


        /// <summary>
        ///     Recursively update the global coordinates of this segment.
        /// </summary>
        /// <param name="global"> The global transformation from the parent segment</param>
        public void UpdateTransform(Affine3 global)
        {
            // Compute the global transform at the end of the segment.
            _globalStart = new Vector3(global.GetTranslation() + global.GetLinear() * _start);

            _globalTransform.SetTranslation(_globalStart);
            _globalTransform.SetLinear(global.GetLinear() * _restBasis * _basis);
            _globalTransform.SetTranslation(_translation);

            // Update child transforms.
            for (IKSegment segment = _child; segment != null; segment = segment.GetSibling())
                segment.UpdateTransform(_globalTransform);
        }


        /// <summary>
        ///     Update the angles using the dTheta's computed from the jacobian matrix.
        /// </summary>
        /// <param name="jacobian"></param>
        /// <param name="delta"></param>
        /// <param name="clamp"></param>
        /// <returns> True if the angles were limited.</returns>
        public abstract bool UpdateAngle(IKJacobian jacobian, ref Vector3 delta, ref bool[] clamp);
        public abstract void ApplyAngleUpdates();


        /// <summary>
        ///     Returns the axis from the rotation matrix for derivative computation.
        /// </summary>
        public abstract Vector3 GetAxis(int dof);


        public virtual void SetLimit(int axis, float minLimit, float maxLimit) { }
        public virtual void SetBasis(Matrix3x3 newBasis) { }


        public void PrependBasis(Matrix3x3 mat) => _basis = Matrix3x3.TryCreateFromMatrix(_restBasis.GetInverse() * mat * _restBasis * _basis);


        public virtual void Scale(float scale)
        {
            _start *= scale;
            _translation *= scale;
            _originalTranslation *= scale;
            _globalStart *= scale;
            _globalTransform.Scale(scale);
            _maxExtension *= scale;
        }
    }


    public class IKSphericalSegment : IKSegment
    {
        private Matrix3x3 _newBasis;

        private bool _limitX, _limitY, _limitZ;

        float[] _min = new float[2], _max = new float[2];
        float _minY, _maxY, _maxX, _maxZ, _offsetX, _offsetZ;
        float _lockedAngleX, _lockedAngleY, _lockedAngleZ;

        public IKSphericalSegment()
            : base(3, false)
        {
            _limitX = false;
            _limitY = false;
            _limitZ = false;
        }

        public override Vector3 GetAxis(int dof) => _globalTransform.GetLinear().GetColumn(dof);


        public override bool UpdateAngle(IKJacobian jacobian, ref Vector3 delta, ref bool[] clamp)
        {
            if (_locked[0] && _locked[1] && _locked[2])
                return false;

            Vector3 dq = new Vector3(
                x: jacobian.AngleUpdate(_dofId),
                y: jacobian.AngleUpdate(_dofId + 1),
                z: jacobian.AngleUpdate(_dofId + 2)
            );

            // Directly update the rotation matrix (Using Rodrigues' rotation formula) to avoid singularities and to allow for smooth integration.
            float theta = dq.magnitude;

            if (!IKMath.FuzzyZero(theta))
            {
                Vector3 w = dq * (1.0f / theta);

                float sine = IKMath.Sin(theta);
                float cosine = IKMath.Cos(theta);
                float cosineInv = 1.0f - cosine;

                float xSine = w.x * sine;
                float ySine = w.y * sine;
                float zSine = w.z * sine;

                float xxCosine = w.x * w.x * cosineInv;
                float xyCosine = w.x * w.y * cosineInv;
                float xzCosine = w.x * w.z * cosineInv;
                float yyCosine = w.y * w.y * cosineInv;
                float yzCosine = w.y * w.z * cosineInv;
                float zzCosine = w.z * w.z * cosineInv;

                Matrix3x3 m = new Matrix3x3(
                    cosine + xxCosine,
                    -zSine + xyCosine,
                    ySine + xzCosine,
                    zSine + xyCosine,
                    cosine + yyCosine,
                    -xSine + yzCosine,
                    -ySine + xzCosine,
                    xSine + yzCosine,
                    cosine + zzCosine
                );

                _newBasis = _basis * m;
            }
            else
            {
                _newBasis = _basis;
            }


            if (!_limitX && !_limitY && !_limitZ)
                // No angle limiting.
                return false;

            // Limit angles.
            Vector3 a = IKMath.SphericalRangeParameters(_newBasis);

            if (_locked[0])
                a.x = _lockedAngleX;
            if (_locked[1])
                a.y = _lockedAngleY;
            if (_locked[2])
                a.z = _lockedAngleZ;

            float angleX = a.x, angleY = a.y, angleZ = a.z;
            clamp[0] = clamp[1] = clamp[2] = false;

            if (_limitY)
            {
                if (a.y > _maxY)
                {
                    angleY = _maxY;
                    clamp[1] = true;
                }
                else if (a.y < _minY)
                {
                    angleY = _minY;
                    clamp[1] = true;
                }
            }


            if (_limitX && _limitZ)
            {
                if (IKMath.EllipseClamp(ref angleX, ref angleZ, _min, _max))
                    clamp[0] = clamp[2] = true;
            }
            else if (_limitX)
            {
                if (a.x > _max[0])
                {
                    angleX = _max[0];
                    clamp[0] = true;
                }
                else if (a.x < _min[0])
                {
                    angleX = _min[0];
                    clamp[0] = true;
                }
            }
            else if (_limitZ)
            {
                if (a.z > _max[1])
                {
                    angleZ = _max[1];
                    clamp[2] = true;
                }
                else if (a.z < _min[1])
                {
                    angleZ = _min[1];
                    clamp[2] = true;
                }
            }


            if (!clamp[0] && !clamp[1] && !clamp[2])
                // Nothing was clamped.
            {
                if (_locked[0] || _locked[1] || _locked[2])
                    _newBasis = IKMath.ComputeSwingMatrix(angleX, angleZ) * IKMath.ComputeTwistMatrix(angleY);
                return false;
            }
            // We clamped one of our angles.

            _newBasis = IKMath.ComputeSwingMatrix(angleX, angleZ) * IKMath.ComputeTwistMatrix(angleY);
            delta = IKMath.MatrixToAxisAngle(_basis.GetTranspose() * _newBasis);

            if (!(_locked[0] || _locked[2]) && (clamp[0] || clamp[2]))
            {
                _lockedAngleX = angleX;
                _lockedAngleZ = angleZ;
            }

            if (!_locked[1] && clamp[1])
                _lockedAngleY = angleY;

            return true;
        }
        public override void Lock(int dof, IKJacobian jacobian, Vector3 delta)
        {
            if (dof == 1)
            {
                _locked[1] = true;
                jacobian.Lock(_dofId + 1, delta.y);
            }
            else
            {
                _locked[0] = _locked[2] = true;
                jacobian.Lock(_dofId, delta.x);
                jacobian.Lock(_dofId + 2, delta.z);
            }
        }
        public override void ApplyAngleUpdates() => _basis = _newBasis;


        //public bool ComputeClampRotation(Vector3 clamp) { }


        public override void SetLimit(int axis, float limitMin, float limitMax)
        {
            if (limitMin > limitMax)
                return; // Invalid limit.

            limitMin = IKMath.Clamp(limitMin, -IKMath.PI, IKMath.PI);
            limitMax = IKMath.Clamp(limitMin, -IKMath.PI, IKMath.PI);

            if (axis == 1)
            {
                _minY = limitMin;
                _maxY = limitMax;

                _limitY = true;
            }
            else
            {
                // Convert to angle-axis perameters.
                limitMin = IKMath.Sin(limitMin * 0.5f);
                limitMax = IKMath.Sin(limitMax * 0.5f);

                if (axis == 0)
                {
                    _min[0] = -limitMax;
                    _max[0] = -limitMin;

                    _limitX = true;
                }
                else if (axis == 2)
                {
                    _min[1] = -limitMax;
                    _max[1] = -limitMin;

                    _limitZ = true;
                }
            }
        }
    }


    public class IKNullSegment : IKSegment
    {
        public IKNullSegment()
            : base(0, false)
        { }

        public override bool UpdateAngle(IKJacobian jacobian, ref Vector3 delta, ref bool[] clamp) => false;
        public override void ApplyAngleUpdates() { }

        public override Vector3 GetAxis(int _) => Vector3.zero;
        public override void SetBasis(Matrix3x3 _) => _basis.SetIdentity();
    }


    public class IKRevoluteSegment : IKSegment
    {
        private int _axis;

        private float _angle;
        private float _newAngle;

        private bool _useLimit;
        private float _minLimit;
        private float _maxLimit;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="axis"> The axis of the Degree of Freedom, in the range 0..2</param>
        public IKRevoluteSegment(int axis)
            : base(1, false)
        {
            _axis = axis;
            _angle = 0.0f;
            _useLimit = false;
        }


        public override Vector3 GetAxis(int _) => _globalTransform.GetLinear().GetColumn(_axis);


        public override bool UpdateAngle(IKJacobian jacobian, ref Vector3 delta, ref bool[] clamp)
        {
            if (_locked[0])
                return false;

            _newAngle = _angle + jacobian.AngleUpdate(_dofId);

            clamp[0] = false;

            if (_useLimit == false)
                return false; // We don't need to apply limits.

            if (_newAngle > _maxLimit)
            {
                delta[0] = _maxLimit - _angle;
            }
            else if (_newAngle < _minLimit)
            {
                delta[0] = _minLimit - _angle;
            }
            else
                return false; // We don't need to apply limits.

            clamp[0] = true;
            _newAngle = _angle + delta[0];

            return true; // We applied a limit.
        }
        public override void Lock(int _, IKJacobian jacobian, Vector3 delta)
        {
            _locked[0] = true;
            jacobian.Lock(_dofId, delta[0]);
        }
        public override void ApplyAngleUpdates()
        {
            _angle = _newAngle;
            _basis = IKMath.RotationMatrix(_angle, _axis);
        }


        public override void SetLimit(int axis, float minLimit, float maxLimit)
        {
            if (minLimit > maxLimit || axis != _axis)
                return; // Invalid limit for this segment type.

            // Clamp and convert to angle-axis parameters.
            _minLimit = IKMath.Clamp(minLimit, -IKMath.PI, IKMath.PI);
            _maxLimit = IKMath.Clamp(maxLimit, -IKMath.PI, IKMath.PI);

            _useLimit = true;
        }
        public override void SetWeight(int axis, float newWeight)
        {
            if (axis == _axis)
                _weight[0] = newWeight;
        }
        public override void SetBasis(Matrix3x3 newBasis)
        {
            if (_axis == 1)
            {
                _angle = IKMath.ComputeTwist(newBasis);
                _basis = IKMath.ComputeTwistMatrix(_angle);
            }
            else
            {
                _angle = IKMath.EulerAngleFromMatrix(newBasis, _axis);
                _basis = IKMath.RotationMatrix(_angle, _axis);
            }
        }
    }


    public class IKSwingSegment : IKSegment
    {
        private Matrix3x3 _newBasis;

        private bool _useLimitX;
        private bool _useLimitZ;
        private float[] _minLimits;
        private float[] _maxLimits;

        private float _maxX;
        private float _maxZ;
        private float _offsetX;
        private float _offsetZ;

        /// <summary>
        ///     Uses an XZ Degree of Freedom with a single direct rotation.
        /// </summary>
        public IKSwingSegment()
            : base(2, false)
        {
            _useLimitX = false;
            _useLimitZ = false;
        }


        public override Vector3 GetAxis(int dof) => _globalTransform.GetLinear().GetColumn((dof == 0) ? 0 : 2);


        public override bool UpdateAngle(IKJacobian jacobian, ref Vector3 delta, ref bool[] clamp)
        {
            if (_locked[0] && _locked[1])
                return false;

            Vector3 dq = new Vector3(
                x: jacobian.AngleUpdate(_dofId),
                y: 0.0f,
                z: jacobian.AngleUpdate(_dofId + 1)
                );


            // Directly update the rotation matrix with Rodrigues' rotation formula to avoid singularities and allow smooth rotation.
            float theta = dq.magnitude;
            if (!IKMath.FuzzyZero(theta))
            {
                Vector3 w = dq * (1.0f / theta);

                float sine = IKMath.Sin(theta);
                float cosine = IKMath.Cos(theta);
                float cosineInv = 1.0f - cosine;

                float xSine = w.x * sine;
                float zSine = w.z * sine;

                float xxCosine = w.x * w.x * cosineInv;
                float xzCosine = w.x * w.z * cosineInv;
                float zzCosine = w.z * w.z * cosineInv;

                Matrix3x3 m = new Matrix3x3(
                    cosine + xxCosine, -zSine, xzCosine,
                    zSine, cosine, -xSine,
                    xzCosine, xSine, cosine + zzCosine
                );

                _newBasis = _basis * m;
                IKMath.RemoveTwist(ref _newBasis);
            }
            else
                _newBasis = _basis;


            if (!_useLimitX && !_useLimitZ)
                return false; // We don't need to do any clamping.


            // Check if we need to clamp, clamping as required.
            Vector3 a = IKMath.SphericalRangeParameters(_newBasis);
            float ax = 0.0f, az = 0.0f;

            clamp[0] = clamp[1] = false;

            if (_useLimitX && _useLimitZ)
            {
                ax = a.x;
                az = a.z;

                if (IKMath.EllipseClamp(ref ax, ref az, _minLimits, _maxLimits))
                    clamp[0] = clamp[1] = true;
            }
            else if (_useLimitX)
            {
                if (ax < _minLimits[0])
                {
                    ax = _minLimits[0];
                    clamp[0] = true;
                }
                else if (ax > _maxLimits[0])
                {
                    ax = _maxLimits[0];
                    clamp[0] = true;
                }
            }
            else if (_useLimitZ)
            {
                if (az < _minLimits[1])
                {
                    az = _minLimits[1];
                    clamp[1] = true;
                }
                else if (az > _maxLimits[1])
                {
                    az = _maxLimits[1];
                    clamp[1] = true;
                }
            }

            if (!clamp[0] && !clamp[1])
                return false; // We didn't need to clamp.

            _newBasis = IKMath.ComputeSwingMatrix(ax, az);
            delta = IKMath.MatrixToAxisAngle(_basis.GetTranspose() * _newBasis);
            delta[1] = delta[2];
            delta[2] = 0.0f;

            return true; // We needed to clamp the rotation.
        }
        public override void Lock(int _, IKJacobian jacobian, Vector3 delta)
        {
            _locked[0] = _locked[1] = true;
            jacobian.Lock(_dofId, delta[0]);
            jacobian.Lock(_dofId + 1, delta[1]);
        }
        public override void ApplyAngleUpdates() => _basis = _newBasis;


        public override void SetLimit(int axis, float minLimit, float maxLimit)
        {
            if (minLimit > maxLimit)
                return; // Invalid limit for this IK Segment type.

            // Clamp and convret to axis angle parameters.
            minLimit = IKMath.Sin(IKMath.Clamp(minLimit, -IKMath.PI, IKMath.PI) * 0.5f);
            maxLimit = IKMath.Sin(IKMath.Clamp(maxLimit, -IKMath.PI, IKMath.PI) * 0.5f);

            // Put the centre of the ellipse in the midpoint of min and max.
            float offset = 0.5f * (minLimit + maxLimit);

            if (axis == 0)
            {
                _minLimits[0] = -maxLimit;
                _maxLimits[0] = minLimit;

                _useLimitX = true;
                _offsetX = offset;
                _maxX = maxLimit;
            }
            else if (axis == 2)
            {
                _minLimits[1] = -maxLimit;
                _maxLimits[1] = -minLimit;

                _useLimitZ = true;
                _offsetZ = offset;
                _maxZ = maxLimit;
            }
        }
        public override void SetWeight(int axis, float newWeight)
        {
            if (axis == 0)
                _weight[0] = newWeight;
            else if (axis == 2)
                _weight[1] = newWeight;
        }
        public override void SetBasis(Matrix3x3 newBasis)
        {
            _basis = newBasis;
            IKMath.RemoveTwist(ref _basis);
        }
    }


    public class IKElbowSegment : IKSegment
    {
        private int _axis;

        private float _twist;
        private float _angle;
        private float _newTwist;
        private float _newAngle;

        private float _cosTwist;
        private float _sinTwist;


        private bool _useAngleLimit;
        private float _minAngle;
        private float _maxAngle;

        private bool _useTwistLimit;
        private float _minTwist;
        private float _maxTwist;


        /// <summary>
        ///     Uses two sequential rotations:
        ///     - First rotate around X or Z
        ///     - Then rotate around Y (Twist).
        /// </summary>
        /// <param name="axis"> XY or ZY</param>
        public IKElbowSegment(int axis)
            : base(2, false)
        {
            _axis = axis;

            _twist = 0.0f;
            _angle = 0.0f;
            _cosTwist = 0.0f;
            _sinTwist = 0.0f;

            _useAngleLimit = false;
            _useTwistLimit = false;
        }


        public override Vector3 GetAxis(int dof)
        {
            if (dof != 0)
                return _globalTransform.GetLinear().GetColumn(1);

            Vector3 v = _axis == 0
                ? new Vector3(_cosTwist, 0, _sinTwist)
                : new Vector3(-_sinTwist, 0, _cosTwist);
            return (EigenPort.Vector3)(_globalTransform.GetLinear() * v);
        }


        public override bool UpdateAngle(IKJacobian jacobian, ref Vector3 delta, ref bool[] clamp)
        {
            if (_locked[0] && _locked[1])
                return false;

            clamp[0] = clamp[1] = false;

            if (_locked[0])
            {
                _newAngle = _angle + jacobian.AngleUpdate(_dofId);

                if (_useAngleLimit)
                {
                    if (_newAngle > _maxAngle)
                    {
                        delta[0] = _maxAngle - _angle;
                        _newAngle = _maxAngle;
                        clamp[0] = true;
                    }
                    else if (_newAngle < _minAngle)
                    {
                        delta[0] = _minAngle - _angle;
                        _newAngle = _minAngle;
                        clamp[0] = true;
                    }
                }
            }

            if (_locked[1])
            {
                _newTwist = _twist + jacobian.AngleUpdate(_dofId + 1);

                if (_useTwistLimit)
                {
                    if (_newTwist > _maxTwist)
                    {
                        delta[1] = _maxTwist - _twist;
                        _newTwist = _maxTwist;
                        clamp[1] = true;
                    }
                    else if (_newTwist < _minTwist)
                    {
                        delta[1] = _minTwist - _twist;
                        _newTwist = _minTwist;
                        clamp[1] = true;
                    }
                }
            }

            // Return true if we clamped, or false otherwise.
            return clamp[0] || clamp[1];
        }
        public override void Lock(int dof, IKJacobian jacobian, Vector3 delta)
        {
            if (dof == 0)
            {
                _locked[0] = true;
                jacobian.Lock(_dofId, delta[0]);
            }
            else
            {
                _locked[1] = true;
                jacobian.Lock(_dofId + 1, delta[1]);
            }
        }
        public override void ApplyAngleUpdates()
        {
            _angle = _newAngle;
            _twist = _newTwist;

            _sinTwist = IKMath.Sin(_twist);
            _cosTwist = IKMath.Cos(_twist);

            Matrix3x3 a = IKMath.RotationMatrix(_axis, _axis);
            Matrix3x3 t = IKMath.RotationMatrix(_sinTwist, _cosTwist, 1);

            _basis = a * t;
        }


        public override void SetLimit(int axis, float minLimit, float maxLimit)
        {
            if (minLimit > maxLimit)
                return; // Invalid limit for this IK Segment type.

            // Clamp and convert to axis angle parameters.
            minLimit = IKMath.Clamp(minLimit, -IKMath.PI, IKMath.PI);
            maxLimit = IKMath.Clamp(maxLimit, -IKMath.PI, IKMath.PI);

            if (axis == 1)
            {
                _minTwist = minLimit;
                _maxTwist = maxLimit;
                _useTwistLimit = true;
            }
            else if (axis == _axis)
            {
                _minAngle = minLimit;
                _maxAngle = maxLimit;
                _useAngleLimit = true;
            }
        }
        public override void SetWeight(int axis, float newWeight)
        {
            if (axis == _axis)
                _weight[0] = newWeight;
            else if (axis == 1)
                _weight[1] = newWeight;
        }
        public override void SetBasis(Matrix3x3 newBasis)
        {
            _basis = newBasis;

            _twist = IKMath.ComputeTwist(_basis);
            IKMath.RemoveTwist(ref _basis);
            _angle = IKMath.EulerAngleFromMatrix(newBasis, _axis);

            _basis = IKMath.RotationMatrix(_angle, _axis) * IKMath.ComputeTwistMatrix(_twist);
        }
    }


    public class IKTranslateSegment : IKSegment
    {
        private int[] _axis = new int[3];
        private bool[] _axisEnabled = new bool[3];

        private Vector3 _newTranslation;

        private bool[] _useLimit = new bool[3];
        private float[] _minLimit = new float[3];
        private float[] _maxLimit = new float[3];


        /// <summary>
        ///     An <see cref="IKTranslateSegment"/> with 1 axis of freedom.
        /// </summary>
        /// <param name="axis1"> The axis of freedom (0 = X, 1 = Y, 2 = Z).</param>
        public IKTranslateSegment(int axis1)
            : base(1, true)
        {
            _axisEnabled[0] = _axisEnabled[1] = _axisEnabled[2] = false;
            _axisEnabled[axis1] = true;

            _axis[0] = axis1;

            _useLimit[0] = _useLimit[1] = _useLimit[2] = false;
        }
        public IKTranslateSegment(int axis1, int axis2)
            : base(2, true)
        {
            _axisEnabled[0] = _axisEnabled[1] = _axisEnabled[2] = false;
            _axisEnabled[axis1] = _axisEnabled[axis2] = true;

            _axis[0] = axis1;
            _axis[1] = axis2;

            _useLimit[0] = _useLimit[1] = _useLimit[2] = false;
        }
        public IKTranslateSegment()
            : base(3, true)
        {
            _axisEnabled[0] = _axisEnabled[1] = _axisEnabled[2] = true;

            _axis[0] = 0;
            _axis[1] = 1;
            _axis[2] = 2;

            _useLimit[0] = _useLimit[1] = _useLimit[2] = false;
        }


        public override Vector3 GetAxis(int dof) => _globalTransform.GetLinear().GetColumn(_axis[dof]);


        public override bool UpdateAngle(IKJacobian jacobian, ref Vector3 delta, ref bool[] clamp)
        {
            int dofId = _dofId;
            int dof = 0;
            bool hasClamped = false;

            for (int i = 0; i < 3; ++i)
            {
                if (!_axisEnabled[i])
                {
                    _newTranslation[i] = _translation[i];
                    continue;
                }

                clamp[dof] = false;

                if (!_locked[dof])
                {
                    _newTranslation[i] = _translation[i] + jacobian.AngleUpdate(dofId);

                    if (_useLimit[i])
                    {
                        if (_newTranslation[i] > _maxLimit[i])
                        {
                            delta[dof] = _maxLimit[i] - _translation[i];
                            _newTranslation[i] = _maxLimit[i];
                            hasClamped = clamp[dof] = true;
                        }
                        else if (_newTranslation[i] < _minLimit[i])
                        {
                            delta[dof] = _minLimit[i] - _translation[i];
                            _newTranslation[i] = _minLimit[i];
                            hasClamped = clamp[dof] = true;
                        }
                    }
                }

                ++dofId;
                ++dof;
            }

            return hasClamped;
        }
        public override void Lock(int dof, IKJacobian jacobian, Vector3 delta)
        {
            _locked[dof] = true;
            jacobian.Lock(_dofId + dof, delta[dof]);
        }
        public override void ApplyAngleUpdates() => _translation = _newTranslation;


        public override void SetLimit(int axis, float minLimit, float maxLimit)
        {
            if (minLimit > maxLimit)
                return; // Invalid limit for this IKSegment type.

            _minLimit[axis] = minLimit;
            _maxLimit[axis] = maxLimit;
            _useLimit[axis] = true;
        }
        public override void SetWeight(int axis, float newWeight)
        {
            for (int i = 0; i < _numberOfDoF; ++i)
            {
                if (_axis[i] == axis)
                    _weight[i] = newWeight;
            }
        }


        public override void Scale(float scale)
        {
            base.Scale(scale);

            for (int i = 0; i < 3; ++i)
            {
                _minLimit[0] *= scale;
                _maxLimit[1] *= scale;
            }

            _newTranslation *= scale;
        }
    }
}