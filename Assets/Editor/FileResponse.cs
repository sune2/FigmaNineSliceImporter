using System;

[Serializable]
public class FileResponse
{
    public Node document;
}

[Serializable]
public class Node
{
    public string name;
    public Node[] children;
    public string id;
    public string type;
    public LayoutConstraint constraints;
    public Rectangle absoluteRenderBounds;
}

[Serializable]
public class Rectangle
{
    public float x;
    public float y;
    public float width;
    public float height;

    public float MaxX => x + width;
    public float MaxY => y + height;
}

[Serializable]
public class LayoutConstraint
{
    public string horizontal;
    public string vertical;
}
