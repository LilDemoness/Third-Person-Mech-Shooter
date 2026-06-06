using UnityEngine;

namespace EigenPort
{
    public class Affine3
    {
        private Matrix _matrix; // 4x4 matrix.
        private Block _rotationPart; // 3x3 of top left corner
        private VectorBlock _translationPart; // 3x1 top of right side

        public Affine3()
        {
            // Create the Affine matrix.
            _matrix = new Matrix(4, 4);
            _matrix.SetZeros();
            _matrix[3,3] = 1;

            _rotationPart = _matrix.GetBlock(0, 0, 3, 3);
            _translationPart = _matrix.GetColumnRef(3).GetTailRef(3);
        }


        public Vector3 GetTranslation() => new Vector3(_translationPart.GetEffectiveMatrix());
        public void SetTranslation(Vector newTranslation)
        {
            if (newTranslation.GetSize() != 3)
                throw new System.ArgumentException($"Cannot set the translation expression of an Affine3 with a {newTranslation.GetSize()}d vector. You must use a 3d vector.");

            _translationPart[0] = newTranslation[0];
            _translationPart[1] = newTranslation[1];
            _translationPart[2] = newTranslation[2];
        }

        public Matrix3x3 GetLinear() => Matrix3x3.TryCreateFromMatrix(_rotationPart.GetEffectiveMatrix());
        public void SetLinear(Matrix newLinear)
        {
            if (newLinear.GetRowCount() != 3 || newLinear.GetColumnCount() != 3)
                throw new System.ArgumentException($"Cannot set the linear expression of an Affine3 with a {newLinear.GetRowCount()}x{newLinear.GetColumnCount()} matrix. You must use a 3x3 matrix.");

            _rotationPart[0,0] = newLinear[0,0];
            _rotationPart[0,1] = newLinear[0,1];
            _rotationPart[0,2] = newLinear[0,2];

            _rotationPart[1,0] = newLinear[1,0];
            _rotationPart[1,1] = newLinear[1,1];
            _rotationPart[1,2] = newLinear[1,2];

            _rotationPart[2,0] = newLinear[2,0];
            _rotationPart[2,1] = newLinear[2,1];
            _rotationPart[2,2] = newLinear[2,2];
        }

        /// <summary>
        ///     Sets the last row of the internal matrix to 0, ..., 0, 1
        /// </summary>
        public void MakeAffine()
        {
            //for (int i = 0; i < _matrix.GetColumnCount(); ++i)
            //    _matrix[_matrix.GetRowCount() - 1, i] = 0;
            //_matrix[_matrix.GetRowCount() - 1, _matrix.GetColumnCount() - 1] = 1;
            _matrix[3, 0] = 0.0f;
            _matrix[3, 1] = 0.0f;
            _matrix[3, 2] = 0.0f;
            _matrix[3, 3] = 1.0f;
        }



        public void Scale(float scale)
        {
            _translationPart[0] *= scale;
            _translationPart[1] *= scale;
            _translationPart[2] *= scale;
        }

        // Concatenates two transformations.
        public static Affine3 operator *(Affine3 lhs, Affine3 rhs)
        {
            Affine3 result = new Affine3();
            result.SetLinear(lhs.GetLinear() * rhs.GetLinear());
            result.SetTranslation(lhs.GetLinear() * rhs.GetTranslation() + lhs.GetTranslation());
            result.MakeAffine();
            return result;
        }
    }
}