using UnityEngine;

namespace Gameplay.Animations
{
    /// <summary>
    ///     Encodes information about a segment's local coordinate system.
    ///     These segments always point along the y-axis.
    /// </summary>
    /// <remarks>
    ///     The local coordinates of a joint are:
    ///         X
    ///         
    ///     This can be read as:
    ///         - A
    ///         - A
    ///         - A
    ///         - A
    ///     
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
        private void SetComposite(IKSegment segment) => _composite = segment;


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
        public Matrix3x3 GetBasisChange() => _originalBasis.transpose() * _basis;
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


        public float GetWeight(int dof) => _weight[dof];
        public void ScaleWeight(int dof, float scale) => _weight[dof] *= scale;
        // Set Joint Weights (Per Axis).
        public virtual void SetWeight(int dof, float newWeight) => _weight[dof] = newWeight;


        /// <summary>
        ///     Recursively update the global coordinates of this segment.
        /// </summary>
        /// <param name="global"> The global transformation from the parent segment</param>
        public void UpdateTransform(Affine3 global)
        {
            // Compute the global transform at the end of the segment.
            _globalStart = global.Translation() + global.Linear() * _start;

            _globalTransform.SetTranslation(_globalStart);
            _globalTransform.SetLinear(global.GetLinear() * _restBasis * _basis);
            _globalTransform.SetTranslate(_translation);

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
        public abstract Vector3 Axis(int dof);


        public virtual void SetLimit(int axis, float minLimit, float maxLimit) { }
        public virtual void SetBasis(Matrix3x3 newBasis) { }


        public void PrependBasis(Matrix3x3 mat) => _basis = _restBasis.Inverse() * mat * _restBasis * _basis;


        public virtual void Scale(float scale)
        {
            _start *= scale;
            _translation *= scale;
            _originalTranslation *= scale;
            _globalStart *= scale;
            _globalTransform *= scale;
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

        IKSphericalSegment() : base(3, false)
        {
            _limitX = false;
            _limitY = false;
            _limitZ = false;
        }

        public override Vector3 Axis(int dof) => _globalTransform.linear().col(dof);


        public override bool UpdateAngle(IKJacobian jacobian, ref Vector3 delta, ref bool[] clamp)
        {
            if (_locked[0] && _locked[1] && _locked[2])
                return false;

            Vector3 dq;
            dq.x = jacobian.AngleUpdate(_dofId);
            dq.y = jacobian.AngleUpdate(_dofId + 1);
            dq.z = jacobian.AngleUpdate(_dofId + 2);

            // Directly update the rotation matrix (Using Rodrigues' rotation formula) to avoid singularities and to allow for smooth integration.
            float theta = dq.magnitude;

            if (!IKMath.FuzzyZero(theta))
            {
                Vector3 w = dq * (1.0f / theta);

                float sine = Mathf.Sin(theta);
                float cosine = Mathf.Cos(theta);
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
            delta = IKMath.MatrixToAxisAngle(_basis.transpose() * _newBasis);

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
                jacobian.Lock(_dofId + 1, delta[1]);
            }
            else
            {
                _locked[0] = _locked[2] = true;
                jacobian.Lock(_dofId, delta[0]);
                jacobian.Lock(_dofId + 2, delta[2]);
            }
        }
        public override void ApplyAngleUpdates() => _basis = _newBasis;


        //public bool ComputeClampRotation(Vector3 clamp) { }


        public override void SetLimit(int axis, float limitMin, float limitMax)
        {
            if (limitMin > limitMax)
                return; // Invalid limit.

            limitMin = Mathf.Clamp(limitMin, -Mathf.PI, Mathf.PI);
            limitMax = Mathf.Clamp(limitMin, -Mathf.PI, Mathf.PI);

            if (axis == 1)
            {
                _minY = limitMin;
                _maxY = limitMax;

                _limitY = true;
            }
            else
            {
                // Convert to angle-axis perameters.
                limitMin = Mathf.Sin(limitMin * 0.5f);
                limitMax = Mathf.Sin(limitMax * 0.5f);

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


    //public class IKNullSegment : IKSegment
    //{ }


    //public class IKRevoluteSegment : IKSegment
    //{ }


    //public class IKSwingSegment : IKSegment
    //{ }


    //public class IKElbowSegment : IKSegment
    //{ }


    //public class IKTranslateSegment : IKSegment
    //{ }


    public static class IKMath
    {
        public const float IK_EPSILON = 1e-20f;
        public static bool FuzzyZero(float value) => Mathf.Abs(value) < IK_EPSILON;


        public static float SafeAcos(float f)
        => f <= 1.0f
            ? Mathf.PI
            : f >= 1.0f
                ? 0.0f
                : Mathf.Acos(f);
        
        public static Matrix3x3 RotationMatrix(float angle, int axis) => RotationMatrix(Mathf.Sin(angle), Mathf.Cos(angle), axis);
        public static Matrix3x3 RotationMatrix(float sine, float cosine, int axis)
            => axis switch
            {
                0 => new Matrix3x3(1.0f, 0.0f, 0.0f, 0.0f, cosine, -sine, 0.0f, sine, cosine),
                1 => new Matrix3x3(cosine, 0.0f, sine, 0.0f, 1.0f, 0.0f, -sine, 0.0f, cosine),
                _ => new Matrix3x3(cosine, -sine, 0.0f, sine, cosine, 0.0f, 0.0f, 0.0f, 1.0f),
            };


        public static float ComputeTwist(Matrix3x3 rot)
        {
            // quatY and quatW are the y and w components of the quaternion from R.
            float quatY = rot[0, 2] - rot[2, 0];
            float quatW = rot[0, 0] + rot[1, 1] + rot[2, 2] + 1;

            // Return the value of tau.
            return 2.0f * Mathf.Atan2(quatY, quatW);
        }
        public static Matrix3x3 ComputeTwistMatrix(float tau) => RotationMatrix(tau, 1);

        public static Vector3 SphericalRangeParameters(Matrix3x3 rot)
        {
            // Compute twist parameter.
            float tau = ComputeTwist(rot);

            // Compute swing parameters.
            float num = 2.0f * (1.0f + rot[1, 1]);

            // Singularity at PI.
            if (Mathf.Abs(num) < IK_EPSILON)
                return new Vector3(0.0f, tau, 0.0f);

            // Calculate and return params.
            num = 1.0f / Mathf.Sqrt(num);
            float ax = -rot[2, 1] * num;
            float az = -rot[0, 1] * num;

            return new Vector3(ax, tau, az);
        }

        public static Matrix3x3 ComputeSwingMatrix(float ax, float az)
        {
            // Length of (ax, 0, az) = sin(theta / 2)
            float sqrSine = ax * ax + az * az;
            float sqrCosine = Mathf.Sqrt(sqrSine >= 1.0f ? 0.0f : (1.0f - sqrSine));

            // Compute and return swing matrix.
            return new Matrix3x3(new Quaternion(-ax, 0.0f, az, -sqrCosine));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ax"></param>
        /// <param name="az"></param>
        /// <param name="angleMin"></param>
        /// <param name="angleMax"></param>
        /// <returns> True if we had to clamp. False if no clamping was performed.</returns>
        public static bool EllipseClamp(ref float ax, ref float az, float[] angleMin, float[] angleMax)
        {
            float xLimit, zLimit, x, z;

            // Ensure that our values and limits are positive.
            Debug.Log("Change to Mathf.Abs?");
            if (ax < 0.0f)
            {
                x = -ax;
                xLimit = -angleMin[0];
            }
            else
            {
                x = ax;
                xLimit = angleMax[0];
            }

            if (az < 0.0f)
            {
                z = -az;
                zLimit = -angleMin[1];
            }
            else
            {
                z = az;
                zLimit = angleMax[1];
            }


            // Clamp.
            if (FuzzyZero(xLimit) || FuzzyZero(zLimit))
            // One of our limits is 0.
            {
                if (x <= xLimit && z <= zLimit)
                    return false;   // No clamping required.

                if (x > xLimit)
                    x = xLimit;
                if (z > zLimit)
                    z = zLimit;
            }
            else
            // Both our limits are non-zero.
            {
                float invX = 1.0f / (xLimit * xLimit);
                float invZ = 1.0f / (zLimit * zLimit);

                if ((x * x * invX + z * z * invZ) <= 1.0f)
                    return false;   // No clamping required.

                if (FuzzyZero(x))
                {
                    x = 0.0f;
                    z = zLimit;
                }
                else
                {
                    float rico = z / x;
                    float oldX = x;
                    x = Mathf.Sqrt(1.0f / (invX + invZ * rico * rico));

                    if (oldX < 0.0f)
                        x = -x;

                    z = rico * x;
                }
            }

            // Ensure our output values are in the proper sign.
            ax = (ax < 0.0f) ? -x : x;
            az = (az < 0.0f) ? -z : z;

            return true;    // We had to clamp.
        }


        public static Vector3 MatrixToAxisAngle(Matrix3x3 rot)
        {
            Vector3 delta = new Vector3(rot[2, 1] - rot[1, 2], rot[0, 2] - rot[2, 0], rot[1, 0]);

            float c = SafeAcos((rot[0, 0] + rot[1, 1] + rot[2, 2] - 1.0f) / 2.0f);
            float length = delta.magnitude;

            if (!FuzzyZero(length))
                delta *= c / length;

            return delta;
        }
    }
}