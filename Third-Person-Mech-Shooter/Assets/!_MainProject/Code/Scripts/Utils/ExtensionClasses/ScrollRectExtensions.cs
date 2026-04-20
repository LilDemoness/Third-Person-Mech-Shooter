using UnityEngine;
using UnityEngine.UI;

public static class ScrollRectExtensions
{
    /// <inheritdoc cref="ScrollTo(ScrollRect, RectTransform, float)"/>
    public static void ScrollTo(this ScrollRect scrollRect, Transform target, float padding = 0.0f) => scrollRect.ScrollTo(target as RectTransform, padding);
    /// <summary>
    ///     Scrolls a RectTransform along the horizontal and vertical axies to have <paramref name="child"/> be visible with the desired padding.<br/>
    ///     Ignores horizontal movement if the scroll rect has horizontal disabled, with the same corresponding to vertical.
    /// </summary>
    /// <param name="scrollRect"></param>
    /// <param name="child"></param>
    /// <param name="padding"></param>
    public static void ScrollTo(this ScrollRect scrollRect, RectTransform child, float padding = 0.0f)
    {
        Canvas.ForceUpdateCanvases();
        // Cache repeatedly required values)
        float viewportWidth = scrollRect.viewport.rect.width;
        float viewportHeight = scrollRect.viewport.rect.height;
        Vector2 scrollPosition = scrollRect.content.anchoredPosition;

        // Calculate the Top-Left and Bottom-Right corners of our child element.
        Vector2 elementTopLeft = child.anchoredPosition + new Vector2(-child.pivot.x * child.rect.width, (1.0f - child.pivot.y) * child.rect.height);
        Vector2 elementBottomright = elementTopLeft + new Vector2(child.rect.width, -child.rect.height);

        // Calculate the Top-Left and Bottom-Right corners of the visible area (Accounting for padding).
        Vector2 visibleContentTopLeft = new Vector2(
            -scrollPosition.x + padding,
            -scrollPosition.y - padding
            );
        Vector2 visibleContentBottomRight = new Vector2(
            -scrollPosition.x + viewportWidth - padding,
            -scrollPosition.y - viewportHeight + padding
            );

        if (scrollRect.vertical)
        {
            // Handle keeping the child in view vertically.
            float scrollDelta =
                elementTopLeft.y > visibleContentTopLeft.y ? visibleContentTopLeft.y - elementTopLeft.y :
                elementBottomright.y < visibleContentBottomRight.y ? visibleContentBottomRight.y - elementBottomright.y :
                0f;

            scrollPosition.y += scrollDelta;
        }
        if (scrollRect.horizontal)
        {
            // Handle keeping the child in view horizontally.
            float scrollDelta =
                elementTopLeft.x < visibleContentTopLeft.x ? visibleContentTopLeft.x - elementTopLeft.x :
                elementBottomright.x > visibleContentBottomRight.x ? visibleContentBottomRight.x - elementBottomright.x :
                0f;

            scrollPosition.x += scrollDelta;
        }

        // Set the position to keep the child in view.
        scrollRect.content.anchoredPosition = scrollPosition;
    }
}
