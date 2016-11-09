using UnityEngine;

public class MeleeShield : MeleeItem{
    public int MoveSet_ID;
    public Damage.Recoil_ID Recoil_ID;
    public float defenseRate = 100f;
    public float defenseRange = 90f;
    public float staminaRecoveryDelay = 1f;
    [Tooltip("Break the attack of the opponent and trigger a recoil animation")]
    public bool breakAttack;

    private Transform root;

    public void Init(){
        var meleeManager = GetComponentInParent<MeleeEquipmentManager>();
        if(meleeManager != null)
            root = meleeManager.transform;
    }
    public bool AttackInDefenseRange(Transform damageSender){
        var localTarget = root.InverseTransformPoint(damageSender.position);
        var angle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        if (angle <= defenseRange && angle >= -defenseRange)
            return true;
        return false;
    }
}