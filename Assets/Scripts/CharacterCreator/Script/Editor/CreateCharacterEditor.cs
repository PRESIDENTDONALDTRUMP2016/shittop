using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using Invector;
using Invector.CharacterController;
using UnityEngine.EventSystems;

public class CreateCharacterEditor : EditorWindow {
    GUISkin skin;
    GameObject charObj;
    Animator charAnimator;
    RuntimeAnimatorController controller;
    TPCameraListData cameraListData;
    Vector2 rect = new Vector2(500, 570);
    Vector2 scrool;
    Editor humanoidpreview;
    List<CharacterTemplate> templates;
    List<string> templateNames;
    int selectedTemplate;
    CharacterTemplate currentTemplate;
    Rect buttomRect = new Rect();

    public enum CharacterType {
        CharacterController, CharacterAI
    }

    public CharacterType charType = CharacterType.CharacterController;

    /// CharacterTemplate Menu 
    [MenuItem("3rd Person Controller/Resources/New Character Template")]
    static void NewCameraStateData(){
        ScriptableObjectUtility.CreateAsset<CharacterTemplate>();
    }

	/// 3rdPersonController Menu   
    [MenuItem("3rd Person Controller/Create New Character", false, 0)]
    public static void CreateNewCharacter(){
        GetWindow<CreateCharacterEditor>();
    }

    void OnEnable(){
        templates = new List<CharacterTemplate>();
        LoadAllAssets(Application.dataPath, ref templates);
    }

    bool isHuman, isValidAvatar, charExist;

    void OnGUI(){
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        this.minSize = rect;
        this.titleContent = new GUIContent("Character", null, "3rd Person Character Creator");
        if (templateNames == null) templateNames = new List<string>();
        if (templates == null) return;
        foreach (CharacterTemplate template in templates) {
            if (!templateNames.Contains(template.name))
                templateNames.Add(template.name);
        }

        GUILayout.BeginVertical("Character Creator Window", "window");
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        GUILayout.BeginVertical("box");
        charType = (CharacterType)EditorGUILayout.EnumPopup("Character Type", charType);        

        if (charType == CharacterType.CharacterController) {
            if (templates.Count > 0) {                
                selectedTemplate = EditorGUILayout.Popup("Character Template", selectedTemplate, templateNames.ToArray());                
                currentTemplate = templates[selectedTemplate];
            }
            else            
                EditorGUILayout.HelpBox("Can't find a Character Template\nPlease create Template in\n3rd Person Controller>Resources>New Character Template ", MessageType.Error);            

            var currentEvent = Event.current;

            buttomRect = GUILayoutUtility.GetLastRect();
            buttomRect.position = new Vector2(0, buttomRect.position.y);
            buttomRect.width = this.maxSize.x;
            if (buttomRect.Contains(currentEvent.mousePosition) || templates == null)
            {
                templates = new List<CharacterTemplate>();
                LoadAllAssets(Application.dataPath, ref templates);
            }
        }

        if (!isValidTemplate())
        {
            EditorGUILayout.HelpBox("The Character Template is null or incomplete", MessageType.Error);            
            GUILayout.FlexibleSpace();
            return;
        }

        if (!charObj)
            EditorGUILayout.HelpBox("Make sure your FBX model is select as Humanoid!", MessageType.Info);
        else if (!charExist)        
           EditorGUILayout.HelpBox("Missing a Animator Component", MessageType.Error);                
        else if (!isHuman)
            EditorGUILayout.HelpBox("This is not a Humanoid", MessageType.Error);
        else if (!isValidAvatar)
            EditorGUILayout.HelpBox(charObj.name + " is a invalid Humanoid", MessageType.Info);

        charObj = EditorGUILayout.ObjectField("FBX Model", charObj, typeof(GameObject), true, GUILayout.ExpandWidth(true)) as GameObject;

        if (GUI.changed && charObj != null)
            humanoidpreview = Editor.CreateEditor(charObj);

        GUILayout.EndVertical();

        if (charObj)
            charAnimator = charObj.GetComponent<Animator>();
        charExist = charAnimator != null;
        isHuman = charExist ? charAnimator.isHuman : false;
        isValidAvatar = charExist ? charAnimator.avatar.isValid : false;

        if (CanCreate())
        {
            DrawHumanoidPreview();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create"))
                Create();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    bool CanCreate()
    {
        return isValidTemplate() && isValidAvatar && isHuman;
    }

    void LoadAllAssets(string pathTarget, ref List<CharacterTemplate> _templates)
    {
        if (!Directory.Exists(pathTarget))
            Directory.CreateDirectory(pathTarget);
        DirectoryInfo info = new DirectoryInfo(pathTarget);
        DirectoryInfo[] dirInfos = info.GetDirectories("*", SearchOption.TopDirectoryOnly);
        FileInfo[] fInfos = info.GetFiles("*.asset", SearchOption.TopDirectoryOnly);

        foreach (FileInfo fInfo in fInfos)
        {
            var dir = @fInfo.FullName;
            var path = dir.Remove(0, Application.dataPath.ToString().Length);

            var template = AssetDatabase.LoadAssetAtPath<CharacterTemplate>("Assets" + path);
            if (template != null)
                templates.Add(template);
        }
        foreach (DirectoryInfo dirInfo in dirInfos)
        {
            LoadAllAssets(dirInfo.FullName, ref _templates);
        }
    }

    /// Check if the Template is valid
    bool isValidTemplate()
    {
        if (currentTemplate == null ||
            currentTemplate.animatorController == null ||
            currentTemplate.cameraListData == null ||
            currentTemplate.hud == null) return false;
        return true;
    }

    /// Draw the Preview window
    void DrawHumanoidPreview(){
        GUILayout.FlexibleSpace();

        if (humanoidpreview != null)
            humanoidpreview.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(100, 400), "window");
    }

    /// Created the Third Person Controller
    void Create()
    {
        // base for the character
        var _3rdPerson = GameObject.Instantiate(charObj, Vector3.zero, Quaternion.identity) as GameObject;
        if (!_3rdPerson)
            return;
        SceneView view = SceneView.lastActiveSceneView;
        if (SceneView.lastActiveSceneView == null)
            throw new UnityException("The Scene View can't be access");

        Vector3 spawnPos = view.camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));      
        _3rdPerson.transform.position = spawnPos;
        var rigidbody = _3rdPerson.AddComponent<Rigidbody>();
        var collider = _3rdPerson.AddComponent<CapsuleCollider>();

        if (charType == CharacterType.CharacterController)
        {
            _3rdPerson.name = "3rdPersonController";

            if (currentTemplate.templateType == CharacterTemplate.TemplateType.ThirdPerson)
                _3rdPerson.AddComponent<ThirdPersonController>();
            //else
            //    _3rdPerson.AddComponent<TopDownController>();

            // camera
            GameObject camera = null;
            if (Camera.main == null)
            {
                var cam = new GameObject("3rdPersonCamera");
                cam.AddComponent<Camera>();
                cam.AddComponent<FlareLayer>();
                cam.AddComponent<GUILayer>();
                cam.AddComponent<AudioListener>();
                camera = cam;
                camera.GetComponent<Camera>().tag = "MainCamera";
                camera.GetComponent<Camera>().nearClipPlane = 0.01f;
            }
            else
            {
                camera = Camera.main.gameObject;
                camera.gameObject.name = "3rdPersonCamera";
            }
            var tpcamera = camera.GetComponent<TPCamera>();

            if (tpcamera == null)
                tpcamera = camera.AddComponent<TPCamera>();

            // define the camera cursorObject       
            tpcamera.target = _3rdPerson.transform;

            cameraListData = currentTemplate.cameraListData;
            if (cameraListData != null)
                tpcamera.CameraStateList = cameraListData;

            GameObject gC = null;
            var gameController = FindObjectOfType<vGameController>();
            if (gameController == null)
            {
                gC = new GameObject("GameController");
                gC.AddComponent<vGameController>();
            }

            _3rdPerson.GetComponent<ThirdPersonMotor>().tpCamera = tpcamera;
            _3rdPerson.GetComponent<ThirdPersonMotor>().hud = CreateHud();
            _3rdPerson.tag = "Player";
        }
        else
        {
            _3rdPerson.tag = "Enemy";
            _3rdPerson.name = "EnemyAI";
            _3rdPerson.AddComponent<AIController>();

            var navMesh = _3rdPerson.AddComponent<NavMeshAgent>();
            navMesh.radius = 0.3f;
            navMesh.speed = 0f;
            navMesh.angularSpeed = 300f;
            navMesh.acceleration = 4f;
            navMesh.autoBraking = false;
            navMesh.autoTraverseOffMeshLink = false;
        }

        // rigidBody		
        if (charType == CharacterType.CharacterController)
        {
            rigidbody.useGravity = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            rigidbody.useGravity = false;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePosition;
        }

        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rigidbody.mass = 50;

        // capsule collider 
        collider.height = ColliderHeight(_3rdPerson.GetComponent<Animator>());
        collider.center = new Vector3(0, (float)System.Math.Round(collider.height * 0.5f, 2), 0);
        collider.radius = (float)System.Math.Round(collider.height * 0.15f, 2);

        // animator
        _3rdPerson.GetComponent<Animator>().avatar = _3rdPerson.GetComponent<Animator>().avatar;
        _3rdPerson.GetComponent<Animator>().updateMode = AnimatorUpdateMode.AnimatePhysics;
              
        controller = currentTemplate.animatorController;
        if (controller)
            _3rdPerson.GetComponent<Animator>().runtimeAnimatorController = controller;

        // footStep
        _3rdPerson.AddComponent<FootStepFromTexture>(); 
        this.Close();
    }

    /// <summary>
    /// Capsule Collider height based on the Character height
    /// </summary>
    /// <param name="animator">animator humanoid</param>
    /// <returns></returns>
    float ColliderHeight(Animator animator)
    {
        var foot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        return (float)System.Math.Round(Vector3.Distance(foot.position, hips.position) * 2f, 2);
    }

    /// Return Hud Object
    HUDController CreateHud()
    {
        //check Canvas
        var canvas = FindObjectOfType<Canvas>();
        if (FindObjectOfType<EventSystem>() == null)
        {
            #if UNITY_5_3
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            #else
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule), typeof(TouchInputModule));
            #endif
        }
        if (canvas == null || (canvas!=null && !canvas.tag.Equals("PlayerUI")))
        {
            var canvasObj = new GameObject("UI");
            canvasObj.tag = "PlayerUI";
            canvas = canvasObj.AddComponent<Canvas>();
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (canvas.GetComponent<UnityEngine.UI.CanvasScaler>() != null)
        {
            canvas.GetComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.GetComponent<UnityEngine.UI.CanvasScaler>().screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        }
        //Check HUD
        RectTransform Hud = canvas.transform.FindChild("HUD") as RectTransform;

        if (Hud == null)
        {
            var _Hud = currentTemplate.hud.gameObject;
            _Hud = GameObject.Instantiate(_Hud) as GameObject;
            if (_Hud)
                _Hud.GetComponent<RectTransform>().SetParent(canvas.transform);
            Hud = _Hud.GetComponent<RectTransform>();
            Hud.offsetMax = new Vector2(0, 0);

            Hud.name = "HUD";
        }
        if (Hud.GetComponent<HUDController>() == null)
            Hud.gameObject.AddComponent<HUDController>();
        //HUD Components       

        return Hud.GetComponent<HUDController>();
    }
}
