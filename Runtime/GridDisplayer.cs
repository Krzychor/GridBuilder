using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class GridDisplayer : MonoBehaviour
{
    [SerializeField]
    GridData grid;
    public float lineWidth = 0.1f;
    public float lineHeight = 0.1f;
    public Color linesColor = Color.blue;

    [SerializeField]
    public LayerMask terrainMask;
    public LineRenderer[] gridLines = null;



    public void SetGrid(GridData newGrid)
    {
        grid = newGrid;
        PrepareGridLines();
    }

    public void PrepareGridLines()
    {
        if (gridLines.Length == 2)
        {
            Destroy(gridLines[0].gameObject);
            Destroy(gridLines[1].gameObject);
        }
        else
            gridLines = new LineRenderer[2];

        GameObject G = new GameObject("lines0");
        G.transform.position = grid.GetPosition();
        G.transform.SetParent(transform);
        gridLines[0] = G.AddComponent<LineRenderer>();
        PrepareGridHorizontal(gridLines[0]);

        G = new GameObject("lines1");
        G.transform.position = grid.GetPosition();
        G.transform.SetParent(transform);
        gridLines[1] = G.AddComponent<LineRenderer>();
        PrepareGridVertical(gridLines[1]);

        if (enabled == false)
        {
            gridLines[0].enabled = false;
            gridLines[1].enabled = false;
        }
    }

    void OnEnable()
    {
        if (gridLines.Length == 2)
        {
            gridLines[0].enabled = true;
            gridLines[1].enabled = true;
        }


    }

    void OnDisable()
    {
        if (gridLines == null)
            return;

        if (gridLines[0])
            gridLines[0].enabled = false;
        if (gridLines[1])
            gridLines[1].enabled = false;
    }

    void PrepareGridHorizontal(LineRenderer renderer)
    {
        List<Vector3> positions = new();
        Vector3 pos = new Vector3(0, lineHeight, 0) + grid.GetPosition();

        bool goUp = false;
        int dir = 1;
        for (int i = 0; i < grid.size + grid.size + 1; i++)
        {
            pos.y = SampleHeight(pos.x, pos.z) + lineHeight;
            positions.Add(pos);

            if (!goUp)
            {
                for(int j = 0; j < grid.size; j++)
                {
                    pos.x += dir * grid.cellSize;
                    pos.y = SampleHeight(pos.x, pos.z) + lineHeight;
                    positions.Add(pos);
                }
                dir *= -1;
            }
            else
            {
                pos.z += grid.cellSize;
            }
            goUp = !goUp;
        }
        pos.y = SampleHeight(pos.x, pos.z) + lineHeight;
        positions.Add(pos);

        renderer.positionCount = positions.Count;
        renderer.SetPositions(positions.ToArray());
        renderer.startColor = linesColor;
        renderer.endColor = linesColor;
        renderer.widthMultiplier = lineWidth;
    }

    void PrepareGridVertical(LineRenderer renderer)
    {
        List<Vector3> positions = new();
        Vector3 pos = new Vector3(0, lineHeight, 0) + grid.GetPosition();

        bool goUp = false;
        int dir = 1;
        for (int i = 0; i < grid.size + grid.size + 1; i++)
        {
            pos.y = SampleHeight(pos.x, pos.z) + lineHeight;
            positions.Add(pos);

            if (!goUp)
            {
                for (int j = 0; j < grid.size; j++)
                {
                    pos.z += dir * grid.cellSize;
                    pos.y = SampleHeight(pos.x, pos.z) + lineHeight;
                    positions.Add(pos);
                }
                dir *= -1;
            }
            else
            {
                pos.x += grid.cellSize;
            }
            goUp = !goUp;
        }
        pos.y = SampleHeight(pos.x, pos.z) + lineHeight;
        positions.Add(pos);

        renderer.positionCount = positions.Count;
        renderer.SetPositions(positions.ToArray());
        renderer.startColor = linesColor;
        renderer.endColor = linesColor;
        renderer.widthMultiplier = lineWidth;
    }

    float SampleHeight(float worldPosX, float worldPosZ)
    {
        if (Physics.Raycast(new Vector3(worldPosX, grid.maxHeight, worldPosZ), Vector3.down, 
            out RaycastHit hit, grid.maxHeight - grid.transform.position.y, terrainMask))
        {
            return hit.point.y;
        }
        return grid.transform.position.y;
    }
}
