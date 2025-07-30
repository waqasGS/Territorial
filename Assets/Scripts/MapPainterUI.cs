using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapPainterUI : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [Header("UI References")]
    public RawImage rawImage;
    public Image brushPreviewImage;
    public Toggle[] terrainToggles;
    public Toggle toggleCircle, toggleSquare, toggleTriangle, toggleRandom;
    public Slider brushSizeSlider;

    [Header("Settings")]
    public Color backgroundColor = Color.white;
    public bool isErasing = false;
    public TerrainType[] terrainTypes;

    [Header("Brush Textures")]
    public Texture2D textureCircle;
    public Texture2D textureSquare;
    public Texture2D textureTriangle;
    public Texture2D textureRandom;

    public enum BrushShape { Circle, Square, Triangle, Random }
    public BrushShape brushShape = BrushShape.Circle;

    private Texture2D texture;
    private int textureSize = 512;
    private int brushSize = 10;
    private Color paintColor = Color.red;

    void Start()
    {
        // Initialize texture
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

        // Assign brush shape toggles
        toggleCircle.onValueChanged.AddListener(isOn =>
        {
            if (isOn) SetBrushShape(BrushShape.Circle, textureCircle);
        });

        toggleSquare.onValueChanged.AddListener(isOn =>
        {
            if (isOn) SetBrushShape(BrushShape.Square, textureSquare);
        });

        toggleTriangle.onValueChanged.AddListener(isOn =>
        {
            if (isOn) SetBrushShape(BrushShape.Triangle, textureTriangle);
        });

        toggleRandom.onValueChanged.AddListener(isOn =>
        {
            if (isOn) SetBrushShape(BrushShape.Random, textureRandom);
        });

        // Assign terrain color toggles
        for (int i = 0; i < terrainToggles.Length; i++)
        {
            int index = i;
            terrainToggles[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    paintColor = terrainTypes[index].color;
                }
            });
        }

        // Brush size slider
        brushSizeSlider.minValue = 1;
        brushSizeSlider.maxValue = 100;
        brushSizeSlider.value = brushSize;
        brushSizeSlider.onValueChanged.AddListener(val =>
        {
            brushSize = Mathf.RoundToInt(val);
        });

        // Set initial preview brush
        SetBrushShape(BrushShape.Circle, textureCircle);
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        // Position brush preview
        brushPreviewImage.transform.position = mousePos;
        brushPreviewImage.rectTransform.sizeDelta = new Vector2(brushSize, brushSize);

        // Show only if inside paint area
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform,
            mousePos,
            null,
            out Vector2 localPos
        );

        Rect rect = rawImage.rectTransform.rect;
        brushPreviewImage.gameObject.SetActive(rect.Contains(localPos));
    }

    void SetBrushShape(BrushShape shape, Texture2D tex)
    {
        brushShape = shape;
        brushPreviewImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    public void ToggleErase() => isErasing = !isErasing;

    public void OnPointerDown(PointerEventData eventData) => Paint(eventData);
    public void OnDrag(PointerEventData eventData) => Paint(eventData);

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

        Color drawColor = isErasing ? backgroundColor : paintColor;

        Texture2D selectedBrush = brushShape switch
        {
            BrushShape.Circle => textureCircle,
            BrushShape.Square => textureSquare,
            BrushShape.Triangle => textureTriangle,
            BrushShape.Random => textureRandom,
            _ => textureCircle
        };

        PaintWithBrushTexture(centerX, centerY, selectedBrush, drawColor);
        texture.Apply();
    }

    void PaintWithBrushTexture(int cx, int cy, Texture2D brushTexture, Color color)
    {
        int brushWidth = brushTexture.width;
        int brushHeight = brushTexture.height;

        for (int y = 0; y < brushHeight; y++)
        {
            for (int x = 0; x < brushWidth; x++)
            {
                Color mask = brushTexture.GetPixel(x, y);
                if (mask.a > 0.1f)
                {
                    int px = cx + Mathf.RoundToInt(x * brushSize / (float)brushWidth) - brushSize / 2;
                    int py = cy + Mathf.RoundToInt(y * brushSize / (float)brushHeight) - brushSize / 2;

                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }
}
