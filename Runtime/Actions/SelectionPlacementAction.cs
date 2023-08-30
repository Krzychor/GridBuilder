using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public interface ISelectionValidator
{
    public bool Validate(List<Vector3Int> cells);
}




public class SelectionPlacementAction : GridAction
{
    GridBuilder builder;
    Building building;
    BuildingGridInstance buildingGrid;

    Vector3Int startIndex;
    Vector3Int endIndex;
    bool isSelecting = false;
    List<Vector3Int> selectedCells = new();

    BuildingDisplayer displayer;
    CustomSelectionDisplayer customDisplayer;

    public SelectionPlacementAction(GridBuilder builder, Building building)
    {
        this.builder = builder;
        this.building = building;
        buildingGrid = new BuildingGridInstance(building.grid);

        if(building.customSelectionDisplayer != null)
        {
            customDisplayer = GameObject.Instantiate(building.customSelectionDisplayer);
        }
        else
        {
            displayer = InstancedBuilding.TryCreate(building);
            if (displayer == null)
                displayer = new GameObjectDisplayer(building, builder.transform);
        }
    }

    public void Update()
    {
        UpdateSelection();

        if (customDisplayer != null)
            customDisplayer.Display(selectedCells, builder.grid);
        else
            displayer.Display(selectedCells, buildingGrid.GetRotation(), builder.grid);
    }

    public void Cancel()
    {
        if (customDisplayer != null)
            GameObject.Destroy(customDisplayer.gameObject);
        else
            displayer.OnDestroy();
        customDisplayer = null;
    }

    public void OnStart()
    {

    }


    private void UpdateDisplay(Vector2Int table)
    {
        selectedCells.Clear();

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
                Vector3Int cell = new(minCell.x + dirX * x * buildSizeInCells.x + buildingGrid.GetCenter().x,
                    0,
                    minCell.z + dirZ * z * buildSizeInCells.y + buildingGrid.GetCenter().y);

                Vector3 pos = builder.grid.GetCellWorldPosition(cell.x, cell.z);

                Vector3 rayPos = new Vector3(pos.x, builder.camera.transform.position.y, pos.z);
                if (Physics.Raycast(rayPos, Vector3.down, out RaycastHit hit, 900, builder.terrainMask))
                    pos.y = hit.point.y;

                if (builder.grid.CanPlace(pos, buildingGrid))
                    selectedCells.Add(cell);
            }

        if (building.placementValidator != null)
            building.placementValidator.Validate(ref selectedCells, builder.grid, building);
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

        if(customDisplayer != null)
        {

        }
        else
        {

        }


        foreach(Vector3Int cell in selectedCells)
        {
            Vector3 pos = builder.grid.GetCellWorldPosition(cell.x, cell.z);
            if (builder.applyAction)
            {
                GameObject placed = builder.grid.TryPlaceBuilding(building, pos, buildingGrid);

                builder.onBuildingPlaced?.Invoke(placed, pos, building, buildingGrid, cell);
            }
            else
                builder.onBuildingPlaced?.Invoke(null, pos, building, buildingGrid, cell);

        }

        if (builder.GetAction() == this)
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
        public void Display(List<Vector3Int> cells, Quaternion rotation, GridData grid);

        public void OnUpdate();

        public void OnDestroy();
    }

    private class InstancedBuilding : BuildingDisplayer
    {
        Vector3 meshShift;
        Vector3 scale;
        Mesh mesh;
        Material material;
        List<Matrix4x4> matrices = new();



        public void OnUpdate()
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
        }

        public static InstancedBuilding TryCreate(Building building)
        {
            MeshFilter filter = null;
            GameObject model = null;

            MeshFilter[] filters = null;
            if (building.model != null)
            {
                filters = building.model.GetComponentsInChildren<MeshFilter>();
            }

            if ((filters == null || filters.Length < 1) && building.preview == null)
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

            if (building.preview != null)
            {
                MeshRenderer renderer = building.preview.GetComponentInChildren<MeshRenderer>();
                if (renderer == null)
                    return null;

                if(renderer.sharedMaterial.enableInstancing == false)
                    return null;

                instance.mesh = filter.sharedMesh;
                instance.material = renderer.sharedMaterial;
                instance.scale = filter.transform.lossyScale;
                instance.meshShift = filter.transform.position;
            }
            else
            {
                MeshRenderer renderer = model.GetComponentInChildren<MeshRenderer>();
                if (renderer == null)
                    return null;

                if (renderer.sharedMaterial.enableInstancing == false)
                    return null;

                instance.mesh = filter.sharedMesh;
                instance.material = renderer.sharedMaterial;
                instance.scale = filter.transform.lossyScale;
                instance.meshShift = filter.transform.position;
            }

            return instance;
        }

        public void OnDestroy()
        {

        }

        public void Display(List<Vector3Int> cells, Quaternion rotation, GridData grid)
        {
            matrices.Clear();
            foreach(Vector3Int cell in cells)
            {
                Vector3 pos = grid.GetCellWorldPosition(cell.x, cell.z) + meshShift;

                matrices.Add(Matrix4x4.TRS(pos, rotation, scale));
            }
        }
    }

    private class GameObjectDisplayer : BuildingDisplayer
    {
        List<Transform> transforms = new();
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

        public void Display(List<Vector3Int> cells, Quaternion rotation, GridData grid)
        {
            foreach (Transform transform in transforms)
                pool.Release(transform.gameObject);
            transforms.Clear();

            foreach (Vector3Int cell in cells)
            {
                Vector3 pos = grid.GetCellWorldPosition(cell.x, cell.z);

                GameObject G = pool.Get();
                G.transform.position = pos;
                G.transform.rotation = rotation;
                transforms.Add(G.transform);
            }
        }

        public void OnUpdate() { }

        public void OnDestroy()
        {
            GameObject.Destroy(poolParent.gameObject);
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

    }

}

