using UnityEngine;

public static class CanvasGroupExtensions
{
    /// <summary>
    ///     Shows a Canvas Group by setting it's alpha to 1 and enabling interactability.
    /// </summary>
    public static void Show(this CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 1.0f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    /// <summary>
    ///     Hides a Canvas Group by setting it's alpha to 0 and disabling interactability.
    /// </summary>
    public static void Hide(this CanvasGroup canvasGroup)
    {
        canvasGroup.alpha = 0.0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
