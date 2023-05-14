using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GridData : MonoBehaviour
{
    public float maxHeight = 10;
    public int size = 10;
    public float cellSize = 1;

    [SerializeField, HideInInspector]
    bool[] placeable;
    [SerializeField, HideInInspector]
    public List<PlacedBuilding> placedBuildings = new();

    Action onGridChanged;

    public void RegisterOnChageListener(Action listener)
    {
        onGridChanged += listener;
    }
    public void UnregisterOnChageListener(Action listener)
    {
        onGridChanged -= listener;
    }

    public bool IsInsideGrid(Vector3 point)
    {
        return point.x < size * cellSize + GetPosition().x && point.x >= GetPosition().x &&
        point.z < size * cellSize + GetPosition().z && point.z >= GetPosition().z;
    }

    public bool IsInsideGrid(int cellIndexX, int cellIndexZ)
    {
        return cellIndexX >= 0 && cellIndexZ >= 0 && cellIndexX < size && cellIndexZ < size;
    }

    public bool CanPlace(Vector3 point, BuildingGridInstance buildingGrid)
    {
        if (!IsInsideGrid(point))
            return false;

        Vector3Int cell = GetCell(point);
        return CanPlace(cell.x, cell.z, buildingGrid);
    }

    public bool CanPlace(int cellX, int cellZ)
    {
        return placeable[cellX + cellZ * size];
    }

    public bool CanPlace(int cellX, int cellZ, BuildingGridInstance buildingGrid)
    {
        Vector2Int min = buildingGrid.Min();
        Vector2Int max = buildingGrid.Max();
        for (int x = min.x; x <= max.x; x++)
            for (int z = min.y; z <= max.y; z++)
            {
                if (IsInsideGrid(cellX + x, cellZ + z))
                {
                    if (buildingGrid.Get(x, z) == true)
                    {
                        if (placeable[cellX + x + (cellZ + z) * size] == false)
                            return false;
                    }
                }
                else
                    return false;
            }

        return true;
    }

    public bool TryPlace(Vector3 point, BuildingGridInstance buildingGrid, Building building, GameObject gameObject)
    {
        if (!CanPlace(point, buildingGrid))
            return false;

        Vector3Int cell = GetCell(point);
        Place(cell, buildingGrid, building, gameObject);
        return true;
    }

    public void Place(Vector3Int cell, BuildingGridInstance buildingGrid, Building building, GameObject gameObject)
    {
        PlacedBuilding newPlaced = gameObject.GetComponent<PlacedBuilding>();
        if (newPlaced == null)
            newPlaced = gameObject.AddComponent<PlacedBuilding>();
        newPlaced.cell = cell;
        newPlaced.grid = this;
        newPlaced.building = building;

        placedBuildings.Add(newPlaced);
        SetCells(cell, buildingGrid, true);
    }

    public void Unregister(PlacedBuilding placed)
    {
        BuildingGridInstance buildingGrid = new(placed.building.grid);
        SetCells(placed.cell, buildingGrid, false);
    }

    public bool TryFind(GameObject building, out PlacedBuilding result)
    {
        result = null;
        foreach(PlacedBuilding B in placedBuildings)
        {
            if (B.building == building)
            {
                result = B;
                return true;
            }
        }
        return false;
    }

    public void SetCell(int x, int z, bool blocked)
    {
        placeable[x + z * size] = !blocked;
        onGridChanged?.Invoke();
    }

    public void Clear()
    {
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                placeable[x + z * size] = true;
        onGridChanged?.Invoke();
    }

    public void SetCells(Vector3Int cell, BuildingGridInstance buildingGrid, bool blocked)
    {
        Vector2Int min = buildingGrid.Min();
        Vector2Int max = buildingGrid.Max();
        for (int x = min.x; x <= max.x; x++)
            for (int z = min.y; z <= max.y; z++)
            {
                if (IsInsideGrid(cell.x + x, cell.z + z))
                {
                    if (buildingGrid.Get(x, z) == true)
                    {
                        placeable[cell.x + x + (cell.z + z) * size] = !blocked;
                    }
                }
            }
        onGridChanged?.Invoke();
    }

    public Vector3Int GetCell(Vector3 point)
    {
        point -= GetPosition();
        point /= cellSize;
        return new Vector3Int((int)point.x, (int)point.y, (int)point.z);
    }

    public Vector3 GetCellWorldPosition(int x, int z)
    {
        Vector3 pos = new Vector3(cellSize * x, 0, cellSize * z) + GetPosition();
        return pos;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public GameObject TryPlaceBuilding(Building building, Vector3 position,
        BuildingGridInstance grid)
    {
        if (CanPlace(position, grid) == false)
            return null;

        GameObject G = Instantiate(building.model, position, grid.GetRotation());
        TryPlace(position, grid, building, G);
        return G;
    }

    public void GenerateGrid()
    {
        if(size*size == placeable.GetLength(0))
        {
            Clear();
            return;
        }

        placeable = new bool[size * size];
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                placeable[x + z * size] = true;
        onGridChanged?.Invoke();
    }

    private void Awake()
    {
        if (placeable == null || placeable.GetLength(0) != size * size)
            GenerateGrid();
    }

    void OnDrawGizmos()
    {
        if (placeable == null)
            return;
        
        float totalSize = size * cellSize;
        float f = 0.01f;
        Gizmos.color = Color.blue;
        Vector3 pos;
        for (int x = 0; x < size + 1; x++)
        {
            pos = GetPosition() + new Vector3(x * cellSize, f, 0);
            Gizmos.DrawLine(pos, pos + new Vector3(0, 0, totalSize));

        }
        for (int z = 0; z < size + 1; z++)
        {
            pos = GetPosition() + new Vector3(0, f, z * cellSize);
            Gizmos.DrawLine(pos, pos + new Vector3(totalSize, 0, 0));
        }

        /*

        float s = cellSize / 2.0f;
        Vector3 pos = transform.position + new Vector3(s, s, s);
        Vector3 ext = new Vector3(s, s, s);
        for (int x = 0; x < size; x++)
        {
            for(int z = 0; z < size; z++)
            {
                Color color = Color.green;
                if (placeable[x, z] == false)
                    color = Color.red;

                Vector3 center = new Vector3(x*cellSize + pos.x, 0, z*cellSize + pos.z);
           //     Gizmos.DrawCube(center, ext);
            }
        }
        return
        */
    }

}
