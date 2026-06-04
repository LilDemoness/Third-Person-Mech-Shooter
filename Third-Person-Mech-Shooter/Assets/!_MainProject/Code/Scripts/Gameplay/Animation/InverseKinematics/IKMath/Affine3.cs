using UnityEngine;

namespace Gameplay.Animations
{
    public class Affine3
    {
        //typedef Transform<double,3,Affine> Affine3d;

        //template<typename _Scalar, int _Dim, int _Mode, int _Options>
        //class Transform

        private MatrixX _matrix;


        public Affine3()
        {
            _matrix = new MatrixX(3, 4);
        }
        public Affine3(Affine3 other)
        {
            _matrix = other._matrix;
        }
        public Affine3(Matrix3x3 linear, Vector3 translation)
        {
            _matrix = new MatrixX(3, 4);
            _matrix.Set3x3Submatrix(linear, 0, 0);
            _matrix.SetColumn(3, translation.ToVectorX());
        }


        public float this[int row, int column]
        {
            get => _matrix[row, column];
            set => _matrix[row, column] = value;
        }


        /// <summary>
        ///     Returns the linear part of the transformation.
        /// </summary>
        public Matrix3x3 GetLinear() => _matrix.Get3x3Submatrix(0, 0);
        public void SetLinear(Matrix3x3 newLinear) => _matrix.Set3x3Submatrix(newLinear, 0, 0);


        /*/// <summary>
        ///     Returns the 3x4 Affine part of the transformation.
        /// </summary>
        /// <returns></returns>
        public Matrix3x3 GetAffine() => ;
        public void SetAffine(Matrix3x3 newAffine) => ;*/


        /// <summary>
        ///     Returns the translation vector of the transformation.
        /// </summary>
        public Vector3 GetTranslation() => _matrix.GetColumn(3).ToVector3();
        public void SetTranslation(Vector3 newTranslation)
        {
            _matrix[0, 3] = newTranslation[0];
            _matrix[1, 3] = newTranslation[1];
            _matrix[2, 3] = newTranslation[2];
        }



        #region Operator Overloading

        /// <summary>
        ///     Concatenates two Affine3 Transformations.
        /// </summary>
        public static Affine3 operator *(Affine3 a, Affine3 b)
        {
            return new Affine3(
                linear: a.GetLinear() * b.GetLinear(),
                translation: a.GetLinear() * b.GetTranslation() + a.GetTranslation()
                );
        }

        #endregion
    }



    public static class Vector3Extensions
    {
        public static VectorX ToVectorX(this Vector3 vector) => new VectorX(vector);
    }
}