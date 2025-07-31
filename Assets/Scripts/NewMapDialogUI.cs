//NewMapDialogUI.cs
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class NewMapDialogUI : MonoBehaviour
{
    public InputField nameInput;
    public Slider sizeSlider;
    public Dropdown tileDropdown;
    public Button createButton;
    public MapPainterUI mapPainter;
    public TileType[] tileTypes;


    //128x128
    //256x256
    //256x256

    //toggle button for default tiletypes 

    void Start()
    {
        // Populate dropdown
        tileDropdown.ClearOptions();
        tileDropdown.AddOptions(tileTypes.Select(t => t.name).ToList());

        createButton.onClick.AddListener(() =>
        {
            string mapName = nameInput.text;
            int size = Mathf.RoundToInt(sizeSlider.value);
            TileType defaultTile = tileTypes[tileDropdown.value];

            mapPainter.CreateNewMap(size, mapName, defaultTile);
            gameObject.SetActive(false); // hide dialog
        });
    }
}