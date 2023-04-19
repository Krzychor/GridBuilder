using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridDisplayer : MonoBehaviour
{
    [SerializeField]
    GridData grid;
    public float lineWidth = 0.1f;
    public float lineHeight = 0.1f;
    public Color linesColor = Color.blue;

    public LineRenderer[] gridLines = null;



    public void SetGrid(GridData newGrid)
    {
        grid = newGrid;
        PrepareGridLines();
    }

    public void PrepareGridLines()
    {
        Debug.Log("PrepareGridLines");
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

        if(enabled == false)
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
        Vector3[] positions = new Vector3[grid.size + grid.size + 1 + 1];
        int index = 0;
        Vector3 pos = new Vector3(0, lineHeight, 0) + grid.GetPosition();

        bool goUp = false;
        int dir = 1;
        for (int j = 0; j < grid.size + grid.size + 1; j++)
        {
            if (!goUp)
            {
                positions[index] = pos;
                pos.x += dir * grid.cellSize * grid.size;
                dir *= -1;
            }
            else
            {
                positions[index] = pos;
                pos.z += grid.cellSize;
            }
            goUp = !goUp;
            index++;

        }
        positions[index] = pos;

        renderer.positionCount = positions.Length;
        renderer.SetPositions(positions);
        renderer.startColor = linesColor;
        renderer.endColor = linesColor;
        renderer.widthMultiplier = lineWidth;
    }

    void PrepareGridVertical(LineRenderer renderer)
    {
        Vector3[] positions = new Vector3[grid.size + grid.size + 1 + 1];
        int index = 0;
        Vector3 pos = new Vector3(0, lineHeight, 0) + grid.GetPosition();

        bool goUp = false;
        int dir = 1;
        for (int j = 0; j < grid.size + grid.size + 1; j++)
        {
            if (!goUp)
            {
                positions[index] = pos;
                pos.z += dir * grid.cellSize * grid.size;
                dir *= -1;
            }
            else
            {
                positions[index] = pos;
                pos.x += grid.cellSize;
            }
            goUp = !goUp;
            index++;

        }
        positions[index] = pos;

        renderer.positionCount = positions.Length;
        renderer.SetPositions(positions);
        renderer.startColor = linesColor;
        renderer.endColor = linesColor;
        renderer.widthMultiplier = lineWidth;
    }

}
