using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridBuilder))]
public class GridBuilderStarter : MonoBehaviour
{
    [SerializeField]
    GridData grid;

    void Start()
    {
        GetComponent<GridBuilder>().SetGrid(grid);
    }

}
