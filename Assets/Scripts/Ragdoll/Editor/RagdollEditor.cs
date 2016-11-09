using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(Invector.CharacterController.Ragdoll),true)]
public class RagdollEditor : Editor
{
    GUISkin skin;

    public override void OnInspectorGUI()
    {
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        GUILayout.BeginVertical("window");
        EditorGUILayout.BeginVertical();       
        base.OnInspectorGUI();

        EditorGUILayout.HelpBox("Every gameobject children of the character must have their tag added in the IgnoreTag List.", MessageType.Info);
        GUILayout.EndVertical();
        EditorGUILayout.EndVertical();
    }
}