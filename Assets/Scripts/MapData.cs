using System;

[Serializable]
public class PixelData
{
    public float moveSpeed;
    public float expandSpeed;
    public float populationSpeed;
}

[Serializable]
public class MapData
{
    public int width;
    public int height;
    public PixelData[] data;
}
