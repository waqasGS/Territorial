using UnityEngine;

[System.Serializable]
public class TerrainType
{
    public string name;
    public Color color;
    public float moveSpeed;
    public float expandSpeed;
    public float populationSpeed;

    public TerrainType(string name, Color color, float moveSpeed, float expandSpeed, float populationSpeed)
    {
        this.name = name;
        this.color = color;
        this.moveSpeed = moveSpeed;
        this.expandSpeed = expandSpeed;
        this.populationSpeed = populationSpeed;
    }
}
