using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

#if ENABLE_INPUT_SYSTEM
#endif

[RequireComponent(typeof(GridDisplayer))]
public class GridBuilder : MonoBehaviour
{
    public GridData grid { get; private set; }
    [SerializeField]
    LayerMask buildingMask;
    [SerializeField]
    new Camera camera;

    public Action<GameObject, Building> onBuildingPlaced;
    GridDisplayer gridDisplayer;
    GridAction currentAction;


    public void SetGrid(GridData grid)
    {
        this.grid = grid;
        gridDisplayer.SetGrid(grid);
    }

    public bool IsDuringAction()
    {
        return currentAction != null;
    }

    public void CancelAction()
    {
        currentAction?.Cancel();
        currentAction = null;
    }

    public void StartMassPlacing(Building building)
    {
        CancelAction();

        SelectionPlacementAction newAction = new SelectionPlacementAction(this, building);
        currentAction = newAction;
        newAction.OnStart();
    }

    public void StartPlacing(Building building)
    {
        CancelAction();

        PlaceAction newAction = new PlaceAction(building, this);
        currentAction = newAction;
        newAction.OnStart();
    }

    public void StartDestroyAction()
    {
        CancelAction();

        DestroyAction newAction = new DestroyAction(this);
        currentAction = newAction;
        newAction.OnStart();
    }

    public Bounds CalculateBounds(GameObject G)
    {
        Collider coll = G.GetComponentInChildren<Collider>(true/*include inactive*/);
        return coll.bounds;

        Bounds bounds = new Bounds(transform.position, Vector3.one);
        Renderer[] renderers = G.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    public bool IsOverUI()
    {
        if(EventSystem.current != null)
            return EventSystem.current.IsPointerOverGameObject();
        return false;
    }

    public bool RaycastMouse(out Vector3 pos)
    {
        pos = default;
        if (IsOverUI())
            return false;

        Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        float rayLenght = 1000.0f;
        if (Physics.Raycast(ray, out RaycastHit hit, rayLenght, buildingMask))
        {
            pos = hit.point;
            return true;
        }

        return false;
    }

    public GameObject RaycastMouse()
    {
        if (IsOverUI())
            return null;

        Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        float rayLenght = 1000.0f;

        if (Physics.Raycast(ray, out RaycastHit hit, rayLenght))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    private void Awake()
    {
        if(camera == null)
            camera = Camera.main;

        gridDisplayer = gameObject.GetComponent<GridDisplayer>();
    }

    private void Update()
    {
        currentAction?.Update();

        if (Keyboard.current[Key.Escape].wasPressedThisFrame == true)
        {
            if (IsDuringAction())
                CancelAction();
        }
    }

}
