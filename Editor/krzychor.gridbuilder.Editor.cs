using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridGeneratorEditor : EditorWindow
{
    public float cellSize = 1;
    public float epsilon = 0.01f;
    public float scannerHeight = 5;
    public Building building;
    bool dynamicGridSize = true;
    Vector2Int demandedGridSize;

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

        if(IsScanSceneReady())
        {
            cellSize = EditorGUILayout.FloatField("cell size", cellSize);
            epsilon = EditorGUILayout.FloatField("epsilon", epsilon);
            scannerHeight = EditorGUILayout.FloatField("scanner height", scannerHeight);
            building = (Building)EditorGUILayout.ObjectField("building", building, typeof(Building), true);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("dynamic grids size");
            bool newDynamicGridSize = EditorGUILayout.Toggle(dynamicGridSize);
            EditorGUILayout.EndHorizontal();

            if (newDynamicGridSize != dynamicGridSize)
            {
                demandedGridSize = building.grid.gridSize;
                dynamicGridSize = newDynamicGridSize;
            }

            if (dynamicGridSize == false)
            {
                demandedGridSize = EditorGUILayout.Vector2IntField("grid size", demandedGridSize);
            }

            if (oldBuilding != building)
                CreateNewModel();

            if (building != null)
            {
                if (GUILayout.Button("Generate"))
                    Generate();
            }
            
        }
        else
        {
            if(CanOpenScanScene())
            {
                if (GUILayout.Button("Open"))
                {
                    CreateScene();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Save open scenes");
            }
        }
        

    }

    private void CreateNewModel()
    {
        if (G != null)
            DestroyImmediate(G);

        if(building != null)
        {
            gridInstance = new BuildingGridInstance(building.grid);
            G = PrefabUtility.InstantiatePrefab(building.model) as GameObject;
            G.transform.position = Vector3.zero;

            Vector3 pos = Vector3.zero;
            if (building.grid.boundsList.Count > 0)
            {
                pos = building.grid.boundsList[gridInstance.rotation].center;
                pos.y = 0;
            }
            G.transform.position = -pos;
            G.transform.rotation = gridInstance.GetRotation();

            Selection.activeGameObject = G;
            SceneView.FrameLastActiveSceneView();
        }


        GenerateTiles();
    }

    private void RefreshModel()
    {
        if (G != null)
            DestroyImmediate(G);

        if (building != null)
        {
            G = PrefabUtility.InstantiatePrefab(building.model) as GameObject;
            Vector3 pos = building.grid.boundsList[gridInstance.rotation].center;
            pos.y = 0;
            G.transform.position = -pos;
            G.transform.rotation = gridInstance.GetRotation();
        }

        GenerateTiles();
    }

    List<Bounds> CreateBoundsList()
    {
        List<Bounds> boundsList = new(4);

        gridInstance.SetRotation(0);
        for (int i = 0; i < 4; i++)
        {
            GameObject gameobject = Instantiate(building.model, new Vector3(), gridInstance.GetRotation());
            boundsList.Add(CalculateBounds(gameobject));
            DestroyImmediate(gameobject);
            gridInstance.RotateRight();
        }
        return boundsList;
    }

    private bool CanOpenScanScene()
    {
        for(int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
            if(EditorSceneManager.GetSceneAt(i).isDirty == true)
                return false;
        return true;
    }

    private bool IsScanSceneReady()
    {
        return EditorSceneManager.loadedSceneCount == 1 && EditorSceneManager.GetSceneAt(0).name == "Scanner";
    }

    private void CreateScene()
    {
        var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        newScene.name = "Scanner";

        CreateFloorObject();
        CreateNewModel();
    }

    private void CreateFloorObject()
    {
        floor = new GameObject("floor");
        MeshCollider collider = floor.AddComponent<MeshCollider>();
        MeshFilter filter = floor.AddComponent<MeshFilter>();
        MeshRenderer renderer = floor.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        floor.hideFlags = HideFlags.HideAndDontSave;
    }

    private void Generate()
    {
        CreateNewModel();
        G.transform.rotation = Quaternion.identity;
        Bounds bounds = CalculateBounds(G);

        max = GetSize(bounds);
        if (dynamicGridSize == false)
            max = demandedGridSize;

        BuildingGridTemplate bg = new BuildingGridTemplate(max, CreateBoundsList());
        gridInstance = new BuildingGridInstance(bg);

        floor.GetComponentInChildren<Collider>().enabled = false;
        for (int x = 0; x < max.x; x++)
            for (int y = 0; y < max.y; y++)
            {
                Vector3 pos = new Vector3(x * cellSize, 0, y * cellSize);
                Vector3 center = bounds.min + new Vector3(cellSize, cellSize, cellSize) / 2 + pos;
                center.y = scannerHeight / 2.0f;
                Vector3 ext = new(cellSize - epsilon, scannerHeight/2.0f, cellSize - epsilon);
                bg.Set(x, y, Physics.CheckBox(center, ext));
            }

        floor.GetComponentInChildren<Collider>().enabled = true;
        floor.transform.position = new Vector3(-max.x/2.0f, 0, -max.y/2.0f);
        building.grid = bg;
        EditorUtility.SetDirty(building);
        AssetDatabase.SaveAssets();
        GenerateTiles();
    }

    private void Update()
    {
        if (floor == null)
            CreateFloorObject();
        if (gridInstance == null && building != null)
            gridInstance = new BuildingGridInstance(building.grid);
        
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

    private Bounds CalculateBounds(GameObject G)
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

        if (IsScanSceneReady() == false)
            return;

        if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
        {
            gridInstance.RotateRight();
            RefreshModel();
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
        bool isValid = true;

        if (building == null || building.grid.boundsList == null || building.grid.boundsList.Count == 0)
            isValid = false;

        if (isValid == false)
        {
            floor.GetComponent<MeshFilter>().sharedMesh = null;
            floor.GetComponent<MeshCollider>().sharedMesh = null;
            return;
        }

        var mesh = new Mesh
        {
            name = "Procedural Mesh"
        };

        List<Color> colors = new();
        List<Vector3> verts = new();
        List<int> inds = new();

        Vector2Int min = gridInstance.Min();
        Vector2Int max = gridInstance.Max(); 
        floor.transform.position = new Vector3(-gridInstance.GetSize().x / 2.0f, 0, -gridInstance.GetSize().y / 2.0f);
        //    floor.transform.position = building.grid.boundsList[gridInstance.rotation].min;
        //   floor.transform.position = new Vector3(building.grid.gridSize.x / 2.0f, 0, building.grid.gridSize.y / 2.0f);

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
                verts.Add(new Vector3(xMin,            0, zMin));
                verts.Add(new Vector3(xMin + cellSize, 0, zMin));
                verts.Add(new Vector3(xMin + cellSize, 0, zMin + cellSize));
                verts.Add(new Vector3(xMin,            0, zMin + cellSize));
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
