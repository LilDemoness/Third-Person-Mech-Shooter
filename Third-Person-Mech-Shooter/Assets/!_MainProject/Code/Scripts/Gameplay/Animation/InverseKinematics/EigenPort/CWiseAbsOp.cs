namespace EigenPort
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///     Blender Source Code Link: https://github.com/dfelinto/blender/blob/87a0770bb969ce37d9a41a04c1658ea09c63933a/extern/Eigen3/Eigen/src/Core/CwiseUnaryOp.h#L18
    /// </remarks>
    public class CWiseAbsOp : Matrix
    {
        public CWiseAbsOp(Matrix source)
            : base(source)
        {
            // Unsure how they've done it in Eigen, so we're implementing it this way for now.
            for (int i = 0; i < GetSize(); ++i)
                _values[i] = IKMath.Abs(_values[i]);
        }
    }
}