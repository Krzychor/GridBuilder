using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public abstract class PlacementValidator : ScriptableObject
{
    public abstract void Validate(ref List<Vector3Int> cells, GridData grid, Building building);

    public virtual bool CanStartPlacing( GridData grid, Building building)
    {
        return true;
    }

}
