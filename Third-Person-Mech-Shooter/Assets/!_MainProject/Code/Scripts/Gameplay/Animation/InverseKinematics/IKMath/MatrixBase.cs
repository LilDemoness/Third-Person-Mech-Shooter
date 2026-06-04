using UnityEngine; // Imported for Mathf, _.

namespace Gameplay.Animations.IK
{
    /// <summary>
    ///     The base class for all custom matrices (Including Xx1 matrices such as Vectors).
    /// </summary>
    /// <typeparam name="TDerived"></typeparam>
    /// <typeparam name="TType"> The type being stored by this <see cref="MatrixBase{TDerived, TType}"/>. Sould be a numerical type (int, float, double, etc)</typeparam>
    /// <remarks>
    ///     Blender Source Code Link: https://github.com/dfelinto/blender/blob/87a0770bb969ce37d9a41a04c1658ea09c63933a/extern/Eigen3/Eigen/src/Core/MatrixBase.h
    /// </remarks>

    public abstract class MatrixBase<TDerived, TType> : DenseBase<TDerived, TType> where TDerived : EigenBase<TDerived>
    {
        /// <summary>
        ///     Returns the size of the main diagonal.<br/>
        ///     By default: min(GetRowCount(), GetColumnCount())
        /// </summary>
        public virtual int GetDiagonalSize() => Mathf.Min(GetRowCount(), GetColumnCount());


        /// <summary>
        ///     Returns the magnitude of the matrix.
        /// </summary>
        /// <remarks>
        ///     In blender/eigen, this is 'norm()'.
        /// </remarks>
        public abstract float magnitude { get; }
        /// <summary>
        ///     Returns the square magnitude of the matrix.
        /// </summary>
        /// <remarks>
        ///     In blender/eigen, this is 'squaredNorm()'.
        /// </remarks>
        public abstract float sqrMagnitude { get; }


        public abstract MatrixBase<TDerived, TType> normalized { get; }
        public abstract void Normalize();


        public virtual void ApplyOnTheLeft<TOtherDerived>(EigenBase<TOtherDerived> other) where TOtherDerived : EigenBase<TOtherDerived> => other.ApplyThisOnTheLeft<TDerived>(this.GetDerived());
        public virtual void ApplyOnTheRight<TOtherDerived>(EigenBase<TOtherDerived> other) where TOtherDerived : EigenBase<TOtherDerived> => other.ApplyThisOnTheRight<TDerived>(this.GetDerived());


        //public abstract TTransposeReturnType GetAdjoint<TTransposeReturnType>() where TTransposeReturnType : Transpose<TDerived>;
        public abstract Transpose<TDerived> GetAdjoint();
        public abstract void AdjointInPlace();


        //public abstract TDiagonalReturnType GetDiagonal<TDiagonalReturnType>() where TDiagonalReturnType : Diagonal<TDerived>;
        public abstract Diagonal<TDerived> GetDiagonal();

        //public abstract TTriangularViewReturnType GetTriangularView<TTriangularViewReturnType>(TriangularViewMode mode) where TTriangularViewReturnType : TriangularView<TDerived>;
        public abstract TriangularView<TDerived> GetTriangularView(TriangularViewMode mode);


        /// <summary>
        ///     Sets the values within the matrix to become an identity matrix.
        /// </summary>
        public abstract TDerived SetIdentity();
        /// <summary>
        ///     Resizes the matrix and sets the values within to become an identity matrix.
        /// </summary>
        public abstract TDerived SetIdentity(int rows, int columns);


        public abstract bool IsIdentity(TType precision);
        public abstract bool IsDiagonal(TType precision);
        public abstract bool IsUpperTriangular(TType precision);
        public abstract bool IsLowerTriangular(TType precision);


        #region Operator Overloading



        #endregion


        #region Module Functions

        #region QR Functions

        public abstract HouseholderQR<> GetHouseholderQR();
        public abstract ColPivHouseholderQR<> GetColPivHouseholderQR();
        //public abstract FullPivHouseholderQR<> GetFullPivHouseholderQR();
        //public abstract CompleteOrthogonalDecomposition<> CompleteOrthogonalDecomposition();

        #endregion

        #region SVD Functions

        public abstract JacobiSVD<> GetJacobiSVD(uint computationOptions = 0);
        //public abstract BDCSVD<> GetBDCSVD(uint computationOptions = 0);

        #endregion

        #region Householder Functions

        /// <summary>
        ///     Computes the householder transformation/elemenary reflector 'H' such that:
        ///     - H this = [beta 0 ... 0]^T
        ///     where the transformation H is:
        ///     - H = I - tau * v * v^8
        ///     and the vector v is:
        ///     - v^T = [1 essential^T]
        /// </summary>
        /// <param name="tau"> The scaling factor of the Householder transformation.</param>
        /// <param name="beta"> The result of H * this.</param>
        public void MakeHouseholderInPlace(out TType tau, out TType beta)
        {
            VectorBlock<TDerived> essentialPart = new VectorBlock<TDerived>(GetDerived(), 1, GetSize() - 1);
            MakeHouseholder(essentialPart, out tau, out beta);
        }
        /// <summary>
        ///     Computes the householder transformation/elemenary reflector 'H' such that:
        ///     - H this = [beta 0 ... 0]^T
        ///     where the transformation H is:
        ///     - H = I - tau * v * v^8
        ///     and the vector v is:
        ///     - v^T = [1 essential^T]
        /// </summary>
        /// <param name="essentialPart"> The essential part of the vector v.</param>
        /// <param name="tau"> The scaling factor of the Householder transformation.</param>
        /// <param name="beta"> The result of H * this.</param>
        public void MakeHouseholder<TEssentialPart>(TEssentialPart essentialPart, out TType tau, out TType beta)
        {
            VectorBlock<TDerived> tail = new VectorBlock<TDerived>(GetDerived(), 1, GetSize() - 1);

            float tailSqrMagnitude = GetSize() == 1 ? 0.0f : tail.sqrMagnitude;
            TType c0 = this[0];
        }
        public void ApplyHouseholderOnTheLeft<TEssentialPart>(TEssentialPart essentialPart, TType tau, ref TType[] workspace)
        {

        }
        public void ApplyHouseholderOnTheRight<TEssentialPart>(TEssentialPart essentialPart, TType tau, ref TType[] workspace)
        {

        }

        #endregion

        #region Jacobi Functions

        public abstract void ApplyOnTheLeft<TOtherScalar>(int p, int q, JacobiRotation<TOtherScalar> j);
        public abstract void ApplyOnTheRight<TOtherScalar>(int p, int q, JacobiRotation<TOtherScalar> j);

        #endregion

        #endregion
    }


    public class Diagonal<TDerived>
    { }
    public class TriangularView<TDerived>
    {
    }
    public enum TriangularViewMode
    {
        UnitLower,
        Upper
    }
}