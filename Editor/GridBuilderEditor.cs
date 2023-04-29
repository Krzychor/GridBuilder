using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GridBuilder))]
public class GridBuilderEditor : Editor
{

    public override void OnInspectorGUI()
    {
        GridBuilder builder = target as GridBuilder;
        base.DrawDefaultInspector();

        DisplayCurrentAction(builder);
    }

    private void DisplayCurrentAction(GridBuilder builder)
    {
        GridAction action = builder.GetAction();
        if (action == null)
        {
            GUILayout.Label("no action");
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label("current action: ");
        if (action is PlaceAction)
            GUILayout.Label("place");
        else if (action is DestroyAction)
            GUILayout.Label("destroy");
        else if (action is SelectionPlacementAction)
            GUILayout.Label("mass placing");
        else
            GUILayout.Label("unknow");

        GUILayout.EndHorizontal();
    }
}
