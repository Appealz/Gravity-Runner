using UnityEngine;
using UnityEngine.UI;

public class LetterBox : MonoBehaviour
{
    private const float TargetAspectWidth = 21f;
    private const float TargetAspectHeight = 9f;

    private void Awake()
    {
        if (!TryGetComponent(out Camera cam))
        {
            Debug.LogError("Main Camera is missing");
            return;
        }

        float targetAspect = TargetAspectWidth / TargetAspectHeight;
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        Rect rect;

        if (scaleHeight < 1f)
        {
            rect = new Rect(
                0f,
                (1f - scaleHeight) / 2f,
                1f,
                scaleHeight);
        }
        else
        {
            float scaleWidth = 1f / scaleHeight;

            rect = new Rect(
                (1f - scaleWidth) / 2f,
                0f,
                scaleWidth,
                1f);
        }
                
        cam.rect = rect;
                
        CreateBlackBars(rect);
    }

    private void CreateBlackBars(Rect rect)
    {
        GameObject oldOverlay = GameObject.Find("LetterBoxOverlay");

        if (oldOverlay != null)
            Destroy(oldOverlay);

        GameObject overlayObject =
            new GameObject(
                "LetterBoxOverlay",
                typeof(RectTransform),
                typeof(Canvas));

        Canvas canvas = overlayObject.GetComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        CreateBar(
            overlayObject.transform,
            "Top",
            new Vector2(0f, rect.yMax),
            new Vector2(1f, 1f));

        CreateBar(
            overlayObject.transform,
            "Bottom",
            new Vector2(0f, 0f),
            new Vector2(1f, rect.yMin));

        CreateBar(
            overlayObject.transform,
            "Left",
            new Vector2(0f, rect.yMin),
            new Vector2(rect.xMin, rect.yMax));

        CreateBar(
            overlayObject.transform,
            "Right",
            new Vector2(rect.xMax, rect.yMin),
            new Vector2(1f, rect.yMax));
    }

    private void CreateBar(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject barObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

        barObject.transform.SetParent(parent, false);

        RectTransform rectTransform =
            barObject.GetComponent<RectTransform>();

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = barObject.GetComponent<Image>();

        image.color = Color.black;
        image.raycastTarget = false;
    }
}