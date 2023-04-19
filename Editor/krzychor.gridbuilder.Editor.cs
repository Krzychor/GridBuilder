using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridGeneratorEditor : EditorWindow
{
    public float cellSize = 1;
    public float epsilon = 0.01f;
    public Building building;

    public GameObject G;
    public GameObject floor;

    private BuildingGridInstance gridInstance;

    [MenuItem("Window/Building Grid Generator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(GridGeneratorEditor));
    }

    private void OnGUI()
    {
        Building oldBuilding = building;

        cellSize = EditorGUILayout.FloatField("cell size", cellSize);
        epsilon = EditorGUILayout.FloatField("epsilon", epsilon);
        building = (Building)EditorGUILayout.ObjectField("building", building, typeof(Building), true);
        floor = (GameObject)EditorGUILayout.ObjectField("floor", floor, typeof(GameObject), true);
        EditorGUILayout.ObjectField("G", G, typeof(GameObject), true);

        if (G == null && building != null)
        {
            RefreshDisplay();
        }

        if(building != null && building != oldBuilding)
        {
            RefreshDisplay();  
        }

        if (building == null && G != null)
        {
            DestroyImmediate(G);
            G = null;
        }

        if (GUILayout.Button("Generate"))
        {
            Generate();
        }
    }

    private void RefreshDisplay()
    {
        if (G != null)
            DestroyImmediate(G);
        G = Instantiate(building.model, Vector3.zero, Quaternion.identity);

        GenerateTiles();
    }

    List<Bounds> CreateBoundsList()
    {
        List<Bounds> boundsList = new List<Bounds>();
        Quaternion rotation = Quaternion.Euler(0, 0, 0);
        GameObject gameobject = Instantiate(building.model, new Vector3(), rotation);
        boundsList.Add(CalculateBounds());
        DestroyImmediate(gameobject);

        rotation = Quaternion.Euler(0, -90, 0);
        gameobject = Instantiate(building.model, new Vector3(), rotation);
        boundsList.Add(CalculateBounds());
        DestroyImmediate(gameobject);

        rotation = Quaternion.Euler(0, -180, 0);
        gameobject = Instantiate(building.model, new Vector3(), rotation);
        boundsList.Add(CalculateBounds());
        DestroyImmediate(gameobject);

        rotation = Quaternion.Euler(0, -270, 0);
        gameobject = Instantiate(building.model, new Vector3(), rotation);
        boundsList.Add(CalculateBounds());
        DestroyImmediate(gameobject);
        return boundsList;
    }


    private void Generate()
    {
        if (G != null)
            DestroyImmediate(G);
        G = Instantiate(building.model, new Vector3(), Quaternion.Euler(0, 0, 0));
        Collider[] colls = G.GetComponentsInChildren<Collider>(true);
        //      G.transform.rotation = Quaternion.Euler(0, -90 * building.grid.rotation, 0);
        G.transform.rotation = Quaternion.identity;
        Bounds bounds = colls[0].bounds;
        for (int i = 1; i < colls.Length; i++)
            bounds.Encapsulate(colls[i].bounds);

        max = GetSize(bounds);
        Debug.Log("size = " + max);
        BuildingGridTemplate bg = new BuildingGridTemplate(max, CreateBoundsList());

        floor.GetComponentInChildren<Collider>().enabled = false;
        for (int x = 0; x < max.x; x++)
            for (int y = 0; y < max.y; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
                Vector3 center = bounds.min + new Vector3(cellSize, cellSize, cellSize) / 2 + pos;
                Vector3 ext = new(cellSize - epsilon, bounds.size.y, cellSize - epsilon);
                bg.Set1(x, y, Physics.CheckBox(center, ext));
            }

        floor.GetComponentInChildren<Collider>().enabled = true;
        building.grid = bg;
        EditorUtility.SetDirty(building);
        AssetDatabase.SaveAssets();
        GenerateTiles();
    }

    private void Update()
    {
        if (G != null)
        {
            if (Keyboard.current[Key.LeftShift].isPressed)
                G.SetActive(false);
            else
                G.SetActive(true);
        }
    }

    private void OnEnable()
    {
        SceneView.beforeSceneGui += BeforeSceneGui;
    }

    private void OnDisable()
    {
        SceneView.beforeSceneGui -= BeforeSceneGui;
    }

    private Vector2Int GetCoords(Vector3 point)
    {
        Vector2Int res = new((int)(point.x / cellSize), (int)(point.z / cellSize));
        if (point.x < 0)
            res.x--;
        if (point.z < 0)
            res.y--;
        return res;
    }

    private Bounds CalculateBounds()
    {
        Collider[] colls = G.GetComponentsInChildren<Collider>(true);
        Bounds bounds = colls[0].bounds;
        for (int i = 1; i < colls.Length; i++)
            bounds.Encapsulate(colls[i].bounds);
        return bounds;
    }

    private void BeforeSceneGui(SceneView sceneView)
    {
        if (building == null)
            return;

        if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
        {
            gridInstance.RotateRight();
            RefreshDisplay();
            return;
        }

        if (Event.current == null)
            return;
        if (Event.current.shift == false)
            return;
        if (Event.current.type != EventType.MouseDown || Event.current.button != 0)
            return;

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 9999))
        {
            if (hit.collider.gameObject == floor)
            {
                hit.point -= building.grid.boundsList[gridInstance.rotation].min;
                Vector2Int coords = GetCoords(hit.point);
                coords -= gridInstance.GetCenter();

                bool value = gridInstance.Get(coords.x, coords.y);
                gridInstance.Set(coords.x, coords.y, !value);

                EditorUtility.SetDirty(building);
                AssetDatabase.SaveAssets();
                GenerateTiles();
            }
        }
        Event.current.Use();
    }

    private Vector2Int GetSize(Bounds bounds)
    {
        Vector3 span = bounds.max - bounds.min;
        Vector2Int max = new((int)(span.x / cellSize), (int)(span.z / cellSize));
        if (max.x * cellSize < span.x)
            max.x++;
        if (max.y * cellSize < span.z)
            max.y++;
        return max;
    }

    private void GenerateTiles(bool onlyOccupied = false)
    {
        var mesh = new Mesh
        {
            name = "Procedural Mesh"
        };

        List<Color> colors = new();
        List<Vector3> verts = new();
        List<int> inds = new();

        Vector2Int gridSize = gridInstance.GetSize();
        Vector2Int min = gridInstance.Min();
        Vector2Int max = gridInstance.Max();
        floor.transform.position = building.grid.boundsList[gridInstance.rotation].min;
        Vector3 pos = new Vector3();

        for (int x = min.x; x <= max.x; x++)
            for (int z = min.y; z <= max.y; z++)
            {
                Color color = Color.red;
                if (gridInstance.Get(x, z) == false)
                    color = Color.green;
                else if (onlyOccupied)
                    continue;
                float xMin = (x - min.x) * cellSize;
                float zMin = (z - min.y) * cellSize;
                verts.Add(pos + new Vector3(xMin,            0, zMin));
                verts.Add(pos + new Vector3(xMin + cellSize, 0, zMin));
                verts.Add(pos + new Vector3(xMin + cellSize, 0, zMin + cellSize));
                verts.Add(pos + new Vector3(xMin,            0, zMin + cellSize));
                inds.Add(verts.Count - 4);
                inds.Add(verts.Count - 2);
                inds.Add(verts.Count - 3);

                inds.Add(verts.Count - 2);
                inds.Add(verts.Count - 4);
                inds.Add(verts.Count - 1);

                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
                colors.Add(color);
            }

        mesh.vertices = verts.ToArray();
        mesh.triangles = inds.ToArray();
        mesh.colors = colors.ToArray();

        floor.GetComponent<MeshFilter>().sharedMesh = mesh;
        floor.GetComponent<MeshCollider>().sharedMesh = mesh;
    }


    public Vector2Int max;

}
