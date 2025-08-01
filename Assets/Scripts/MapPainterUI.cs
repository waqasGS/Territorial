//MapPainterUI.cs
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
    public Slider brushSizeSlider;
    public Toggle eraseToggle;
    public Canvas canvas;

    [Header("Toggle For Shapes")]
    public Toggle toggleCircle, toggleSquare, toggleTriangle, toggleRandom;

    [Header("Tile Types")]
    public TileType[] tileTypes;
    public Toggle[] tileTypeToggles;

    [Header("Brush Textures")]
    public Texture2D textureCircle;
    public Texture2D textureSquare;
    public Texture2D textureTriangle;
    public Texture2D textureRandom;

    public enum BrushShape { Circle, Square, Triangle, Random }
    public BrushShape brushShape = BrushShape.Circle;

    [Header("Settings")]
    public Color backgroundColor = Color.white;

    private Texture2D texture;
    private int textureSize = 512;
    private int brushSize = 10;
    private Color paintColor;
    private TileType selectedTileType;
    private Tile[] tiles;
    private bool isErasing = false;

    public string mapName;

    void Start()
    {
        paintColor = tileTypes[0].color;
        selectedTileType = tileTypes[0];

        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        tiles = new Tile[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                int index = y * textureSize + x;
                tiles[index] = new Tile(new Vector2Int(x, y), null);
                texture.SetPixel(x, y, backgroundColor);
            }
        }

        texture.Apply();
        rawImage.texture = texture;

        // Set initial brush
        SetBrushShape(BrushShape.Circle, textureCircle);

        // Brush shape toggles
        toggleCircle.onValueChanged.AddListener(isOn => { if (isOn) SetBrushShape(BrushShape.Circle, textureCircle); });
        toggleSquare.onValueChanged.AddListener(isOn => { if (isOn) SetBrushShape(BrushShape.Square, textureSquare); });
        toggleTriangle.onValueChanged.AddListener(isOn => { if (isOn) SetBrushShape(BrushShape.Triangle, textureTriangle); });
        toggleRandom.onValueChanged.AddListener(isOn => { if (isOn) SetBrushShape(BrushShape.Random, textureRandom); });

        // Tile type toggles
        for (int i = 0; i < tileTypeToggles.Length; i++)
        {
            int index = i;
            tileTypeToggles[i].GetComponentInChildren<Text>().text = tileTypes[i].name;
            tileTypeToggles[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    selectedTileType = tileTypes[index];
                    paintColor = selectedTileType.color;
                    if (!isErasing)
                        brushPreviewImage.color = paintColor;
                }
            });
        }

        brushSizeSlider.minValue = 1;
        brushSizeSlider.maxValue = 20;
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

    void UpdateBrushPreviewSize()
    {
        float ratioX = rawImage.rectTransform.rect.width / texture.width;
        float ratioY = rawImage.rectTransform.rect.height / texture.height;
        brushPreviewImage.rectTransform.sizeDelta = new Vector2(brushSize * ratioX, brushSize * ratioY);
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
                if (brushTexture.GetPixel(x, y).a > 0.1f)
                {
                    int px = cx + Mathf.RoundToInt(x * brushSize / (float)brushWidth) - brushSize / 2;
                    int py = cy + Mathf.RoundToInt(y * brushSize / (float)brushHeight) - brushSize / 2;

                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        int index = py * texture.width + px;
                        texture.SetPixel(px, py, color);
                        tiles[index].ApplyTileType(isErasing ? null : selectedTileType);
                    }
                }
            }
        }
    }

    public void SaveMap()
    {
        MapData mapData = new MapData
        {
            width = texture.width,
            height = texture.height,
            data = tiles.Select(t =>
            {
                int index = -1;
                if (t.tileType != null)
                {
                    for (int i = 0; i < tileTypes.Length; i++)
                    {
                        if (tileTypes[i] == t.tileType)
                        {
                            index = i;
                            break;
                        }
                    }
                }

                return new PixelData
                {
                    tileTypeIndex = index,
                    moveSpeed = t.moveSpeed,
                    expandSpeed = t.expandSpeed,
                    populationSpeed = t.populationSpeed
                };
            }).ToArray()
        };

        string path = Application.dataPath + "/Resources/" + mapName + ".json";
        File.WriteAllText(path, JsonUtility.ToJson(mapData, true));
        Debug.Log("Map saved to: " + path);
    }

    public void LoadMap()
    {
        string path = Application.dataPath + "/Resources/" + mapName + ".json";
        if (!File.Exists(path))
        {
            Debug.LogWarning("Map file not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        MapData mapData = JsonUtility.FromJson<MapData>(json);

        textureSize = mapData.width;
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        tiles = new Tile[textureSize * textureSize];

        for (int y = 0; y < mapData.height; y++)
        {
            for (int x = 0; x < mapData.width; x++)
            {
                int index = y * mapData.width + x;
                PixelData data = mapData.data[index];
                TileType matchedType = (data.tileTypeIndex >= 0 && data.tileTypeIndex < tileTypes.Length)
                    ? tileTypes[data.tileTypeIndex]
                    : null;

                tiles[index] = new Tile(new Vector2Int(x, y), matchedType);
                tiles[index].moveSpeed = data.moveSpeed;
                tiles[index].expandSpeed = data.expandSpeed;
                tiles[index].populationSpeed = data.populationSpeed;

                Color color = matchedType != null ? matchedType.color : backgroundColor;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        rawImage.texture = texture;
        Debug.Log("Map loaded from: " + path);
    }

    public void CreateNewMap(int size, string mapName, TileType defaultTile)
    {
        texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        tiles = new Tile[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                tiles[index] = new Tile(new Vector2Int(x, y), defaultTile);
                texture.SetPixel(x, y, defaultTile != null ? defaultTile.color : backgroundColor);
            }
        }

        texture.Apply();
        rawImage.texture = texture;

        this.mapName = mapName;
        Debug.Log("New map created: " + mapName + " Size: " + size + "x" + size);
    }
}
