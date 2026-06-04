


// Comment out to remove UnityEngine.Debug.Assert() calls in the this[int] and this[int, int] accessors.
#define ASSERT_IN_ACCESSORS

namespace Gameplay.Animations.IK
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDerived"></typeparam>
    /// <typeparam name="TType"> The type being stored by this <see cref="DenseCoeffsBase{TDerived, TType}"/></typeparam>
    /// <remarks>
    ///     Blender Source Code Link: https://github.com/dfelinto/blender/blob/87a0770bb969ce37d9a41a04c1658ea09c63933a/extern/Eigen3/Eigen/src/Core/DenseCoeffsBase.h
    /// </remarks>
    public abstract class DenseCoeffsBase<TDerived, TType> : EigenBase<TDerived> where TDerived : EigenBase<TDerived>
    {
        //public virtual int GetRowIndexByOuterInner(int outer, int inner);
        //public virtual int GetColumnIndexByOuterInner(int outer, int inner);


        #region 2D Accessor

        public TType this[int row, int column]
        {
            get
            {
                #if ASSERT_IN_ACCESSORS
                UnityEngine.Debug.Assert(row >= 0 && row < GetRowCount() && column >= 0 && column < GetColumnCount());
                #endif
                return GetCoeff(row, column);
            }
            set
            {
                #if ASSERT_IN_ACCESSORS
                UnityEngine.Debug.Assert(row >= 0 && row < GetRowCount() && column >= 0 && column < GetColumnCount());
                #endif
                SetCoeff(value, row, column);
            }
        }

        /// <summary>
        ///     Returns the coefficient at the given row and column.
        ///     Should only be called via <see cref="this[int, int]"/>.
        /// </summary>
        /// <remarks>
        ///     Called by <see cref="this[int, int]"/>'s getter.
        /// </remarks>
        protected virtual TType GetCoeff(int row, int column) => GetCoeff(row + column * row);

        /// <summary>
        ///     Sets the value of the coefficient at the given row and column.
        ///     Should only be called via <see cref="this[int, int]"/>.
        /// </summary>
        /// <remarks>
        ///     Called by <see cref="this[int, int]"/>'s setter.
        /// </remarks>
        protected virtual void SetCoeff(TType value, int row, int column) => SetCoeff(value, row + column * row);

#endregion

        #region 1D Accessor

        public TType this[int index]
        {
            get
            {
                #if ASSERT_IN_ACCESSORS
                UnityEngine.Debug.Assert(index >= 0 && index < GetSize());
                #endif
                return GetCoeff(index);
            }
            set
            {
                #if ASSERT_IN_ACCESSORS
                UnityEngine.Debug.Assert(index >= 0 && index < GetSize());
                #endif
                SetCoeff(value, index);
            }
        }
        /// <summary>
        ///     Returns the coefficient at the given index.
        ///     Should only be called via <see cref="this[int]"/> or <see cref="GetCoeff(int, int)"/>.
        /// </summary>
        /// <remarks>
        ///     Called by <see cref="this[int]"/>'s getter and <see cref="GetCoeff(int, int)"/>.
        /// </remarks>
        protected abstract TType GetCoeff(int index);

        /// <summary>
        ///     Sets the value of the coefficient at the given index.
        ///     Should only be called via <see cref="this[int]"/> or <see cref="SetCoeff(TType, int, int)"/>.
        /// </summary>
        /// <remarks>
        ///     Called by <see cref="this[int]"/>'s setter and <see cref="SetCoeff(TType, int, int)"/>.
        /// </remarks>
        protected abstract void SetCoeff(TType value, int index);

        #endregion


        // Strides.
    }
}