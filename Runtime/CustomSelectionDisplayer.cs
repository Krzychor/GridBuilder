using System.Collections;
using System.Collections.Generic;
using UnityEngine;





public abstract class CustomSelectionDisplayer : MonoBehaviour
{

    public abstract void Display(List<Vector3Int> cells, GridData grid);
}
