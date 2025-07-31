using System;
using System.IO;
using System.Linq;
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
    private MapData currentMapData;

    void Start()
    {
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        currentMapData = new MapData
        {
            width = textureSize,
            height = textureSize,
            data = new PixelData[textureSize * textureSize]
        };

        for (int i = 0; i < currentMapData.data.Length; i++)
        {
            currentMapData.data[i] = new PixelData();
        }

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);
        float radius = textureSize / 2f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);

                if (dist <= radius)
                    texture.SetPixel(x, y, backgroundColor);
                else
                    texture.SetPixel(x, y, new Color(0, 0, 0, 0)); // transparent outside
            }
        }

        texture.Apply();
        rawImage.texture = texture;

        SetBrushShape(BrushShape.Circle, textureCircle);
        brushPreviewImage.color = paintColor;

        toggleCircle.onValueChanged.AddListener(isOn => { if (isOn) SetBrushShape(BrushShape.Circle, textureCircle); });
        toggleSquare.onValueChanged.AddListener(isOn => { if (isOn) SetBrushShape(BrushShape.Square, textureSquare); });
        toggleTriangle.onValueChanged.AddListener(isOn => { if (isOn) SetBrushShape(BrushShape.Triangle, textureTriangle); });
        toggleRandom.onValueChanged.AddListener(isOn => { if (isOn) SetBrushShape(BrushShape.Random, textureRandom); });

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

        brushSizeSlider.minValue = 1f;
        brushSizeSlider.maxValue = 20f;
        brushSizeSlider.value = brushSize;
        brushSizeSlider.onValueChanged.AddListener(val =>
        {
            brushSize = Mathf.RoundToInt(val);
            UpdateBrushPreviewSize();
        });

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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImage.rectTransform, mousePos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out Vector2 localPos);
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
        brushPreviewImage.rectTransform.sizeDelta = new Vector2(brushSize * ratioX, brushSize * ratioY);
    }

    void SetBrushShape(BrushShape shape, Texture2D tex)
    {
        brushShape = shape;
        brushPreviewImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    public void OnPointerDown(PointerEventData eventData) => Paint(eventData);
    public void OnDrag(PointerEventData eventData) => Paint(eventData);

    void Paint(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rawImage.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
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

    public void SaveMap()
    {
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color pixelColor = texture.GetPixel(x, y);
                int index = y * texture.width + x;

                var terrain = terrainTypes.FirstOrDefault(t =>
                    Mathf.Approximately(t.color.r, pixelColor.r) &&
                    Mathf.Approximately(t.color.g, pixelColor.g) &&
                    Mathf.Approximately(t.color.b, pixelColor.b) &&
                    Mathf.Approximately(t.color.a, pixelColor.a));

                if (terrain == null)
                {
                    Debug.LogWarning($"Unmatched color at ({x}, {y}): {pixelColor}");
                }

                currentMapData.data[index].moveSpeed = terrain?.moveSpeed ?? 0f;
                currentMapData.data[index].expandSpeed = terrain?.expandSpeed ?? 0f;
                currentMapData.data[index].populationSpeed = terrain?.populationSpeed ?? 0f;
            }
        }

        string json = JsonUtility.ToJson(currentMapData, true);
        string path = Application.dataPath + "/Resources/map.json";
        File.WriteAllText(path, json);
        Debug.Log("Map saved to: " + path);
    }

    public void LoadMap()
    {
        string path = Application.dataPath + "/Resources/map.json";
        if (!File.Exists(path))
        {
            Debug.LogWarning("Map file not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        currentMapData = JsonUtility.FromJson<MapData>(json);

        for (int y = 0; y < currentMapData.height; y++)
        {
            for (int x = 0; x < currentMapData.width; x++)
            {
                int index = y * currentMapData.width + x;
                PixelData data = currentMapData.data[index];

                Color matchedColor = terrainTypes.FirstOrDefault(t =>
                    Mathf.Approximately(t.moveSpeed, data.moveSpeed) &&
                    Mathf.Approximately(t.expandSpeed, data.expandSpeed) &&
                    Mathf.Approximately(t.populationSpeed, data.populationSpeed))?.color ?? backgroundColor;

                texture.SetPixel(x, y, matchedColor);
            }
        }

        texture.Apply();
        Debug.Log("Map loaded from: " + path);
    }
}
