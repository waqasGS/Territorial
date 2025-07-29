using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapPainterUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RawImage rawImage;
    public Color backgroundColor = Color.white; 
    public Color paintColor = Color.red;

    public bool isErasing = false;  // Toggle this in UI to switch between painting and erasing

    private Texture2D texture;
    private int textureSize = 512;
    public int BrushSize = 5;

    void Start()
    {
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                texture.SetPixel(x, y, backgroundColor);  // Use background color
            }
        }
        texture.Apply();

        rawImage.texture = texture;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Paint(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Paint(eventData);
    }

    void Paint(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImage.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPos);

        Rect rect = rawImage.rectTransform.rect;
        float uvX = (localPos.x - rect.x) / rect.width;
        float uvY = (localPos.y - rect.y) / rect.height;

        int centerX = Mathf.FloorToInt(uvX * texture.width);
        int centerY = Mathf.FloorToInt(uvY * texture.height);

        int halfBrush = Mathf.Max(1, BrushSize / 2);
        Color drawColor = isErasing ? backgroundColor : paintColor;

        for (int y = -halfBrush; y <= halfBrush; y++)
        {
            for (int x = -halfBrush; x <= halfBrush; x++)
            {
                int px = centerX + x;
                int py = centerY + y;

                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    texture.SetPixel(px, py, drawColor);
                }
            }
        }

        texture.Apply();
    }

    // Optional: Toggle erase mode from a UI button
    public void ToggleErase()
    {
        isErasing = !isErasing;
    }
}
