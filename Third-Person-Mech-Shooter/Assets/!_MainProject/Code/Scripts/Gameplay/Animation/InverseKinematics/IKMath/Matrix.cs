

namespace Gameplay.Animations.IK
{
    // Note: Eigen's Matrix.h has the following inheritance chain: Matrix > PlainObjectBase > MatrixBase (Via dense_xpr_base::type) > DenseBase > DenseCoeffsBase > EigenBase
    //      For now, follow this structure. Once we've gotten it working, we'll refactor to collapse down to our use-case.
    public class Matrix<TType> : PlainObjectBase<Matrix<TType>, TType>
    {

    }


    public class Matrix3x3f : Matrix<float>
    { }
    public class MatrixXf : Matrix<float>
    { }
    public class VectorXf : Matrix<float>
    { }
}