using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class DestroyAction : GridAction
{
    GridBuilder builder;

    PlacedBuilding selected;
    public Material[] originalMaterials;
    public Material[] replacedMaterials;

    public void Cancel() { }

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
    }

    void UpdateSelection()
    {
        GameObject G = builder.RaycastMouse();
        PlacedBuilding newSelected = G != null ? G.GetComponentInParent<PlacedBuilding>() : null;

        if (selected == newSelected)
            return;

        if (selected != null)
            RestoreMaterials(selected.gameObject);

        if (newSelected != null)
            SetInvalidMaterial(newSelected.gameObject);

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

    public void OnClick(bool pressedDown, bool released)
    {
        if(pressedDown && selected != null)
        {
            builder.onBuildingDestroyed?.Invoke(selected);
            if(builder.applyAction)
                GameObject.Destroy(selected.gameObject);
            selected = null;
        }
    }

    public void OnRotateLeft()
    {

    }

    public void OnRotateRight()
    {

    }
}


