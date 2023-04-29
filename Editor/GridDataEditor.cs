using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;

[CustomEditor(typeof(GridData))]
public class GridDataEditor : Editor
{
    LayerMask obstacleMask;

    public override void OnInspectorGUI()
    {
        GridData grid = target as GridData;
        base.DrawDefaultInspector();
        if(GUILayout.Button("Clear"))
        {
            grid.GenerateGrid();
        }
        LayerMask tempMask = EditorGUILayout.MaskField(InternalEditorUtility.LayerMaskToConcatenatedLayersMask(obstacleMask), InternalEditorUtility.layers);
        obstacleMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(tempMask);

        if (GUILayout.Button("Generate"))
        {
            Generate(grid);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(grid);
            EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
        }
    }

    private void Generate(GridData grid)
    {
        grid.GenerateGrid();

        for (int x = 0; x < grid.size; x++)
            for(int z = 0; z < grid.size; z++)
            {
                Vector3 pos = grid.GetCellWorldPosition(x, z);
                pos.y = grid.maxHeight;

                pos.x += grid.cellSize;
                pos.z += grid.cellSize;
                if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, grid.maxHeight, obstacleMask))
                {
                    grid.SetCell(x, z, true);
                }
            }


        PlacedBuilding[] placedBuildings = GameObject.FindObjectsOfType<PlacedBuilding>();

        foreach (PlacedBuilding placed in placedBuildings)
        {
            BuildingGridInstance buildingGrid = new(placed.building.grid);

            if (placed.cell.x < 0 || placed.cell.y < 0 || placed.cell.z < 0)
            {
                placed.cell = grid.GetCell(placed.transform.position);
                grid.Place(placed.cell, buildingGrid, placed.building, placed.gameObject);

                EditorUtility.SetDirty(placed);
                EditorSceneManager.MarkSceneDirty(placed.gameObject.scene);
            }
            else
                grid.Place(placed.cell, buildingGrid, placed.building, placed.gameObject);
        }

    }

}
