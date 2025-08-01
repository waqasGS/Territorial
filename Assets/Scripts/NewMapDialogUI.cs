using UnityEngine;
using UnityEngine.UI;

public class NewMapDialogUI : MonoBehaviour
{
    [Header("Map Name")]
    public InputField nameInput;

    [Header("Size Toggles")]
    public Toggle toggle128;
    public Toggle toggle256;
    public Toggle toggle512;
    private int selectedMapSize = 128;

    [Header("Tile Type Toggles")]
    public Toggle[] tileTypeToggles;
    public TileType[] tileTypes;

    [Header("Create Map")]
    public Button createButton;
    public MapPainterUI mapPainter;

    void Start()
    {
        // Set default size selection
        toggle128.onValueChanged.AddListener(isOn => { if (isOn) selectedMapSize = 128; });
        toggle256.onValueChanged.AddListener(isOn => { if (isOn) selectedMapSize = 256; });
        toggle512.onValueChanged.AddListener(isOn => { if (isOn) selectedMapSize = 512; });
      

        // Label tile toggles with tile type names
        for (int i = 0; i < tileTypeToggles.Length; i++)
        {
            int index = i;
            tileTypeToggles[i].GetComponentInChildren<Text>().text = tileTypes[i].name;

            // Optional: Only allow one tile toggle to be active at a time
            tileTypeToggles[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                {
                    for (int j = 0; j < tileTypeToggles.Length; j++)
                    {
                        if (j != index)
                            tileTypeToggles[j].isOn = false;
                    }
                }
            });
        }

        // Create button logic
        createButton.onClick.AddListener(() =>
        {
            if (string.IsNullOrWhiteSpace(nameInput.text))
            {
                print("Please enter map name");
                return;
            }
            string mapName = nameInput.text;
            TileType selectedTile = tileTypes[0]; // default fallback

            for (int i = 0; i < tileTypeToggles.Length; i++)
            {
                if (tileTypeToggles[i].isOn)
                {
                    selectedTile = tileTypes[i];
                    break;
                }
            }

            mapPainter.CreateNewMap(selectedMapSize, mapName, selectedTile);
            gameObject.SetActive(false); // hide dialog
        });
    }
}
