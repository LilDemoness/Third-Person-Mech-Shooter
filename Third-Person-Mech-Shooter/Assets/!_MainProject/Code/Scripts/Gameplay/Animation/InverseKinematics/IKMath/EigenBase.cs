namespace Gameplay.Animations
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TDerived"></typeparam>
    /// <remarks>
    ///     Blender Source Code Link: https://github.com/dfelinto/blender/blob/87a0770bb969ce37d9a41a04c1658ea09c63933a/extern/Eigen3/Eigen/src/Core/EigenBase.h
    /// </remarks>
    public abstract class EigenBase<TDerived> where TDerived : EigenBase<TDerived>
    {
        public TDerived GetDerived() => (TDerived)this;


        /// <summary>
        ///     Returns the number of rows.
        /// </summary>
        public int GetRowCount() => GetDerived().GetRowCount();
        /// <summary>
        ///     Returns the number of columns.
        /// </summary>
        public int GetColumnCount() => GetDerived().GetColumnCount();
        /// <summary>
        ///     Returns the number of coefficients.
        /// </summary>
        public int GetSize() => GetRowCount() * GetColumnCount();


        public abstract void EvalTo<TDest>(TDest dest);
        public virtual void AddTo<TDest>(TDest dest) 
        {

        }
        public virtual void SubTo<TDest>(TDest dest)
        {

        }

        public virtual void ApplyThisOnTheRight<TDest>(TDest dest)
        {

        }
        public virtual void ApplyThisOnTheLeft<TDest>(TDest dest)
        {

        }
    }
}