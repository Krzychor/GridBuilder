using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridData))]
public class GridDebugRenderer : MonoBehaviour
{
    private GridData grid;


    void Start()
    {
        grid = GetComponent<GridData>();
    }

    void OnDrawGizmos()
    {
        if (enabled == false)
            return;
        if (grid == null)
            return;
        float size = grid.size * grid.cellSize;
        float f = 0.01f;
        Gizmos.color = Color.blue;
        Vector3 pos;
        for (int x = 0; x < grid.size + 1; x++)
        {
            pos = grid.GetPosition() + new Vector3(x * grid.cellSize, f, 0);
            Gizmos.DrawLine(pos, pos + new Vector3(0, 0, size));

        }
        for (int z = 0; z < grid.size + 1; z++)
        {
            pos = grid.GetPosition() + new Vector3(0, f, z * grid.cellSize);
            Gizmos.DrawLine(pos, pos + new Vector3(size, 0, 0));
        }

    }
}
