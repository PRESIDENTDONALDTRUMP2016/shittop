using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(CollectableMelee), true)]
public class CollectableMeleeEditor : Editor{
    GUISkin skin;

    public override void OnInspectorGUI(){
        if (!skin)
            skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        CollectableMelee collectableWeapon = (CollectableMelee)target;       

        GUILayout.BeginVertical("Collectable Weapon", "window");
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (collectableWeapon.gameObject.layer == 0)
            EditorGUILayout.HelpBox("Please assign the Layer to Triggers", MessageType.Warning);

        EditorGUILayout.BeginVertical();

        base.OnInspectorGUI();

        //EditorGUILayout.HelpBox("You can create weapon handles (empty gameobject) for each weapon inside the RightHand or LeftForeArm bones.", MessageType.Info);
        GUILayout.EndVertical();
        EditorGUILayout.EndVertical();
    }
}