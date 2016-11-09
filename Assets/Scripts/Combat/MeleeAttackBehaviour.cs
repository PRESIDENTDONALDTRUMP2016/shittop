using UnityEngine;

public class MeleeAttackBehaviour : StateMachineBehaviour{   
    [Header("Work With SimpleWeaponControl")]
    [Tooltip("normalizedTime of Active Damage")]   
    public float startDamage = 0.05f;
    [Tooltip("normalizedTime of Desable Damage")]
    public float endDamage = 0.9f;
    public Damage.Recoil_ID recoilLevel;

    //OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
        animator.gameObject.SendMessage("OnAttackEnter", SendMessageOptions.DontRequireReceiver);
        animator.gameObject.SendMessage("SetRecoilLevel", recoilLevel, SendMessageOptions.DontRequireReceiver);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //}

    // OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){        
        if (stateInfo.normalizedTime >= startDamage && stateInfo.normalizedTime <= endDamage)      
            animator.gameObject.SendMessage("SetInAttack", true, SendMessageOptions.DontRequireReceiver);
        else    
            animator.gameObject.SendMessage("SetInAttack", false, SendMessageOptions.DontRequireReceiver);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex){
        animator.gameObject.SendMessage("SetInAttack", false, SendMessageOptions.DontRequireReceiver);
        animator.gameObject.SendMessage("OnAttackExit", SendMessageOptions.DontRequireReceiver);
    }

    // OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
    //
    //}
}