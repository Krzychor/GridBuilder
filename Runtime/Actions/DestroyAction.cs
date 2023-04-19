using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class DestroyAction : GridAction
{
    GridBuilder builder;
    Material invalidMaterial;

    GameObject selected;
    List<Material> originalMaterials = new();

    public void Cancel()
    {

    }

    public DestroyAction(GridBuilder builder)
    {
        this.builder = builder;
    }

    public void OnStart()
    {

    }

    public void Update()
    {
        UpdateSelection();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            builder.grid.Remove(selected);
        }
    }

    void UpdateSelection()
    {
        GameObject newSelected = builder.RaycastMouse();

        if (selected == newSelected)
            return;

        if (builder.grid.TryFind(newSelected, out PlacedBuilding _) == false)
            return;


        RestoreMaterials(selected);
        SetInvalidMaterial(newSelected);
        selected = newSelected;
    }

    void RestoreMaterials(GameObject building)
    {
        if (building == null)
            return;

        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material = originalMaterials[i];
    }

    void SetInvalidMaterial(GameObject building)
    {
        if (building == null)
            return;

        originalMaterials.Clear();
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        originalMaterials.Capacity = renderers.Length;
        foreach (Renderer renderer in renderers)
        {
            originalMaterials.Add(renderer.material);
            renderer.material = invalidMaterial;
        }
    }

}


