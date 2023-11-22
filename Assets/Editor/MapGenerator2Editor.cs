using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(MapGenerator2))]
public class MapGenerator2Editor : Editor
{ 
    public override void OnInspectorGUI()
    {
        //base.OnInspectorGUI();
        MapGenerator2 mapGen = (MapGenerator2)target;

        if (DrawDefaultInspector())
        {
            if (mapGen.autoUpdate)
            {
                mapGen.DrawSingleChunkInEditor();
            }
        }


        if (GUILayout.Button("Update"))
        {
            mapGen.DrawSingleChunkInEditor();
        }
        //if (texture != null)
        //{ 
        //    GUILayout.Label("", GUILayout.Height(80), GUILayout.Width(80));
        //    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), texture);
        //}
    }
}
