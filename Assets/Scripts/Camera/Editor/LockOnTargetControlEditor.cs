using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LockOnTargetControl),true)]
public class LockOnTargetControlEditor : Editor
{
    GUISkin skin;

    public override void OnInspectorGUI()
    {
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        LockOnTargetControl lockon = (LockOnTargetControl)target;

        GUILayout.BeginVertical("Lock-on", "window");

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (lockon.layerOfObstacles == 0)
        {
            EditorGUILayout.HelpBox("Please assign the Layer of Obstacles to 'Default' ", MessageType.Warning);
        }

        EditorGUILayout.BeginVertical();       
        base.OnInspectorGUI();        
        GUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
    }
}