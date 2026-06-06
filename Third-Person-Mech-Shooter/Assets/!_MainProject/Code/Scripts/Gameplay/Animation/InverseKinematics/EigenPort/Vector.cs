namespace EigenPort
{
    // A representation of a Xx1 matrix.
    public class Vector : Matrix
    {
        public Vector(int size)
            : base(size, 1)
        { }

        private Vector(Matrix singleColumnMatrix)
            : base(singleColumnMatrix)
        { }
        public static Vector TryCreateFromMatrix(Matrix source)
        {
            if (source.GetColumnCount() != 1)
                throw new System.Exception($"Cannot convert a {source.GetRowCount()}x{source.GetColumnCount()} matrix to a Vector. Column Count must be 1");

            return new Vector(source);
        }

        public Vector normalized => new Vector(this) / this.magnitude;


        public override void Resize(int rowCount, int columnSize) => throw new System.Exception("You cannot call Resize(int, int) on a Vector. Use Resize(int) instead");
        public void Resize(int newSize) => base.Resize(newSize, 1);

        public Vector GetTail(int size)
        {
            int thisSize = GetSize();

            Vector tail = new Vector(size);
            for(int i = 0; i < size; ++i)
                tail[i] = this[thisSize - i - 1];

            return tail;
        }
        public VectorBlock GetTailRef(int size) => new VectorBlock(this, GetSize() - size - 1, size);
        public VectorBlock GetBlock(int start, int blockSize) => new VectorBlock(this, start, blockSize);


        public static float Dot(Vector lhs, Vector rhs)
        {
            if (lhs.GetSize() != rhs.GetSize())
                throw new System.ArgumentException($"You are trying to find the dot product between a {lhs.GetSize()}d vector and a {rhs.GetSize()}d vector. Vectors must be the same size.");

            float dot = 0.0f;
            for (int i = 0; i < lhs.GetSize(); ++i)
                dot += lhs[i] * rhs[i];

            return dot;
        }


        #region Operator Overloading

        public static Vector operator -(Vector vector)
        {
            Vector result = new Vector(vector.GetSize());
            for (int i = 0; i < vector.GetSize(); i++)
                result[i] = -vector[i];

            return result;
        }

        #region Matrix

        public static Vector operator*(Matrix lhs, Vector rhs)
        {
            if (lhs.GetRowCount() != 1)
                throw new System.ArithmeticException($"You cannot multiply a {lhs.GetRowCount()}x{lhs.GetColumnCount()} matrix with a {rhs.GetRowCount()}d vector.\nThe matrix's row count must equal 1.");

            Vector result = new Vector(rhs.GetSize());
            for (int i = 0; i < rhs.GetSize(); ++i)
                for (int column = 0; column < lhs.GetColumnCount(); column++)
                    result[i] = lhs[i, column] * rhs[i];

            return result;
        }

        #endregion

        #region Vector

        public static Vector operator +(Vector lhs, Vector rhs)
        {
            if (lhs.GetSize() != rhs.GetSize())
                throw new System.ArithmeticException($"You cannot add a {lhs.GetSize()}d vector with a {rhs.GetSize()}d vector.\nSizes must match.");

            Vector result = new Vector(lhs.GetSize());
            for (int i = 0; i < lhs.GetSize(); ++i)
                result[i] = lhs[i] + rhs[i];

            return result;
        }
        public static Vector operator -(Vector lhs, Vector rhs)
        {
            if (lhs.GetSize() != rhs.GetSize())
                throw new System.ArithmeticException($"You cannot subtract a {lhs.GetSize()}d vector by a {rhs.GetSize()}d vector.\nSizes must match.");

            Vector result = new Vector(lhs.GetSize());
            for (int i = 0; i < lhs.GetSize(); ++i)
                result[i] = lhs[i] - rhs[i];

            return result;
        }

        #endregion

        #region Float

        public static Vector operator +(Vector vector, float value)
        {
            Vector result = new Vector(vector.GetSize());
            for (int i = 0; i < vector.GetSize(); ++i)
                result[i] = vector[i] + value;

            return result;
        }
        public static Vector operator +(float value, Vector vector)
        {
            Vector result = new Vector(vector.GetSize());
            for (int i = 0; i < vector.GetSize(); ++i)
                result[i] = value + vector[i];

            return result;
        }

        public static Vector operator -(Vector vector, float value)
        {
            Vector result = new Vector(vector.GetSize());
            for (int i = 0; i < vector.GetSize(); ++i)
                result[i] = vector[i] - value;

            return result;
        }
        public static Vector operator -(float value, Vector vector)
        {
            Vector result = new Vector(vector.GetSize());
            for (int i = 0; i < vector.GetSize(); ++i)
                result[i] = value - vector[i];

            return result;
        }

        public static Vector operator *(Vector vector, float value)
        {
            Vector result = new Vector(vector.GetSize());
            for (int i = 0; i < vector.GetSize(); ++i)
                result[i] = vector[i] * value;

            return result;
        }
        public static Vector operator *(float value, Vector matrix)
        {
            Vector result = new Vector(matrix.GetSize());
            for (int i = 0; i < matrix.GetSize(); ++i)
                result[i] = value * matrix[i];

            return result;
        }

        public static Vector operator /(Vector vector, float value)
        {
            Vector result = new Vector(vector.GetSize());
            for (int i = 0; i < vector.GetSize(); ++i)
                result[i] = vector[i] / value;

            return result;
        }
        public static Vector operator /(float value, Vector vector)
        {
            Vector result = new Vector(vector.GetSize());
            for (int i = 0; i < vector.GetSize(); ++i)
                result[i] = value / vector[i];

            return result;
        }

        #endregion

        #endregion
    }
}