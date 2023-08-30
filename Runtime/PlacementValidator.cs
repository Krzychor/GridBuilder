using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public abstract class PlacementValidator : ScriptableObject
{
    public abstract void Validate(ref List<Vector3Int> cells, GridData grid, Building building);
}
