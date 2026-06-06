using UnityEngine;

namespace Gameplay.Animations
{
    public static class IKSolver
    {
        /// <summary>
        ///     Aligns the first bone to the target axis, then resets rotation (And optionally position) of all sub-links from the root.
        /// </summary>
        /// <param name="chain"> The <see cref="IKChain"/> to affect.</param>
        /// <param name="target"> The Target Transform we're aiming towards.</param>
        /// <param name="resetPosition"> Should we reset the chain elements positions too?</param>
        public static void FullDirection(IKChain chain, Transform target, bool resetPosition = false)
        {
            // Point the chain in the target's direction.
            IKSolver.AimDirection(chain, target, true);

            // Reset rotation (And optionally position) on all bones except the root.
            for(int i = 1; i < chain.BoneCount; ++i)
            {
                chain.Bones[i].SetRotation(chain.Bones[i].InitialRotation);

                if (resetPosition)
                {
                    chain.Bones[i].SetPosition(chain.Bones[i].InitialPosition);
                }
            }
        }

        /// <summary>
        ///     Aligns the first link of the chain towards the target axis.
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="target"></param>
        /// <param name="doOffset"></param>
        public static void AimDirection(IKChain chain, Transform target, bool doOffset = false)
        {
            chain.Bones[0].SetRotation(target.rotation);

            if (doOffset)
            {
                Quaternion forwardToUp = Quaternion.AngleAxis(90.0f * Mathf.Rad2Deg, Vector3.left);
                chain.Bones[0].AddRotation(forwardToUp);
            }
        }
    }
}