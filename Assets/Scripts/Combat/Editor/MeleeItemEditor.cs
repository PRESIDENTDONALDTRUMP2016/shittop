using UnityEngine;
using UnityEditor;
using Invector;

[CustomEditor(typeof(MeleeItem),true)]
public class MeleeItemEditor : Editor{  
    GUISkin skin;

    void OnSceneGUI()
    {       
        MeleeItem item = (MeleeItem)target;
        switch(item.GetType().ToString())
        {
            case "MeleeWeapon":
               //TODO
                break;
            case "MeleeShield":
                DrawShieldHandle(item);
                break;
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected )]
    //TODO: Replace first argument with the type you are editing
    static void DrawGizmos(Transform aTarget, GizmoType aGizmoType)
    {
        var weapon = aTarget.GetComponent<MeleeWeapon>();
        if (weapon != null)
            DrawWeaponHandler(weapon);
    }

    void DrawShieldHandle(MeleeItem item)
    {
        var shield = item as MeleeShield;
        if(shield.transform.parent!=null)
        {
            var root = shield.transform.GetComponentInParent<MeleeEquipmentManager>();
            
            if (root == null) return;
            
            var coll = root.GetComponent<Collider>();
            if(coll)
            {               
                Handles.DrawWireDisc(coll.bounds.center, Vector3.up,.5f);
                Handles.color = new Color(1, 0, 0, 0.2f);
                Handles.DrawSolidArc(coll.bounds.center, Vector3.up, shield.transform.root.forward, shield.defenseRange, .5f);
                Handles.DrawSolidArc(coll.bounds.center, Vector3.up, shield.transform.root.forward, -shield.defenseRange, .5f);
                Handles.color = new Color(1, 1, 1, 0.5f);
                Handles.DrawSolidDisc(coll.bounds.center, Vector3.up, .3f);
            }           
        }
    }

    static void DrawWeaponHandler(MeleeWeapon weapon)
    {        
        try
        {
            var parent = weapon.transform.parent;
            if(parent != null)
            {
                weapon.gameObject.tag = parent.tag;
                if (weapon.top != null) weapon.top.gameObject.tag = parent.tag;
                if (weapon.center != null) weapon.center.gameObject.tag = parent.tag;
                if (weapon.bottom != null) weapon.bottom.gameObject.tag = parent.tag;
            }

            var curCenterSize= 0f;
            //gameObject.name = "hitBar";
            if ((Mathf.Abs(weapon.centerPos) + weapon.centerSize) > 2.9f)
                curCenterSize = weapon.centerSize - (Mathf.Abs(weapon.centerPos * 2f));
            else
                curCenterSize = weapon.centerSize;

            var boxSize = weapon.top.BoxSize();
            Gizmos.color = new Color(0, 1, 0, .5f);
            var resultSize = new Vector3(boxSize.x, boxSize.y, boxSize.z);
            var resultPosition = weapon.top.GetBoxPoint().center;
            var matrix = Matrix4x4.TRS(resultPosition, weapon.top.transform.rotation, resultSize);
            Gizmos.matrix = matrix;
            Gizmos.DrawCube(Vector3.zero, new Vector3(1, 1, 1));

            boxSize = weapon.center.BoxSize();
            Gizmos.color = new Color(1, 1, 0, 0.5f);
            resultSize = new Vector3(boxSize.x, boxSize.y, boxSize.z);
            resultPosition = weapon.center.GetBoxPoint().center;
            matrix = Matrix4x4.TRS(resultPosition, weapon.center.transform.rotation, resultSize);
            Gizmos.matrix = matrix;
            Gizmos.DrawCube(Vector3.zero, new Vector3(1, 1, 1));

            boxSize = weapon.bottom.BoxSize();
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            resultSize = new Vector3(boxSize.x, boxSize.y, boxSize.z);
            resultPosition = weapon.bottom.GetBoxPoint().center;
            matrix = Matrix4x4.TRS(resultPosition, weapon.bottom.transform.rotation, resultSize);
            Gizmos.matrix = matrix;
            Gizmos.DrawCube(Vector3.zero, new Vector3(1, 1, 1));

            weapon.top.gameObject.hideFlags = weapon.showHitboxes ? HideFlags.None : HideFlags.HideInHierarchy;
            weapon.center.gameObject.hideFlags = weapon.showHitboxes ? HideFlags.None : HideFlags.HideInHierarchy;
            weapon.bottom.gameObject.hideFlags = weapon.showHitboxes ? HideFlags.None : HideFlags.HideInHierarchy;

            if (weapon.lockHitBox)
            {                
                weapon.top.transform.localPosition = new Vector3(0, 1.5f, 0);
                weapon.top.transform.localRotation = Quaternion.Euler(Vector3.zero);
                weapon.top.transform.localScale = new Vector3(1, ((3f * 0.5f) - (curCenterSize * 0.5f)) - weapon.centerPos, 1);
                weapon.top.size = Vector3.one;
                weapon.top.center = new Vector3(0, -0.5f, 0);
                
                weapon.center.transform.localPosition = new Vector3(0, weapon.centerPos, 0);
                weapon.center.transform.localRotation = Quaternion.Euler(Vector3.zero);

                weapon.center.transform.localScale = new Vector3(1, curCenterSize, 1);
                weapon.center.size = Vector3.one;
                weapon.center.center = Vector3.zero;
            
                weapon.bottom.transform.localPosition = new Vector3(0, -1.5f, 0);
                weapon.bottom.transform.localRotation = Quaternion.Euler(Vector3.zero);
                weapon.bottom.transform.localScale = new Vector3(1, ((3f * 0.5f) - (curCenterSize * 0.5f)) + weapon.centerPos, 1);
                weapon.bottom.size = Vector3.one;
                weapon.bottom.center = new Vector3(0, 0.5f, 0);
            }           

            if (weapon.transform.childCount > 3){
                for (int i = 0; i < weapon.transform.childCount; i++){
                    if ((!weapon.transform.GetChild(i).Equals(weapon.top.transform)) &&
                       (!weapon.transform.GetChild(i).Equals(weapon.bottom.transform)) &&
                       (!weapon.transform.GetChild(i).Equals(weapon.center.transform)))
                    {
                        DestroyImmediate(weapon.transform.GetChild(i).gameObject);
                    }
                }
            }
        }
        catch
        {
            if (weapon.top == null || weapon.hitTop == null)
            {
                var _top = weapon.transform.FindChild("hitBox_Top");
                if (_top == null)
                {
                    _top = new GameObject("hitBox_Top").transform;
                    _top.parent = weapon.transform;
                }
                weapon.hitTop = _top.GetComponent<HitBox>() == null ? _top.gameObject.AddComponent<HitBox>() : _top.GetComponent<HitBox>();
                weapon.top = _top.GetComponent<BoxCollider>();
            }
            if (weapon.center == null || weapon.hitCenter == null)
            {
                var _center = weapon.transform.FindChild("hitBox_Center");
                if (_center == null)
                {
                    _center = new GameObject("hitBox_Center").transform;
                    _center.parent = weapon.transform;
                }
                weapon.hitCenter = _center.GetComponent<HitBox>() == null ? _center.gameObject.AddComponent<HitBox>() : _center.GetComponent<HitBox>();
                weapon.center = _center.GetComponent<BoxCollider>();
            }
            if (weapon.bottom == null || weapon.hitBotton == null)
            {
                var _botton = weapon.transform.FindChild("hitBox_Botton");
                if (_botton == null)
                {
                    _botton = new GameObject("hitBox_Botton").transform;
                    _botton.parent = weapon.transform;
                }
                weapon.hitBotton = _botton.GetComponent<HitBox>() == null ? _botton.gameObject.AddComponent<HitBox>() : _botton.GetComponent<HitBox>();
                weapon.bottom = _botton.GetComponent<BoxCollider>();
            }
        }
    }

    public override void OnInspectorGUI()
    {
        if (!skin) skin = Resources.Load("skin") as GUISkin;
        GUI.skin = skin;

        GUILayout.BeginVertical("window");
        EditorGUILayout.BeginVertical();
        base.OnInspectorGUI();
        GUILayout.EndVertical();
        EditorGUILayout.EndVertical();
    }
}
