using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingGridTemplate
{
    public Vector2Int gridSize;
    public bool[] grid;
    public List<Bounds> boundsList;
    public Vector2Int defaultCenter;

    public BuildingGridTemplate(Vector2Int size, List<Bounds> boundsList)
    {
        this.boundsList = boundsList;
        this.gridSize = size;
        grid = new bool[size.x * size.y];
        defaultCenter = size / 2;
    }

    public bool IsValid()
    {
        return boundsList.Count > 0;
    }

    public void Set(int x, int z, bool value)
    {
        grid[x + z * gridSize.x] = value;
    }

    public bool Get(int x, int z)
    {
        return grid[x + z * gridSize.x];
    }

}

public class BuildingGridInstance
{
    public BuildingGridTemplate template { get; private set; }
    public int rotation;


    public Quaternion GetRotation()
    {
        return Quaternion.Euler(0, 90 * rotation, 0);
    }

    public Vector2Int GetCenter()
    {
        int Lx = template.defaultCenter.x;
        int Ly = template.defaultCenter.y;
        int Rx = template.gridSize.x - 1 - Lx;
        int Ry = template.gridSize.y - 1 - Ly;
        int mode = rotation % 4;

        if (mode == 0)
            return template.defaultCenter;
        if (mode == 1)
            return new Vector2Int(Ly, Rx);
        if (mode == 2)
            return new Vector2Int(Rx, Ry);

        return new Vector2Int(Ry, Lx);
    }

    public Vector2Int GetSize()
    {
       if(rotation % 2 == 0)
            return template.gridSize;
        return new Vector2Int(template.gridSize.y, template.gridSize.x);
    }

    public Vector2Int Min()
    {
        return -GetCenter();
    }

    public Vector2Int Max()
    {
        if (template.gridSize.x == 0 && template.gridSize.y == 0)
            return Vector2Int.zero;
        return GetSize() - new Vector2Int(1, 1) - GetCenter();
    }

    public void Set(int x, int z, bool value)
    {
        Vector2Int p = Unconvert(new Vector2Int(x, z));
        try
        {
            template.Set(p.x, p.y, value);
        }
        catch
        {
            Debug.LogError("failed for " + p.x + ", " + p.y + " rot=" + rotation);
            Debug.Log("min=" + Min() + " center=" + GetCenter());
            Debug.Log("raw=" + x + "," + z);
            return;
        }
    }

    public bool Get(int x, int z)
    {
        Vector2Int p = Unconvert(new Vector2Int(x, z));
        try
        {
            return template.Get(p.x, p.y);
        }
        catch
        {
            if(template == null)
                Debug.LogError("failed for " + p.x +", " + p.y + " rot=" + rotation + "(null template!");
            else
                Debug.LogError("failed for " + p.x + ", " + p.y + " rot=" + rotation);
            Debug.Log("min=" + Min() + " center=" + GetCenter());
            Debug.Log("raw=" + x +"," + z );
            return false;
        }
    }

    public Vector2Int Unconvert(Vector2Int point)
    {
        if (rotation == 0)
            return point + template.defaultCenter;

        if(rotation == 1)
        {
            point += GetCenter();
            return new Vector2Int(point.y, template.gridSize.y - 1 - point.x);
        }
        if (rotation == 2)
        {
            point += GetCenter();
            return new Vector2Int(template.gridSize.x - 1 - point.x, template.gridSize.y - 1 - point.y);
        }

        point += GetCenter();
        return new Vector2Int(template.gridSize.x - 1 - point.y, point.x);
    }

    public BuildingGridInstance(BuildingGridTemplate buildingGridTemplate, int rotation = 0)
    {
        template = buildingGridTemplate;
        this.rotation = rotation;
    }

    public void RotateLeft()
    {
        rotation--;
        if (rotation < 0)
            rotation = 3;
    }

    public void RotateRight()
    {
        rotation = (rotation+1)%4;
    }

    public void SetRotation(int rotation)
    {
        this.rotation = rotation % 4;

    }

}


[Serializable, 
    CreateAssetMenu(fileName = "Building", menuName = "ScriptableObjects/Building", order = 4)]
public class Building : ScriptableObject
{
    public new string name;
    public GameObject model;
    public GameObject preview;
    [HideInInspector] public BuildingGridTemplate grid;
    public PlacementValidator placementValidator;
    public CustomSelectionDisplayer customSelectionDisplayer;
}

