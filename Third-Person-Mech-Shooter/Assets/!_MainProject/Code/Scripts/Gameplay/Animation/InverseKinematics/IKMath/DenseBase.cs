

namespace Gameplay.Animations.IK
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDerived"></typeparam>
    /// <typeparam name="TType"> The type being stored by this <see cref="DenseBase{TDerived, TType}"/>. Sould be a numerical type (int, float, double, etc)</typeparam>
    /// <remarks>
    ///     Blender Source Code Link: https://github.com/dfelinto/blender/blob/87a0770bb969ce37d9a41a04c1658ea09c63933a/extern/Eigen3/Eigen/src/Core/DenseBase.h
    /// </remarks>
    public abstract class DenseBase<TDerived, TType> : DenseCoeffsBase<TDerived, TType> where TDerived : EigenBase<TDerived>
    {
        /// <summary>
        ///     Returns the number of non-zero coefficients
        /// </summary>
        /// <remarks>
        ///     By default, returns the number of stored coefficients.
        /// </remarks>
        public virtual int GetNonZerosCount() => GetSize();


        public abstract void Resize(int newSize);
        public abstract void Resize(int newRows, int newColumns);


        public abstract TTransposeReturnType GetTranspose<TTransposeReturnType>() where TTransposeReturnType : Transpose<TDerived>;
        public abstract void TransposeInPlace();


        /// <summary>
        ///     Sets all coefficients to the corresponding value of '1' for <typeparamref name="TType"/>.
        /// </summary>
        public abstract void SetZeros();
        /// <summary>
        ///     Sets all coefficients to the corresponding value of '1' for <typeparamref name="TType"/>.
        /// </summary>
        public abstract void SetOnes();


        public abstract void Swap<TOther>(DenseBase<TOther, TType> other) where TOther : EigenBase<TOther>;


        /// <summary>
        ///     Returns the sum of all coefficients.
        /// </summary>
        /// <returns></returns>
        public abstract TType GetSum();


        /// <summary>
        ///     Returns the smallest coefficient within the specified range.
        /// </summary>
        public abstract TType GetMinCoeff(int rowStart, int rowEnd, int columnStart, int columnEnd);
        /// <summary>
        ///     Returns the smallest coefficient within the specified range.
        /// </summary>
        public abstract TType GetMinCoeff(int indexStart, int indexEnd);
        /// <summary>
        ///     Returns the smallest coefficient.
        /// </summary>
        public abstract TType GetMinCoeff();


        /// <summary>
        ///     Returns the largest coefficient within the specified range.
        /// </summary>
        public abstract TType GetMaxCoeff(int rowStart, int rowEnd, int columnStart, int columnEnd);
        /// <summary>
        ///     Returns the largest coefficient within the specified range.
        /// </summary>
        public abstract TType GetMaxCoeff(int indexStart, int indexEnd);
        /// <summary>
        ///     Returns the largest coefficient.
        /// </summary>
        public abstract TType GetMaxCoeff();
    }


    public class Transpose<TMatrixBase>
    {

    }
}