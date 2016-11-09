using UnityEngine;
using Invector;

public class MeleeWeapon : MeleeItem {
    #region variables
    [Header("Weapon Settings")]
    public int ATK_ID;
    public int MoveSet_ID;
    
    public Damage damage;
    public DamagePercentage damagePercentage;

    [Tooltip("How much stamina the attack will consume")]
    public float staminaCost = 30f;
    [Tooltip("Time for the stamina to recover")]
    public float staminaRecoveryDelay = 1f;

    [Header("Hitbox Settings")]
    [Range(0.1f, 2.9f)]
    public float centerSize = 1f;
    [Range(-1.4f, 1.4f)]
    public float centerPos = 0f;
    public BoxCollider top, bottom, center;
    public bool showHitboxes;
    public HitProperties hitProperties { get; set; }
    protected float curCenterSize;
    [HideInInspector]
    public HitBox hitTop, hitBotton, hitCenter;
    public bool lockHitBox = true;            
    #endregion

    [System.Serializable]
    public class DamagePercentage{
        public float Top = 30f;
        public float Center = 70f;        
    }

    //public override Type GetSubType(){
    //    return GetType();
    //}
    public void Init(){
        hitTop = top.GetComponent<HitBox>();
        hitBotton = bottom.GetComponent<HitBox>();
        hitCenter = center.GetComponent<HitBox>();

        hitTop.hitBarPoint = HitBarPoints.Top;
        hitCenter.hitBarPoint = HitBarPoints.Center;
        hitBotton.hitBarPoint = HitBarPoints.Bottom;

        hitTop.hitControl = this;
        hitBotton.hitControl = this;
        hitCenter.hitControl = this;

        #region Set ignore collision for all hitBox
        //if (transform.GetComponentInParent<MeleeEquipmentManager>() != null){
        //    Physics.IgnoreCollision(top, bottom);
        //    Physics.IgnoreCollision(bottom, center);
        //    Physics.IgnoreCollision(center, top);
        //    var meleeManager = transform.GetComponentInParent<MeleeEquipmentManager>();
        //    if(meleeManager==null)
        //        Debug.LogWarning("The " + gameObject.name + " don't have a MeleeEquipmentManager");
        //    var col = meleeManager.GetComponent<Collider>();

        //    if (col != null && col.enabled){              
        //        Physics.IgnoreCollision(col, bottom);
        //        Physics.IgnoreCollision(col, center);
        //        Physics.IgnoreCollision(col, top);

        //        var colChild = meleeManager.GetComponentsInChildren<Collider>();
        //        foreach (Collider _col in colChild){
        //            if (!_col.Equals(bottom) &&
        //                !_col.Equals(center) &&
        //                !_col.Equals(top) && _col.enabled)
        //            {
        //                Physics.IgnoreCollision(_col, bottom);
        //                Physics.IgnoreCollision(_col, center);
        //                Physics.IgnoreCollision(_col, top);
        //            }
        //        }
        //    }
        //}
        #endregion
    }   

    public bool isActive { get { return active; } }

    public struct DamageType{
        public Collider hitCollider;
        public HitBarPoints hitBarPoints;
    }
}
