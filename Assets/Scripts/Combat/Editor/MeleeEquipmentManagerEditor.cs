using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
[CustomEditor(typeof(MeleeEquipmentManager), true)]
public class MeleeEquipmentManagerEditor : Editor{
    GUISkin skin;
    Transform rightHand, leftHand, rightArm, leftArm;
    GameObject handler;
    Animator animator;
    MeleeEquipmentManager meleeEquip;

    [MenuItem("3rd Person Controller/Component/Melee Equip Manager")]
    static void MenuComponent(){
        Selection.activeGameObject.AddComponent<MeleeEquipmentManager>();
    }

    void OnEnable(){
        meleeEquip = (MeleeEquipmentManager)target;
        animator = meleeEquip.gameObject.GetComponent<Animator>();
        if(animator)
        {
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            rightArm = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            leftArm = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        }      
    }

    public override void OnInspectorGUI(){
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        MeleeEquipmentManager meleeEquip = (MeleeEquipmentManager)target;        

        GUILayout.BeginVertical("Melee Manager", "window");

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (meleeEquip.hitProperts != null && meleeEquip.hitProperts.useRecoil && meleeEquip.hitProperts.hitRecoilLayer == 0)
            EditorGUILayout.HelpBox("Please assign the HitRecoilLayer to Default", MessageType.Warning);

        EditorGUILayout.BeginVertical();
        if(!animator){
            EditorGUILayout.HelpBox("This just work with Animator Component", MessageType.Info);
            GUILayout.EndVertical();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            return;
        }

        if (meleeEquip.hitProperts != null && meleeEquip.hitProperts.hitDamageTags != null){
            if (meleeEquip.hitProperts.hitDamageTags.Contains(meleeEquip.gameObject.tag))
                EditorGUILayout.HelpBox("Please change your HitDamageTags inside the HitProperties, they cannot have the same tag as this gameObject.", MessageType.Error);
        }

        base.OnInspectorGUI();        
        
        if (animator != null)
        {
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+"))
                AddHandler(rightArm);

            if (GUILayout.Button("Right Arm"))
                Selection.activeTransform = rightArm;
            if (GUILayout.Button("Left Arm"))
                Selection.activeTransform = leftArm;

            if (GUILayout.Button("+"))            
                AddHandler(leftArm);            

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+"))
                AddHandler(rightHand);

            if (GUILayout.Button("Right Hand"))
                Selection.activeTransform = rightHand;
            if (GUILayout.Button("Left Hand"))
                Selection.activeTransform = leftHand;
            
            if (GUILayout.Button("+"))            
                AddHandler(leftHand);            

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();           
        }

        EditorGUILayout.HelpBox("You can create weapon handler (empty gameobject) for each weapon, them assign the handler to the corresponding weapon.", MessageType.Info);
        GUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUI.changed)
            EditorUtility.SetDirty(meleeEquip);
    }

    void AddHandler(Transform bone){
        handler = new GameObject("handler@weaponName");
        handler.transform.parent = bone;
        handler.transform.localPosition = Vector3.zero;
        if (meleeEquip.weaponHandlers == null) meleeEquip.weaponHandlers = new System.Collections.Generic.List<Transform>();        
        meleeEquip.weaponHandlers.Add(handler.transform);
        Selection.activeTransform = handler.transform;
    }
}