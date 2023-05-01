using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildabilityDisplay : MonoBehaviour
{
    public GridData grid;
    [SerializeField]
    public LayerMask terrainMask;
    bool changed = true;

    private void OnDisable()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.enabled = false;
    }

    private void OnEnable()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.enabled = true;
    }

    private void Start()
    {
        grid.RegisterOnChageListener(OnGridChanged);
    }

    private void Update()
    {
        if(changed)
        {
            changed = false;
            Generate();
        }
    }

    private void OnDestroy()
    {
        grid.UnregisterOnChageListener(OnGridChanged);
    }

    private void OnGridChanged()
    {
        changed = true;
    }

    private void Generate()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        Mesh mesh = new()
        {
            name = "buildability mesh",
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        List<int> indices = new(6 * grid.size * grid.size);
        List<Vector3> positions = new(4*grid.size*grid.size);
        List<Color> colors = new(4 * grid.size * grid.size);

        for(int x = 0; x < grid.size; x++)
        {
            for (int z = 0; z < grid.size; z++)
            {
                Color color = Color.red;
                if (grid.CanPlace(x, z))
                    color = Color.green;
                for (int i = 0; i < 4; i++)
                    colors.Add(color);

                positions.Add(ProjectCell(x, z));
                positions.Add(ProjectCell(x + 1, z));
                positions.Add(ProjectCell(x + 1, z + 1));
                positions.Add(ProjectCell(x, z + 1));

                int count = positions.Count;
                indices.Add(count - 4);
                indices.Add(count - 2);
                indices.Add(count - 3);

                indices.Add(count - 2);
                indices.Add(count - 4);
                indices.Add(count - 1);
            }
        }
        mesh.vertices = positions.ToArray();
        mesh.colors = colors.ToArray();
        mesh.triangles = indices.ToArray();
        filter.mesh = mesh;
    }

    private Vector3 ProjectCell(int cellX, int cellZ)
    {
        float x = cellX * grid.cellSize;
        float z = cellZ * grid.cellSize;
        return new Vector3(x, SampleHeight(x, z), z);
    }

    private float SampleHeight(float x, float z)
    {
        float epsilon = 0.01f;
        if (Physics.Raycast(new Vector3(x, grid.maxHeight, z), Vector3.down, out RaycastHit hit, grid.maxHeight, terrainMask))
            return hit.point.y + epsilon;
        return grid.transform.position.y + epsilon;
    }
}
