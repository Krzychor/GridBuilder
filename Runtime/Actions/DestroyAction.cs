using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;



public class DestroyAction : GridAction
{
    GridBuilder builder;

    GameObject selected;
    public Material[] originalMaterials;
    public Material[] replacedMaterials;

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

        if (newSelected != null)
            newSelected = newSelected.transform.root.gameObject;

        if (selected == newSelected)
            return;

        if (newSelected == null)
            selected = newSelected;
        else if (builder.grid.TryFind(newSelected, out PlacedBuilding _))
        {
            RestoreMaterials(selected);
            SetInvalidMaterial(newSelected);
            selected = newSelected;
        }

    }

    void RestoreMaterials(GameObject building)
    {
        if (building == null)
            return;

        Debug.Log("restore for " + building);
        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material = originalMaterials[i];
    }

    void SetInvalidMaterial(GameObject building)
    {
        if (building == null)
            return;

        Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        replacedMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
            replacedMaterials[i] = new Material(renderers[i].material)
            {
                color = Color.red
            };
            renderers[i].material = replacedMaterials[i];
        }
    }

}


