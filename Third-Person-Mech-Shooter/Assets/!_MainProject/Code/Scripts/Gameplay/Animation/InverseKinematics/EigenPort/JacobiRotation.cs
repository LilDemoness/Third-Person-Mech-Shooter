namespace EigenPort
{
    public class JacobiRotation
    {
        private float _cosine;
        private float _sine;


        /// <summary>
        ///     Default constructor without any initialisation.
        /// </summary>
        public JacobiRotation() { }
        /// <summary>
        ///     Constructs a planar rotation from a cosine-sine pair.
        /// </summary>
        public JacobiRotation(float cosine, float sine)
        {
            _cosine = cosine;
            _sine = sine;
        }

        public float GetCosine() => _cosine;
        public float GetSine() => _sine;


        /// <summary>
        ///     Returns the transposed transformation.
        /// </summary>
        public JacobiRotation GetTransposition() => new JacobiRotation(_cosine, -_sine);


        /// <summary>
        ///     Makes this a Jacobi rotation 'J' such that applying 'J' on both the right and left sides of the self-adjoint
        ///     2x2 matrix B = {x, y | y, z} yields a diagonal matrix A = J^* B J
        /// </summary>
        public bool MakeJacobi(float x, float y, float z)
        {
            float deno = 2.0f * IKMath.Abs(y);

            if (deno < float.Epsilon)
            {
                _cosine = 1.0f;
                _sine = 0.0f;
                return false;
            }

            float tau = (x - z) / deno;
            float w = IKMath.Sqrt(IKMath.Abs2(tau) + 1.0f);
            float t = tau > 0.0f ? 1.0f / (tau + w) : 1.0f / tau - w;

            float signT = t > 0.0f ? 1.0f : -1.0f;
            float n = 1.0f / IKMath.Sqrt(IKMath.Abs2(t) + 1.0f);

            _sine = signT * (y / IKMath.Abs(y)) * IKMath.Abs(t) * n;
            _cosine = n;
            return true;
        }
        /// <inheritdoc cref="MakeJacobi(float, float, float)"/>
        public bool MakeJacobi(Matrix matrix, int p, int q) => MakeJacobi(matrix[p, p], matrix[p, q], matrix[q, q]);


        public void ApplyRotationInThePlane(Vector xVector, Vector yVector)
        {
            if (xVector.GetSize() != yVector.GetSize())
                throw new System.ArgumentException($"You cannot apply a JacobiRotation to two Vectors of different lengths (x: {xVector.GetSize()} vs y: {yVector.GetSize()}");
            if (_cosine == 1.0f && _sine == 0.0f)
                return;

            int size = xVector.GetSize();

            for(int i = 0; i < size; ++i)
            {
                float xi = xVector[i];
                float yi = yVector[i];

                xVector[i] = _cosine * xi + _sine * yi;
                yVector[i] = -_sine * xi + _cosine * yi;
            }
        }


        #region Operator Overloading

        public static JacobiRotation operator *(JacobiRotation lhs, JacobiRotation rhs) => new JacobiRotation(lhs._cosine * rhs._cosine - rhs._sine * rhs._sine, lhs._cosine * rhs._sine + lhs._sine * rhs._cosine);
        
        #endregion
    }
}