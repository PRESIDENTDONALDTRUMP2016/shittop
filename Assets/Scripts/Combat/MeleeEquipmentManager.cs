using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Invector;

public class MeleeEquipmentManager : MonoBehaviour
{    
    public HitProperties hitProperts;
    public List<Transform> weaponHandlers;

    [HideInInspector] public MeleeWeapon currentMeleeWeapon;
    [HideInInspector] public MeleeShield currentMeleeShield;
    [HideInInspector] public bool changeWeapon;
    [HideInInspector] public bool displayGizmos;
    [HideInInspector] public CollectableMelee currentCollectableShield;
    [HideInInspector] public CollectableMelee currentCollectableWeapon;    
    [HideInInspector] public bool inAttack;
    [HideInInspector] public int damageModifier;
    [HideInInspector] public Damage.Recoil_ID currentRecoilLevel;

    protected List<Collider> hitTopColliders;
    protected List<Collider> hitCenterColliders;
    protected List<Collider> hitBottomColliders;    
    private Transform bodyCenter;    

    void Start()
    {
        var animator = GetComponent<Animator>();
        if (animator == null) Destroy(this);
        bodyCenter = GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Spine);

        hitTopColliders = new List<Collider>();
        hitCenterColliders = new List<Collider>();
        hitBottomColliders = new List<Collider>();        
        SetMeleeWeapon(HumanBodyBones.RightHand);
        SetMeleeShield(HumanBodyBones.LeftLowerArm);
    } 
    public void SetInAttack(bool value)
    {
        if (!currentMeleeWeapon) return;

        inAttack = value;
        currentMeleeWeapon.SetActive(value);
        if (!inAttack)
        {
            ClearHitColliders();
        }        
    } 
   
    public void ClearHitColliders()
    {
        hitTopColliders.Clear();
        hitCenterColliders.Clear();
        hitBottomColliders.Clear();
    }   

    public void OnDamageHit(HitBox.HitInfo hitInfo)
    {
        var damage = new Damage(currentMeleeWeapon.damage);
        switch (hitInfo.hitBarPoint)
        {
            case HitBarPoints.Top:
                damage.value = (int)((currentMeleeWeapon.damagePercentage.Top * (currentMeleeWeapon.damage.value)) / 100);
                TryApplyDamage(hitInfo.hitCollider, damage, HitBarPoints.Top);
                break;
            case HitBarPoints.Center:
                damage.value = (int)((currentMeleeWeapon.damagePercentage.Center * (currentMeleeWeapon.damage.value)) / 100);
                TryApplyDamage(hitInfo.hitCollider, damage, HitBarPoints.Center);
                break;
        }   
    }

    void TryApplyDamage(Collider other, Damage damage, HitBarPoints hitBarPoint)
    {
        damage.sender = transform;
        damage.recoil_id = currentRecoilLevel;
        switch (hitBarPoint)
        {
            case HitBarPoints.Top:
                if (!hitTopColliders.Contains(other))
                {                    
                    other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                    hitTopColliders.Add(other);
                }
                break;
            case HitBarPoints.Center:
                if (!hitCenterColliders.Contains(other))
                {                    
                    other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                    hitCenterColliders.Add(other);
                }
                break;
        }
    }

    public void OnRecoilHit(HitBox.HitInfo hitInfo)
    {
        if (!hitProperts.useRecoil || !inAttack) return;
        Ray ray = new Ray(bodyCenter.position, transform.forward);
        var inForward = (Physics.SphereCast(new Ray(ray.origin, ray.GetPoint(0.1f) - bodyCenter.position), 0.1f, hitProperts.recoilMaxDistance, hitProperts.hitRecoilLayer));

        if (hitInfo.hitBarPoint == HitBarPoints.Bottom && inForward)
        {
            //var recoil_id = (int)damage.recoil_id - (int)(currentMeleeShield != null ? currentMeleeShield.Recoil_ID : 0);
            SendMessage("TriggerRecoil", 0, SendMessageOptions.DontRequireReceiver);
            inAttack = false;
            ClearHitColliders();
        }
    }

    public void SetRecoilLevel(Damage.Recoil_ID recoilLevel)
    {
        currentRecoilLevel = recoilLevel;
    }
        
    public void SetMeleeWeapon(HumanBodyBones bodyPart)
    {
        var part = GetComponent<Animator>().GetBoneTransform(bodyPart);
        if (part)
        {
            var meleeWeapon = part.GetComponentInChildren<MeleeWeapon>();
            var collectWeapon = part.GetComponentInChildren<CollectableMelee>();
            currentMeleeWeapon = meleeWeapon;
            currentCollectableWeapon = collectWeapon;
            
            if(currentMeleeWeapon)
            {
                currentMeleeWeapon.Init();
                currentMeleeWeapon.hitProperties = this.hitProperts;                
            }
            changeWeapon = false;
        }
    }
   
    public void SetWeaponHandler(CollectableMelee weapon)
    {
        if(!changeWeapon)
        {
            changeWeapon = true;
            var handler = weaponHandlers.Find(h => h.name.Equals(weapon.handler));

            if (handler)
            {                
                DropWeapon();
                weapon.transform.position = handler.position;
                weapon.transform.rotation = handler.rotation;
                weapon.transform.parent = handler;
                weapon.EnableMeleeItem();
                SetMeleeWeapon(HumanBodyBones.RightHand);                
            }
            else
            {
                Debug.LogWarning("Missing " + weapon.name + " handler, please create and assign one at the MeleeWeaponManager");
            }
        }        
    }

    public void DropWeapon()
    {
        if(currentCollectableWeapon != null)
        {
            currentCollectableWeapon.transform.parent = null;            
            currentCollectableWeapon.DisableMeleeItem();
            if(currentCollectableWeapon.destroyOnDrop)           
                Destroy(currentCollectableWeapon.gameObject);       
            currentMeleeWeapon = null;
        }
    }
        
    public void SetMeleeShield(HumanBodyBones bodyPart)
    {
        var part = GetComponent<Animator>().GetBoneTransform(bodyPart);
        if (part)
        {
            var meleeShield = part.GetComponentInChildren<MeleeShield>();
            var collectShield = part.GetComponentInChildren<CollectableMelee>();
            currentMeleeShield = meleeShield;
            currentCollectableShield = collectShield;
            if (currentMeleeShield)            
                currentMeleeShield.Init();
           
            changeWeapon = false;
        }
    }

    public void SetShieldHandler(CollectableMelee shield)
    {
        if (!changeWeapon)
        {
            changeWeapon = true;
            var handler = weaponHandlers.Find(h => h.name.Equals(shield.handler));

            if (handler)
            {
                DropShield();
                shield.transform.position = handler.position;
                shield.transform.rotation = handler.rotation;
                shield.transform.parent = handler;
                shield.EnableMeleeItem();
                SetMeleeShield(HumanBodyBones.LeftLowerArm);
            }
            else
            {
                Debug.LogWarning("Missing " + shield.name + " handler, please create and assign one at the MeleeWeaponManager");
            }
        }
    }

    public void DropShield()
    {
        if (currentCollectableShield != null)
        {
            currentCollectableShield.transform.parent = null;            
            currentCollectableShield.DisableMeleeItem();
            if (currentCollectableShield.destroyOnDrop)
                Destroy(currentCollectableShield.gameObject);
            currentMeleeShield = null;
        }
    }
}

[System.Serializable]
public class HitProperties
{
    [Tooltip("Trigger a HitRecoil animation if the character attacks a obstacle")]
    public bool useRecoil = true;
    public float recoilMaxDistance = 1f;
    [Tooltip("layer to Recoil Damage")]
    public LayerMask hitRecoilLayer = 1 << 0;
    [Tooltip("Tag to receiver Damage")]
    public List<string> hitDamageTags = new List<string>() { "Enemy" };
}