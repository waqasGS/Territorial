// Tile.cs
using UnityEngine;

[System.Serializable]
public class Tile
{
    public Vector2Int position;
    public TileType tileType;

    public float moveSpeed;
    public float expandSpeed;
    public float populationSpeed;

    public Tile(Vector2Int position, TileType type)
    {
        this.position = position;
        ApplyTileType(type);
    }

    public void ApplyTileType(TileType type)
    {
        tileType = type;

        if (type != null)
        {
            moveSpeed = type.moveSpeed;
            expandSpeed = type.expandSpeed;
            populationSpeed = type.populationSpeed;
        }
        else
        {
            moveSpeed = 0;
            expandSpeed = 0;
            populationSpeed = 0;
        }
    }
}