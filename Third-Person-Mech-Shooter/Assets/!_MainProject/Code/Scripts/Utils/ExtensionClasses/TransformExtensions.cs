using UnityEngine;

public static class TransformExtensions
{
    /// <summary>
    ///     Returns true if <paramref name="thisTransform"/> is a child of <paramref name="potentialParent"/>.
    /// </summary>
    /// <param name="potentialParent"> The Transform that is potentially the parent of <paramref name="thisTransform"/>.</param>
    /// <param name="checkDepth"> If positive, restricts the search to that many levels down the hierarchy.</br> Set to 1 to check the parent's immediate children only.</param>
    public static bool IsChildOf(this Transform thisTransform, Transform potentialParent, int checkDepth = -1)
    {
        if (checkDepth != -1 && checkDepth == 0)
            // Reached the maximum depth without confirming the check transform as a child of the parent.
            return false;


        int newDepth = checkDepth != -1 ? checkDepth - 1 : -1;
        foreach (Transform child in potentialParent)
        {
            if (child == thisTransform)
                return true;    // This transform is a child of this parent.
            if (IsChildOf(thisTransform, child, newDepth))
                return true;    // This transform was a child of one of the child's children.
        }

        // Failed to find this transform within parent's children.
        return false;
    }
}
