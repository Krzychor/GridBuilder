using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.Pool;


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
        displayer = InstancedBuilding.TryCreate(building, buildingGrid);
        if (displayer == null)
            displayer = new GameObjectDisplayer(building, buildingGrid);
    }

    public void Update()
    {
        UpdateSelection();

        displayer.Draw();
    }

    public void Cancel()
    {

    }

    public void OnStart()
    {

    }

    private void UpdateDisplay(Vector2Int table)
    {
        displayer.Clear();
        displayer.ReserveSize(table.x, table.y);

        Vector3 buildingSize = new Vector3(buildingGrid.GetSize().x * builder.grid.cellSize, 0,
            buildingGrid.GetSize().y * builder.grid.cellSize);
        int dirX = 1;
        int dirZ = 1;

        Vector2Int buildSizeInCells = buildingGrid.GetSize();
        if (startIndex.x > endIndex.x)
            dirX = -1;
        if (startIndex.z > endIndex.z)
            dirZ = -1;
        for (int x = 0; x < table.x; x++)
            for (int z = 0; z < table.y; z++)
            {
                Vector3 pos = builder.grid.GetCellWorldPosition(startIndex.x + dirX * x * buildSizeInCells.x,
                    startIndex.z + dirZ * z * buildSizeInCells.y);
                if (builder.grid.CanPlace(pos, buildingGrid))
                {
                    displayer.Add(pos);
                }
            }
    }

    private void UpdateSelection()
    {
        bool raycast = builder.RaycastMouse(out Vector3 pos);

        if (isSelecting && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            FinishSelection();
            isSelecting = false;
        }

        if (isSelecting && Mouse.current.leftButton.isPressed)
        {
            if (raycast)
            {
                endIndex = builder.grid.GetCell(pos);
                if (builder.grid.IsInsideGrid(endIndex.x, endIndex.z))
                {
                    Vector2Int size = new Vector2Int(Math.Abs(endIndex.x - startIndex.x), Math.Abs(endIndex.z - startIndex.z));
                    Vector2Int table = new Vector2Int(size.x / buildingGrid.GetSize().x,
                        size.y / buildingGrid.GetSize().y);
                    if (table.x == 0)
                        table.x = 1;
                    if (table.y == 0)
                        table.y = 1;
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

        if (Keyboard.current[Key.R].wasPressedThisFrame)
            buildingGrid.RotateRight();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (raycast)
            {
                startIndex = builder.grid.GetCell(pos);
                endIndex = builder.grid.GetCell(pos);
                if (builder.grid.IsInsideGrid(startIndex.x, startIndex.z))
                    isSelecting = true;
            }
        }


    }

    private void FinishSelection()
    {
        displayer.ForEach((Vector3 pos) =>
        {
            GameObject placed = builder.grid.PlaceBuilding(building,
                pos, buildingGrid);

            if (placed != null)
                builder.onBuildingPlaced?.Invoke(placed, building);
        });
        displayer.Clear();

        OnStart();
    }

    private interface BuildingDisplayer
    {
        public void ForEach(Action<Vector3> action);

        public void ReserveSize(int sizeX, int sizeZ);

        public void Clear();

        public void Add(Vector3 pos);

        public void Draw();
    }

    private class InstancedBuilding : BuildingDisplayer
    {
        BuildingGridInstance buildingGrid;
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

        public static InstancedBuilding TryCreate(Building building, BuildingGridInstance buildingGrid)
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
            instance.buildingGrid = buildingGrid;
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

        public void Add(Vector3 pos)
        {
            Quaternion rotation = buildingGrid.GetRotation();
            pos += meshShift;
            Matrix4x4 mat = Matrix4x4.TRS(pos, rotation, scale);
            matrices.Add(mat);
        }
    }

    private class GameObjectDisplayer: BuildingDisplayer
    {
        List<Transform> objects = new();
        ObjectPool<GameObject> pool;
        Building building;
        BuildingGridInstance grid;

        public GameObjectDisplayer(Building building, BuildingGridInstance grid)
        {
            this.building = building;
            this.grid = grid;
            pool = new ObjectPool<GameObject>(OnCreateGameObject, OnTakeFromPool, OnReturnedToPool, OnDestroyPoolObject, 
                false, defaultCapacity: 100, maxSize: 300);
        }

        public void Add(Vector3 pos)
        {
            GameObject G = pool.Get();
            G.transform.position = pos;
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
         //   objects.Count = sizeX * sizeZ;
        }

        private GameObject OnCreateGameObject()
        {
            if(building.preview != null)
                return GameObject.Instantiate(building.preview);
            return GameObject.Instantiate(building.model);
        }

        void OnReturnedToPool(GameObject obj)
        {
            obj.SetActive(false);
        }

        void OnTakeFromPool(GameObject obj)
        {
            obj.SetActive(true);
        }

        void OnDestroyPoolObject(GameObject obj)
        {
            GameObject.Destroy(obj);
        }
    }

}
