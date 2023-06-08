using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceAction : GridAction
{
    public class PlacementData
    {
        public BuildingGridInstance rotatedGrid;
        public GameObject buildingPreview;
        public Building building;
        public Vector3Int cell;
        public bool isValid = false;
        public Material[] originalMaterials;
        public Material[] replacedMaterials;
        public Bounds bounds;
    }

    GridBuilder builder;
    PlacementData placementData = new PlacementData();

    public PlaceAction(Building building, GridBuilder builder)
    {
        this.builder = builder;
        placementData.building = building;
        placementData.rotatedGrid = new BuildingGridInstance(placementData.building.grid);
    }

    public void OnStart()
    {
        Quaternion rotation = placementData.rotatedGrid.GetRotation();

        GameObject model = placementData.building.model;
        if (placementData.building.preview != null)
            model = placementData.building.preview;

        placementData.buildingPreview = GameObject.Instantiate(model, new Vector3(0, 0, 0), rotation);
        placementData.isValid = false;
        placementData.bounds = builder.CalculateBounds(placementData.buildingPreview);
        Renderer[] renderers = placementData.buildingPreview.GetComponentsInChildren<Renderer>();
        placementData.originalMaterials = new Material[renderers.Length];
        placementData.replacedMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            placementData.originalMaterials[i] = renderers[i].material;
            placementData.replacedMaterials[i] = new Material(renderers[i].material)
            {
                color = Color.red
            };
        }

        UpdatePeek();
    }

    public void Cancel()
    {
        if (placementData.buildingPreview == null)
            return;
        GameObject.Destroy(placementData.buildingPreview);
    }

    public void Update()
    {
        UpdatePeek();
    }

    void PlaceInCell(Vector3Int cell)
    {
        Vector3 shift = placementData.rotatedGrid.template.boundsList[placementData.rotatedGrid.rotation].max;
        Vector3 pos = builder.grid.GetPosition() + shift
        + new Vector3(cell.x * builder.grid.cellSize, 0, cell.z * builder.grid.cellSize);

        pos.x -= placementData.rotatedGrid.GetCenter().x * builder.grid.cellSize;
        pos.z -= placementData.rotatedGrid.GetCenter().y * builder.grid.cellSize;

        if(Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 90, builder.terrainMask))
        {
            pos.y = hit.point.y;
        }
        placementData.buildingPreview.transform.position = pos;
    }

    void Place()
    {
        Vector3 pos = placementData.buildingPreview.transform.position;
        GameObject.Destroy(placementData.buildingPreview);
        placementData.buildingPreview = null;

        if (builder.applyAction)
        {
            GameObject placed = builder.grid.TryPlaceBuilding(placementData.building,
                pos, placementData.rotatedGrid);

            builder.onBuildingPlaced?.Invoke(placed, placed.transform.position, placementData.building, placementData.rotatedGrid, placementData.cell);
        }
        else
            builder.onBuildingPlaced?.Invoke(null, pos, placementData.building, placementData.rotatedGrid, placementData.cell);
       
        if(builder.GetAction() == this)
            OnStart();
    }

    void UpdatePeek()
    {
        if (builder.IsOverUI() == false && builder.RaycastMouse(out Vector3 point))
        {
            placementData.buildingPreview.SetActive(true);
            bool canPlace = builder.grid.CanPlace(point, placementData.rotatedGrid);

            if (canPlace)
            {
                placementData.cell = builder.grid.GetCell(point);
                placementData.isValid = true;
                PlaceInCell(placementData.cell);

                RenderAsValid(placementData.buildingPreview);
            }
            else
            {
                Vector3Int cell = builder.grid.GetCell(point);
                PlaceInCell(cell);
                placementData.isValid = false;
                RenderAsInvalid(placementData.buildingPreview);
            }
        }
        else
        {
            placementData.buildingPreview.SetActive(false);
        }
    }

    void RenderAsValid(GameObject building)
    {
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material = placementData.originalMaterials[i];
    }

    void RenderAsInvalid(GameObject building)
    {
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material = placementData.replacedMaterials[i];
    }

    public void OnClick(bool pressedDown, bool released)
    {
        if (released && builder.IsOverUI() == false)
            Place();
    }

    public void OnRotateLeft()
    {
        placementData.rotatedGrid.RotateLeft();
        placementData.buildingPreview.transform.rotation = placementData.rotatedGrid.GetRotation();
    }

    public void OnRotateRight()
    {
        placementData.rotatedGrid.RotateRight();
        placementData.buildingPreview.transform.rotation = placementData.rotatedGrid.GetRotation();
    }
}