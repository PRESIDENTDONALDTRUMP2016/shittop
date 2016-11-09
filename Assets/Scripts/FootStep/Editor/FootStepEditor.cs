using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEditor.Callbacks;
[CanEditMultipleObjects]
[CustomEditor(typeof(FootStepFromTexture),true)]
public class FootStepEditor : Editor{
    GUISkin skin;
    SerializedObject footStep;

    void OnEnable(){
        footStep = new SerializedObject(target);
    }

    public override void OnInspectorGUI(){
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;
        
        if (footStep == null) return;
		CheckColliders();
		EditorGUILayout.Space();
		GUILayout.BeginVertical ("FootStep System by Invector", "window");
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
                
		EditorGUILayout.Separator ();
		footStep.FindProperty ("debugTextureName").boolValue = EditorGUILayout.Toggle("Debug Texture Name", footStep.FindProperty("debugTextureName").boolValue);
		EditorGUILayout.Separator ();		
		GUILayout.BeginHorizontal ("box");
		EditorGUILayout.PropertyField (footStep.FindProperty ("leftFootTrigger"),new GUIContent("",null,"leftFootTrigger"));
		EditorGUILayout.Separator ();
		EditorGUILayout.PropertyField (footStep.FindProperty ("rightFootTrigger"),new GUIContent("",null,"rightFootTrigger"));
		GUILayout.EndHorizontal ();        
        GUILayout.BeginVertical("Default FootStep Audio","window");
        GUILayout.Space(40);       
        EditorGUILayout.PropertyField(footStep.FindProperty("defaultSurface"));     
        EditorGUILayout.HelpBox("This audio will play on any terrain or texture as the primary footstep.", MessageType.Info);
        GUILayout.EndVertical();

        GUILayout.BeginVertical("CustomSurfaces","window");
        GUILayout.Space(20);
        DrawMultipleSurface(footStep.FindProperty("customSurfaces"));
        EditorGUILayout.HelpBox("Create new CustomSurfaces on the 3rd Person Controller menu > Resources > New AudioSurface", MessageType.Info);
        GUILayout.EndVertical();

        GUILayout.EndVertical();
        if (GUI.changed)
        {
            footStep.ApplyModifiedProperties();
        }
		EditorGUILayout.Space();
    }
    
    [MenuItem("3rd Person Controller/Component/FootStep")]
    static void MenuComponent()
    {
        Selection.activeGameObject.AddComponent<FootStepFromTexture>();
    }

    [MenuItem("3rd Person Controller/Resources/New AudioSurface")]
    static void NewAudioSurface()
    {
        ScriptableObjectUtility.CreateAsset<AudioSurface>();
    }
    void CheckColliders()
    {
        var _footStep = (FootStepFromTexture)target;
        var animator = _footStep.transform.GetComponent<Animator>();
        var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var leftFoot_trigger = leftFoot.GetComponentInChildren<FootStepTrigger>();
        if (leftFoot_trigger == null)
        {
            var lFoot = new GameObject("leftFoot_trigger");
            var collider = lFoot.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            leftFoot_trigger = lFoot.AddComponent<FootStepTrigger>();
            leftFoot_trigger.transform.position = new Vector3(leftFoot.position.x,_footStep.transform.position.y,leftFoot.position.z);
            leftFoot_trigger.transform.rotation = _footStep.transform.rotation;
            leftFoot_trigger.gameObject.layer = _footStep.gameObject.layer;
            leftFoot_trigger.transform.parent = leftFoot;
        }
        if (leftFoot_trigger.GetComponent<Collider>() == null)
        {
           var collider = leftFoot_trigger.gameObject.AddComponent<SphereCollider>();
           collider.radius = 0.1f;
        }            

        var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        var rightFoot_trigger = rightFoot.GetComponentInChildren<FootStepTrigger>();
        if (rightFoot_trigger == null)
        {
            var rFoot = new GameObject("rightFoot_trigger");
            var collider = rFoot.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            rightFoot_trigger = rFoot.gameObject.AddComponent<FootStepTrigger>();
            rightFoot_trigger.transform.position = new Vector3(rightFoot.position.x, _footStep.transform.position.y, rightFoot.position.z);
            rightFoot_trigger.transform.rotation = _footStep.transform.rotation;
            rightFoot_trigger.gameObject.layer = _footStep.gameObject.layer;
            rightFoot_trigger.transform.parent = rightFoot;
        }
        if (rightFoot_trigger.GetComponent<Collider>() == null)
        {
          var collider = rightFoot_trigger.gameObject.AddComponent<SphereCollider>();
          collider.radius = 0.1f;
        }
        _footStep.leftFootTrigger = leftFoot_trigger;
        _footStep.rightFootTrigger = rightFoot_trigger;

    }
 
    void DrawSingleSurface(SerializedProperty surface,bool showListNames)
    {
        //GUILayout.BeginVertical("window");
        EditorGUILayout.PropertyField(surface.FindPropertyRelative("source"), false);
		EditorGUILayout.PropertyField(surface.FindPropertyRelative("name"), new GUIContent("Surface Name"), false);

        if (showListNames)
            DrawSimpleList(surface.FindPropertyRelative("TextureOrMaterialNames"),false);

        DrawSimpleList(surface.FindPropertyRelative("audioClips"),true);
        //GUILayout.EndVertical();
    }

    void DrawMultipleSurface(SerializedProperty surfaceList)
    {
        //GUILayout.BeginVertical();
        EditorGUILayout.PropertyField(surfaceList,new GUIContent("Surfaces"));
        if(surfaceList.isExpanded)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                surfaceList.arraySize++;
            }
            if (GUILayout.Button("Clear"))
            {
                surfaceList.arraySize = 0;
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            for (int i = 0; i < surfaceList.arraySize; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginHorizontal("box");
                EditorGUILayout.Space();
                if (i < surfaceList.arraySize && i >= 0)
                {
                    GUILayout.BeginVertical();
                    EditorGUILayout.PropertyField(surfaceList.GetArrayElementAtIndex(i),
                        new GUIContent(surfaceList.GetArrayElementAtIndex(i).objectReferenceValue != null ? surfaceList.GetArrayElementAtIndex(i).objectReferenceValue.name : "Surface " + (i + 1).ToString("00")));                   
                    EditorGUILayout.Space();
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();

                if (GUILayout.Button("-"))
                {
                    surfaceList.DeleteArrayElementAtIndex(i);
                }
                GUILayout.EndHorizontal();
            }
            //GUILayout.EndVertical();
        }           
    }

    void DrawTextureNames(SerializedProperty textureNames)
    {
        for (int i = 0; i < textureNames.arraySize; i++)        
            EditorGUILayout.PropertyField(textureNames.GetArrayElementAtIndex(i), true);      
    }

    void DrawSimpleList(SerializedProperty list,bool useDraBox)
    {
        EditorGUILayout.PropertyField(list);

        if (list.isExpanded)
        {
            if (useDraBox)
                DrawDragBox(list);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                list.arraySize++;
            }
            if (GUILayout.Button("Clear"))
            {
                list.arraySize=0;
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
            
            for (int i = 0; i < list.arraySize; i++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-"))
                {
                    RemoveElementAtIndex(list, i);
                }

                if (i < list.arraySize && i >= 0)
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent("", null, ""));
                
                GUILayout.EndHorizontal();
            }
        }       
    }

    private void RemoveElementAtIndex(SerializedProperty array, int index)
    {
        if (index != array.arraySize - 1)
        {
            array.GetArrayElementAtIndex(index).objectReferenceValue = array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue;
        }
        array.arraySize--;
    }

    void DrawDragBox(SerializedProperty list)
    {
        //var dragAreaGroup = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
        GUI.skin.box.alignment = TextAnchor.MiddleCenter;
        GUI.skin.box.normal.textColor = Color.white;                
        GUILayout.Box("Drag your audio clips here!", "box", GUILayout.MinHeight(50), GUILayout.ExpandWidth(true));
        var dragAreaGroup = GUILayoutUtility.GetLastRect();

        switch (Event.current.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dragAreaGroup.Contains(Event.current.mousePosition))
                    break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var dragged in DragAndDrop.objectReferences)
                    {
                        var clip = dragged as AudioClip;                       
                        if (clip == null)
                            continue;
                        list.arraySize++;
                        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = clip;                                             
                    }
                }
			footStep.ApplyModifiedProperties();
                Event.current.Use();
                break;
        }       
    }
}
[CustomEditor(typeof(AudioSurface),true)]
public class AudioSurfaceEditor:Editor
{
    GUISkin skin;
    SerializedObject footStep;   
    public override void OnInspectorGUI()
    {
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;
        footStep = new SerializedObject(target);
        if(footStep!=null)

        //GUILayout.BeginVertical("AudioSurface","window");
        //GUILayout.Space(40);        
        DrawSingleSurface(footStep, true);        
        GUILayout.BeginVertical("Optional Parameter", "window");
        GUILayout.Space(40);
        EditorGUILayout.PropertyField(footStep.FindProperty("audioMixerGroup"), false);
        EditorGUILayout.PropertyField(footStep.FindProperty("particleObject"), false);

        GUILayout.EndVertical();
        //GUILayout.EndVertical();        
       
        if (GUI.changed)
        {
            footStep.ApplyModifiedProperties();
        }
    }   

    void DrawSingleSurface(SerializedObject surface, bool showListNames)
    {
        if (showListNames)
            DrawSimpleList(surface.FindProperty("TextureOrMaterialNames"), false);
        DrawSimpleList(surface.FindProperty("audioClips"), true);      
    }

    void DrawSimpleList(SerializedProperty list, bool useDraBox)
    {
        var name = list.name;
        GUILayout.BeginVertical(name, "window");
        GUILayout.Space(30);
        switch (list.name)
        {
            case "TextureOrMaterialNames":
                name = "Texture  or  Material  names";               
                EditorGUILayout.HelpBox("Leave this field empty and assign to the defaultSurface to play on any surface or type a Material name and assign to a customSurface to play only when the sphere hit a mesh using it.", MessageType.Info);
                break;
            case "audioClips":
                EditorGUILayout.HelpBox("You can lock the inspector to drag and drop multiple audio files.", MessageType.Info);
                name = "Audio  Clips";                
                break;
               
        }
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();       
        EditorGUILayout.PropertyField(list, false);        
        //GUILayout.Box(list.arraySize.ToString("00"));       
        GUILayout.EndHorizontal();
       
        if (list.isExpanded)
        {
            if (useDraBox)
                DrawDragBox(list);
            EditorGUILayout.Separator();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                list.arraySize++;
            }
            if (GUILayout.Button("Clear"))
            {
                list.arraySize = 0;
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            for (int i = 0; i < list.arraySize; i++)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-"))
                {
                    RemoveElementAtIndex(list, i);
                }

                if (i < list.arraySize && i >= 0)
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent("", null, ""));

                GUILayout.EndHorizontal();
            }
        }
        GUILayout.EndVertical();
        GUILayout.EndVertical(); 
    }

    private void RemoveElementAtIndex(SerializedProperty array, int index)
    {
        if (index != array.arraySize - 1)
        {
            array.GetArrayElementAtIndex(index).objectReferenceValue = array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue;
        }
        array.arraySize--;
    }

    void DrawDragBox(SerializedProperty list)
    {
        //var dragAreaGroup = GUILayoutUtility.GetRect(0f, 35f, GUILayout.ExpandWidth(true));
        GUI.skin.box.alignment = TextAnchor.MiddleCenter;
        GUI.skin.box.normal.textColor = Color.white;
        //GUILayout.BeginVertical("window");
        GUILayout.Box("Drag your audio clips here!", "box", GUILayout.MinHeight(50), GUILayout.ExpandWidth(true));
        var dragAreaGroup = GUILayoutUtility.GetLastRect();
        //GUILayout.EndVertical();
        switch (Event.current.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dragAreaGroup.Contains(Event.current.mousePosition))
                    break;
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var dragged in DragAndDrop.objectReferences)
                    {
                        var clip = dragged as AudioClip;
                        if (clip == null)
                            continue;
                        list.arraySize++;
                        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = clip;
                    }
                }
                footStep.ApplyModifiedProperties();
                Event.current.Use();
                break;
        }
    }
}