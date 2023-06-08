using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(GridDisplayer))]
public class GridBuilder : MonoBehaviour
{
    public GridData grid { get; private set; }
    [SerializeField]
    public LayerMask terrainMask;
    [SerializeField]
    public new Camera camera;
    public float raycastDistance = 1000.0f;

    public delegate void OnBuildingPlacedAction(GameObject placed, Vector3 pos, Building building, BuildingGridInstance gridInstance, 
        Vector3Int cell);
    public delegate void OnBuildingDestroyedAction(PlacedBuilding placed);
    public bool applyAction = true;
    public OnBuildingPlacedAction onBuildingPlaced;
    public OnBuildingDestroyedAction onBuildingDestroyed;
    GridDisplayer gridDisplayer;
    GridAction currentAction;
    bool isOverUI = false;

    public GridBuilderInput input;

    public bool IsDuringAction()
    {
        return currentAction != null;
    }

    public GridAction GetAction() { return currentAction; }

    public void SetGrid(GridData grid)
    {
        this.grid = grid;
        gridDisplayer.SetGrid(grid);
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
        Collider coll = G.GetComponentInChildren<Collider>(includeInactive: true);
        return coll.bounds;
    }

    public bool IsOverUI()
    {
        return isOverUI;
    }

    public bool RaycastMouse(out Vector3 pos)
    {
        pos = default;
        if (IsOverUI())
            return false;

        Ray ray = camera.ScreenPointToRay(input.mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, terrainMask))
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

        Ray ray = camera.ScreenPointToRay(input.mousePos);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    private void Awake()
    {
        if (camera == null)
            camera = Camera.main;

        gridDisplayer = gameObject.GetComponent<GridDisplayer>();
    }

    private void Update()
    {
        if (EventSystem.current != null)
            isOverUI = EventSystem.current.IsPointerOverGameObject();
        else
            isOverUI = false;
        currentAction?.Update();
    }

    private void OnDrawGizmos()
    {
        if(camera != null)
        {
            Gizmos.color = Color.red;
            if (RaycastMouse(out Vector3 pos))
                Gizmos.DrawLine(camera.transform.position, pos);
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        CancelAction();
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.started)
            currentAction?.OnClick(pressedDown: true, released: false);

        if (context.canceled)
            currentAction?.OnClick(pressedDown: false, released: true);
    }

    public void OnRotateRight(InputAction.CallbackContext context)
    {
        if (context.performed)
            currentAction?.OnRotateRight();
    }

    public void OnRotateLeft(InputAction.CallbackContext context)
    {
        if (context.performed)
            currentAction?.OnRotateLeft();
    }

    public void OnMousePos(InputAction.CallbackContext context)
    {
        input.mousePos = context.ReadValue<Vector2>();
    }
}
