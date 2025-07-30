using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapPainterUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public RawImage rawImage;
    public Color backgroundColor = Color.white;

    public bool isErasing = false;

    private Texture2D texture;
    private int textureSize = 512;
    public int BrushSize = 5;

    public TerrainType[] terrainTypes;


    public int ColorIndex;
    public Color paintColor = Color.red;

    void Start()
    {
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        int centerX = textureSize / 2;
        int centerY = textureSize / 2;
        int radius = textureSize / 2;
        int sqrRadius = radius * radius;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                bool insideCircle = dx * dx + dy * dy <= sqrRadius;

                texture.SetPixel(x, y, insideCircle ? backgroundColor : Color.clear);
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPos
        );

        Rect rect = rawImage.rectTransform.rect;
        float uvX = (localPos.x - rect.x) / rect.width;
        float uvY = (localPos.y - rect.y) / rect.height;

        int centerX = Mathf.FloorToInt(uvX * texture.width);
        int centerY = Mathf.FloorToInt(uvY * texture.height);

        int halfBrush = Mathf.Max(1, BrushSize / 2);

        Color c = terrainTypes[ColorIndex].color;
        c.a = 1f;
        paintColor = c;

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


    public void ToggleErase()
    {
        isErasing = !isErasing;
    }
}
