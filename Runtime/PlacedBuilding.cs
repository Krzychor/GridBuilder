using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedBuilding : MonoBehaviour
{
    public GridData grid;
    public Vector3Int cell;
    public int rotation;
    public Building building;


    private void OnDestroy()
    {
        if(grid != null)
            grid.Unregister(this);
    }
}
