using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Windows;


public class SelectionPlacementAction : GridAction
{
    GridBuilder builder;
    Building building;
    BuildingGridInstance buildingGrid;

    Vector3Int startIndex;
    Vector3Int endIndex;
    bool isSelecting = false;

    BuildingDisplayer displayer;

    public SelectionPlacementAction(GridBuilder builder, Building building)
    {
        this.builder = builder;
        this.building = building;
        buildingGrid = new BuildingGridInstance(building.grid);
        displayer = InstancedBuilding.TryCreate(building);
        if (displayer == null)
            displayer = new GameObjectDisplayer(building, builder.transform);
    }

    public void Update()
    {
        UpdateSelection();

        displayer.Draw();
    }

    public void Cancel()
    {
        displayer.OnDestroy();
    }

    public void OnStart()
    {

    }

    private void UpdateDisplay(Vector2Int table)
    {
        displayer.Clear();
        displayer.ReserveSize(table.x, table.y);

        Vector2Int buildSizeInCells = buildingGrid.GetSize();
        int dirX = 1;
        int dirZ = 1;
        if (startIndex.x > endIndex.x)
            dirX = -1;
        if (startIndex.z > endIndex.z)
            dirZ = -1;
        Vector3Int minCell = startIndex;
        for (int x = 0; x < table.x; x++)
            for (int z = 0; z < table.y; z++)
            {
                Vector3 pos = builder.grid.GetCellWorldPosition(
                    minCell.x + dirX * x * buildSizeInCells.x + buildingGrid.GetCenter().x,
                    minCell.z + dirZ * z * buildSizeInCells.y + buildingGrid.GetCenter().y);

                Vector3 rayPos = new Vector3(pos.x, builder.camera.transform.position.y, pos.z);
                if (Physics.Raycast(rayPos, Vector3.down, out RaycastHit hit, 900, builder.terrainMask))
                    pos.y = hit.point.y;

                if (builder.grid.CanPlace(pos, buildingGrid))
                    displayer.Add(pos, buildingGrid.GetRotation());
            }
    }

    private void UpdateSelection()
    {
        bool raycast = builder.RaycastMouse(out Vector3 pos);
        if (isSelecting)
        {
            if (raycast)
            {
                Vector2Int buildingGridSize = buildingGrid.GetSize();
                endIndex = builder.grid.GetCell(pos);
                if (builder.grid.IsInsideGrid(endIndex.x, endIndex.z))
                {
                    Vector2Int size = new Vector2Int(Math.Abs(endIndex.x - startIndex.x),
                        Math.Abs(endIndex.z - startIndex.z));
                    Vector2Int table = new Vector2Int(size.x / buildingGridSize.x,
                        size.y / buildingGridSize.y);

                    table.x = Math.Max(1, table.x);
                    table.y = Math.Max(1, table.y);

                    if (table.x * buildingGrid.GetSize().x < size.x)
                        table.x++;
                    if (table.y * buildingGrid.GetSize().y < size.y)
                        table.y++;
                    if (endIndex.x < startIndex.x)
                        table.x++;
                    if (endIndex.z < startIndex.z)
                        table.y++;


                    UpdateDisplay(table);
                }
            }
        }
        else
        {
            if (raycast)
            {
                startIndex = builder.grid.GetCell(pos);
                endIndex = builder.grid.GetCell(pos);
                UpdateDisplay(new Vector2Int(1, 1));
            }
        }


    }

    private void FinishSelection()
    {
        isSelecting = false;
        displayer.ForEach((Vector3 pos) =>
        {
            GameObject placed = builder.grid.TryPlaceBuilding(building,
                pos, buildingGrid);

            if (placed != null)
                builder.onBuildingPlaced?.Invoke(placed, pos, building, buildingGrid, builder.grid.GetCell(pos));
            
        });
        displayer.Clear();

        OnStart();
    }

    private void TryStartSelection()
    {
        if (builder.RaycastMouse(out Vector3 pos))
        {
            startIndex = builder.grid.GetCell(pos);
            endIndex = builder.grid.GetCell(pos);
            if (builder.grid.IsInsideGrid(startIndex.x, startIndex.z))
                isSelecting = true;
        }
    }

    public void OnClick(bool pressedDown, bool released)
    {
        if (isSelecting && released)
            FinishSelection();

        if (pressedDown)
            TryStartSelection();
    }

    public void OnRotateLeft()
    {
        buildingGrid.RotateLeft();
    }

    public void OnRotateRight()
    {
        buildingGrid.RotateRight();
    }

    private interface BuildingDisplayer
    {
        public void ForEach(Action<Vector3> action);

        public void ReserveSize(int sizeX, int sizeZ);

        public void Clear();

        public void Add(Vector3 pos, Quaternion rotation);

        public void Draw();

        public void OnDestroy();
    }

    private class InstancedBuilding : BuildingDisplayer
    {
        Vector3 meshShift;
        Vector3 scale;
        Mesh mesh;
        Material material;
        List<Matrix4x4> matrices = new();

        private InstancedBuilding() { }

        public void ForEach(Action<Vector3> action)
        {
            foreach (Matrix4x4 matrix in matrices)
                action(matrix.GetPosition());
        }

        public void Draw()
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
        }

        public void ReserveSize(int sizeX, int sizeZ)
        {
            matrices.Capacity = sizeX * sizeZ;
        }

        public static InstancedBuilding TryCreate(Building building)
        {
            MeshFilter filter = null;
            GameObject model = null;

            MeshFilter[] filters = building.model.GetComponentsInChildren<MeshFilter>();
            if (filters.Length != 1 && building.preview == null)
                return null;

            if (building.preview != null)
            {
                MeshFilter[] previewFilters = building.preview.GetComponentsInChildren<MeshFilter>();

                if (previewFilters.Length == 1)
                {
                    model = building.preview;
                    filter = previewFilters[0];
                }
                else if (filters.Length != 1)
                    return null;
            }
            else
            {
                if (filters.Length != 1)
                    return null;

                model = building.model;
                filter = filters[0];
            }


            InstancedBuilding instance = new InstancedBuilding();
            instance.mesh = filter.sharedMesh;
            instance.material = model.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            instance.scale = filter.transform.lossyScale;
            instance.meshShift = filter.transform.position;
            if (building.preview != null)
            {
                instance.mesh = model.GetComponent<MeshFilter>().mesh;
                instance.material = model.GetComponentInChildren<MeshRenderer>().material;
                instance.scale = filter.transform.lossyScale;
                instance.meshShift = filter.transform.position;
            }
            return instance;
        }

        public void Clear()
        {
            matrices.Clear();
        }

        public void Add(Vector3 pos, Quaternion rotation)
        {
            pos += meshShift;
            Matrix4x4 mat = Matrix4x4.TRS(pos, rotation, scale);
            matrices.Add(mat);
        }

        public void OnDestroy()
        {

        }
    }

    private class GameObjectDisplayer: BuildingDisplayer
    {
        List<Transform> objects = new();
        ObjectPool<GameObject> pool;
        Building building;

        Transform poolParent;

        public GameObjectDisplayer(Building building, Transform gridBuilder)
        {
            poolParent = new GameObject().transform;
            poolParent.SetParent(gridBuilder);
            poolParent.gameObject.name = "massplacing pool";

            this.building = building;
            pool = new ObjectPool<GameObject>(OnCreatePoolGameObject, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, 
                false, defaultCapacity: 100, maxSize: 300);
        }

        public void Add(Vector3 pos, Quaternion rotation)
        {
            GameObject G = pool.Get();
            G.transform.position = pos;
            G.transform.rotation = rotation;
            objects.Add(G.transform);
        }

        public void Clear()
        {
            foreach(Transform placed in objects)
            {
                pool.Release(placed.gameObject);
            }
            objects.Clear();
        }

        public void Draw()
        {

        }

        public void ForEach(Action<Vector3> action)
        {
            foreach (Transform transform in objects)
                action(transform.position);
        }

        public void ReserveSize(int sizeX, int sizeZ)
        {

        }

        private GameObject OnCreatePoolGameObject()
        {
            if(building.preview != null)
                return GameObject.Instantiate(building.preview, poolParent);
            return GameObject.Instantiate(building.model, poolParent);
        }

        private void OnReturnedToPool(GameObject obj)
        {
            obj.SetActive(false);
        }

        private void OnTakeFromPool(GameObject obj)
        {
            obj.SetActive(true);
        }

        private void OnDestroyPoolObject(GameObject obj)
        {
            GameObject.Destroy(obj);
        }

        public void OnDestroy()
        {
            GameObject.Destroy(poolParent.gameObject);
        }
    }

}
