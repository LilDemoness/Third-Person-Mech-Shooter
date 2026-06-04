using UnityEngine;

namespace Gameplay.Animations
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///     A Vector of size 'X' is the same as a matrix the size 'X'x1 ('X' rows, 1 column).
    /// </remarks>
    public struct VectorX : IMatrixBase, System.IEquatable<VectorX>, System.IEquatable<UnityEngine.Vector2>, System.IEquatable<UnityEngine.Vector3>
    {
        private float[] _values;
        private int _size;

        public VectorX(int size)
        {
            _values = new float[size];
            _size = size;

            SetZeros();
        }
        public VectorX(VectorX other)
            : this(other._size)
        {
            for (int i = 0; i < _size; ++i)
                _values[i] = other._values[i];
        }
        public VectorX(Vector3 other)
            : this(3)
        {
            _values[0] = other.x;
            _values[1] = other.y;
            _values[2] = other.z;
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
        public float GetCoefficient(int row, int column) => this[row];

        public int GetSize() => _size;
        public int GetRowCount() => _size;
        public int GetColumnCount() => 1;

        public void Resize(int newSize)
        {
            _values = new float[newSize];
            _size = newSize;
        }

        public void SetIdentity()
        {
            throw new System.NotImplementedException();
        }
        public void SetZeros()
        {
            for (int i = 0; i < _size; ++i)
                _values[i] = 0.0f;
        }
        public void SetOnes()
        {
            for (int i = 0; i < _size; ++i)
                _values[i] = 1.0f;
        }


        /// <summary>
        ///     Computes the Dot product of two same-size VectorXs
        /// </summary>
        /// <exception cref="System.ArgumentException"> Exception if the VectorXs are of differing sizes.</exception>
        public static float Dot(VectorX a, VectorX b)
        {
            if (a.GetSize() != b.GetSize())
                throw new System.ArgumentException($"Cannot compute the Dot product between a Vector{a.GetSize()} and Vector{b.GetSize()}");

            float dotProduct = 0.0f;
            for(int i = 0; i < a.GetSize(); ++i)
                dotProduct += a[i] * b[i];

            return dotProduct;
        }

        /// <summary>
        ///     Returns the maximum coefficient (Element) within the Vector.
        /// </summary>
        /// <param name="index"> The index where the max value was found.</param>
        public float GetMaxCoefficient(out int index)
        {
            // Initial values.
            index = -1;
            float maxValue = float.MinValue;

            // Find the max value and its index.
            for(int i = 0; i < _size; ++i)
            {
                if (_values[i] > maxValue)
                {
                    index = i;
                    maxValue = _values[i];
                }
            }

            return maxValue;
        }
        /// <inheritdoc cref="GetMaxCoefficient(out int)"/>
        public float GetMaxCoefficient() => GetMaxCoefficient(out _);



        /// <summary>
        ///     Returns a new vector of the last <paramref name="tailSize"/> elements in the vector.
        /// </summary>
        public VectorX GetTail(int tailSize)
        {
            VectorX tail = new VectorX(tailSize);
            for(int i = 0; i < tailSize; ++i)
                tail[i] = this[_size - (i + 1)];

            return tail;
        }
        /// <summary>
        ///     Returns a vector of the elements between 'start' and 'end' in the vector.
        /// </summary>
        public VectorX GetSubmatrix(int start, int end)
        {
            if (start < end)    throw new System.ArgumentException($"Cannot get a submatrix with start {start} and end {end}");
            if (start >= _size) throw new System.IndexOutOfRangeException($"The value of start ({start} is too large for this vector's length ({_size})");
            if (end >= _size)   throw new System.IndexOutOfRangeException($"The value of end ({end} is too large for this vector's length ({_size})");

            VectorX result = new VectorX(end - start);
            for (int i = 0; i < result.GetSize(); ++i)
                result[i] = this[i + start];

            return result;
        }


        public void ApplyHouseholderOnTheLeft(IMatrixBase essential, float tau, ref float workspace);
        public void ApplyHouseholderOnTheRight(IMatrixBase essential, float tau, ref float workspace);

        public void ApplyOnTheLeft(int p, int q, JacobiRotation j);
        public void ApplyOnTheRight(int p, int q, JacobiRotation j);



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

            for (int i = 0; i < _size; ++i)
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
        public override int GetHashCode()
        {
            int hashCode = _values[0].GetHashCode();
            for(int i = 1; i < _size; ++i)
                hashCode ^= _values[i].GetHashCode();
            return hashCode;
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
                returnedVector[i] = a[i] - +b;

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

        #region VectorX w/ Vector3

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(VectorX a, Vector3 b)
        {
            if (a.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot add a Vector{a.GetSize()} to a Vector3");

            return new Vector3(a[0] + b.x, a[1] + b.y, a[2] + b.z);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(Vector3 a, VectorX b)
        {
            if (b.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot add a Vector3 to a Vector{b.GetSize()}");

            return new Vector3(a.x + b[0], a.y + b[1], a.z + b[2]);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(VectorX a, Vector3 b)
        {
            if (a.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot subtract a Vector{a.GetSize()} by a Vector3");

            return new Vector3(a[0] - b.x, a[1] - b.y, a[2] - b.z);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 a, VectorX b)
        {
            if (b.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot subtract a Vector3 by a Vector{b.GetSize()}");

            return new Vector3(a.x - b[0], a.y - b[2], a.z - b[2]);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(VectorX a, Vector3 b)
        {
            if (a.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot multiply a Vector{a.GetSize()} by a Vector3");

            return new Vector3(a[0] * b.x, a[1] * b.y, a[2] * b.z);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 a, VectorX b)
        {
            if (b.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot multiply a Vector3 by a Vector{b.GetSize()}");

            return new Vector3(a.x * b[0], a.y * b[1], a.z * b[2]);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(VectorX a, Vector3 b)
        {
            if (a.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot divide a Vector{a.GetSize()} by a Vector3");

            return new Vector3(a[0] / b.x, a[1] / b.y, a[2] / b.z);
        }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 a, VectorX b)
        {
            if (b.GetSize() != 3)
                throw new System.ArithmeticException($"Cannot divide a Vector3 by a Vector{b.GetSize()}");

            return new Vector3(a.z / b[0], a.y / b[1], a.z / b[2]);
        }

        #endregion


        public Vector3 ToVector3()
        {
            #if DEBUG
            if (_size != 3)
                throw new System.InvalidCastException($"Cannot convert a VectorX of size {_size} to a Vector3");
            #endif
            return new Vector3(_values[0], _values[1], _values[2]);
        }
    }
}