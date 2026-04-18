using UnityEngine;
using UnityEngine.UI;

public class FlowLayoutGroup : LayoutGroup {
    public float spacingX = 8f;
    public float spacingY = 8f;
    public float pref = 4f;

    public override void CalculateLayoutInputHorizontal() {
        base.CalculateLayoutInputHorizontal();
        SetLayoutInputForAxis(0, 0, -1, 0);
    }

    public override void CalculateLayoutInputVertical() {
        float width = rectTransform.rect.width;
        SetLayoutInputForAxis(1, GetHeight(width), GetHeight(width), 1);
    }

    public override void SetLayoutHorizontal() => SetLayout();
    public override void SetLayoutVertical() => SetLayout();

    float GetHeight(float containerWidth) {
    float x = padding.left, y = padding.top, rowHeight = 0;
    foreach (RectTransform child in rectChildren) {
        LayoutRebuilder.ForceRebuildLayoutImmediate(child); // add this
        float w = LayoutUtility.GetPreferredWidth(child);
        float h = LayoutUtility.GetPreferredHeight(child);
        if (x + w + padding.right > containerWidth && x > padding.left) {
            x = padding.left;
            y += rowHeight + spacingY;
            rowHeight = 0;
        }
        x += w + spacingX;
        rowHeight = Mathf.Max(rowHeight, h);
    }
    return y + rowHeight + padding.bottom;
}

void SetLayout() {
    float containerWidth = rectTransform.rect.width;
    float x = padding.left, y = padding.top, rowHeight = 0;
    foreach (RectTransform child in rectChildren) {
        LayoutRebuilder.ForceRebuildLayoutImmediate(child); // add this
        float w = LayoutUtility.GetPreferredWidth(child);
        pref = w;
        float h = LayoutUtility.GetPreferredHeight(child);
        if (x + w + padding.right > containerWidth && x > padding.left) {
            x = padding.left;
            y += rowHeight + spacingY;
            rowHeight = 0;
        }
        SetChildAlongAxis(child, 0, x, w);
        SetChildAlongAxis(child, 1, y, h);
        x += w + spacingX;
        rowHeight = Mathf.Max(rowHeight, h);
    }
}
}