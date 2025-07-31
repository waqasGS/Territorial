// TileType.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New TileType", menuName = "Map/TileType")]
public class TileType : ScriptableObject
{
    public string tileName;
    public Color color;
    public float moveSpeed;
    public float expandSpeed;
    public float populationSpeed;
}