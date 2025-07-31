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
    public Toggle eraseToggle;
    public Canvas canvas;

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
        // Initialize paint texture
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

        // Set default brush shape and color
        SetBrushShape(BrushShape.Circle, textureCircle);
        brushPreviewImage.color = paintColor;

        // Brush toggles
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

        // Terrain color selection
        for (int i = 0; i < terrainToggles.Length; i++)
        {
            int index = i;
            terrainToggles[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    paintColor = terrainTypes[index].color;
                    if (!isErasing)
                        brushPreviewImage.color = paintColor;
                }
            });
        }

        // Brush size slider
        brushSizeSlider.minValue = 1f;
        brushSizeSlider.maxValue = 20f;
        brushSizeSlider.value = brushSize;
        brushSizeSlider.onValueChanged.AddListener(val =>
        {
            brushSize = Mathf.RoundToInt(val);
            UpdateBrushPreviewSize();
        });

        // Erase toggle
        eraseToggle.onValueChanged.AddListener(isOn =>
        {
            isErasing = isOn;
            brushPreviewImage.color = isOn ? backgroundColor : paintColor;
        });

        UpdateBrushPreviewSize();
    }

    void Update()
    {
        Vector2 mousePos = Input.mousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rawImage.rectTransform,
            mousePos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPos
        );

        Rect rect = rawImage.rectTransform.rect;

        if (rect.Contains(localPos))
        {
            brushPreviewImage.gameObject.SetActive(true);
            brushPreviewImage.rectTransform.anchoredPosition = localPos;
        }
        else
        {
            brushPreviewImage.gameObject.SetActive(false);
        }
    }

    void UpdateBrushPreviewSize()
    {
        float ratioX = rawImage.rectTransform.rect.width / texture.width;
        float ratioY = rawImage.rectTransform.rect.height / texture.height;

        brushPreviewImage.rectTransform.sizeDelta = new Vector2(
            brushSize * ratioX,
            brushSize * ratioY
        );
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
