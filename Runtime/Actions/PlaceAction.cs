using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    }

    public void OnStart()
    {
        placementData.rotatedGrid = new BuildingGridInstance(placementData.building.grid);
        Quaternion rotation = placementData.rotatedGrid.GetRotation();

        GameObject model = placementData.building.model;
        if (placementData.building.preview != null)
            model = placementData.building.preview;

        placementData.buildingPreview = GameObject.Instantiate(model, new Vector3(), rotation);
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

        if (Mouse.current.leftButton.wasPressedThisFrame && builder.IsOverUI() == false)
            Place();
    }

    void PlaceInCell(PlacementData data, Vector3 point)
    {
        Vector3 shift = data.rotatedGrid.template.boundsList[data.rotatedGrid.rotation].max;
        Vector3 pos = builder.grid.GetPosition() + shift
        + new Vector3(data.cell.x * builder.grid.cellSize, point.y, data.cell.z * builder.grid.cellSize);

        if(Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 90, builder.terrainMask))
        {
            pos.y = hit.point.y;
        }
        data.buildingPreview.transform.position = pos;
    }

    void Place()
    {
        PlaceInCell(placementData, placementData.buildingPreview.transform.position);

        GameObject placed = builder.grid.PlaceBuilding(placementData.building,
            placementData.buildingPreview.transform.position, placementData.rotatedGrid);

        GameObject.Destroy(placementData.buildingPreview);
        placementData.buildingPreview = null;
        if (placed != null)
            builder.onBuildingPlaced?.Invoke(placed, placementData.building);

        OnStart();
    }

    void UpdatePeek()
    {
        if (Keyboard.current[Key.R].wasPressedThisFrame == true)
            Rotate();

        if (builder.RaycastMouse(out Vector3 point))
        {
            bool canPlace = builder.grid.CanPlace(point, placementData.rotatedGrid)
                && builder.IsOverUI() == false;

            if (canPlace)
            {
                placementData.cell = builder.grid.GetCell(point);
                placementData.isValid = true;
                PlaceInCell(placementData, point);

                RenderAsValid(placementData.buildingPreview);
            }
            else
            {
                placementData.buildingPreview.transform.position = point;
                placementData.isValid = false;
                RenderAsInvalid(placementData.buildingPreview);
            }
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

    void Rotate()
    {
        Transform transform = placementData.buildingPreview.transform;
        placementData.rotatedGrid.RotateRight();
        transform.rotation = placementData.rotatedGrid.GetRotation();
    }

}