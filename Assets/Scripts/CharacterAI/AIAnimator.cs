using UnityEngine;

namespace Invector{
    public class AIAnimator : AIMotor{
        #region AI Variables       

        AnimatorStateInfo stateInfo;

        [HideInInspector]
        public Transform matchTarget;
        private float lookAtWeight;

        #endregion

        /// Animation behaviour of the AI
        public void UpdateAnimator(float _speed, float _direction)
        {
            if (animator == null || !animator.enabled) return;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            LocomotionAnimation(_speed, _direction);
            QuickTurnAnimation(_direction);
            StepUpAnimation();
            JumpOverAnimation();
            ClimbUpAnimation();
            RollAnimation();
            CrouchAnimation();
            HitReactionAnimation();
            HitRecoilAnimation();
            MeleeATKAnimation();
            DeadAnimation();
        }

        /// Control the Locomotion behaviour of the AI
        void LocomotionAnimation(float _speed, float _direction)
        {
            onGround = (method != OffMeshLinkMoveMethod.JumpAcross && method != OffMeshLinkMoveMethod.DropDown);
            animator.SetBool("OnGround", onGround);
            var maxSpeed = currentState.Equals(AIStates.Idle) ? 0 :
                          (currentState.Equals(AIStates.PatrolPoints) || currentState.Equals(AIStates.PatrolWaypoints) ? patrolSpeed :
                          (OnStrafeArea ? strafeSpeed : chaseSpeed));

            _speed = Mathf.Clamp(_speed, -maxSpeed, maxSpeed);
            if (OnStrafeArea) _direction = Mathf.Clamp(_direction, -strafeSpeed, strafeSpeed);

            animator.SetFloat("Speed", actions ? 0 : (_speed != 0) ? _speed : 0, 0.25f, Time.fixedDeltaTime);
            animator.SetFloat("Direction", _direction, 0.1f, Time.fixedDeltaTime);
            animator.SetBool("Strafing", OnStrafeArea);
            animator.SetFloat("MoveSet_ID", moveSet_ID, 0.2f, Time.fixedDeltaTime);
        }

        /// Trigger a Death by Animation, Animation with Ragdoll or just turn the Ragdoll On
        void DeadAnimation(){
            if (currentHealth > 0) return;

            agent.enabled = false;

            if (deathBy == DeathBy.Animation)
            {
                animator.SetBool("Dead", true);
                _capsuleCollider.isTrigger = true;

                if (stateInfo.IsName("Action.Dead") && stateInfo.normalizedTime >= 1f)
                    RemoveComponents();
            }
            else if (deathBy == DeathBy.AnimationWithRagdoll)
            {
                animator.SetBool("Dead", true);
                _capsuleCollider.isTrigger = true;
                if (stateInfo.IsName("Action.Dead"))
                {
                    // activate the ragdoll after the animation finish played
                    if (stateInfo.normalizedTime >= 0.8f)
                        SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);
                }
            }
            else if (deathBy == DeathBy.Ragdoll)
                SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);

            if (stateInfo.normalizedTime > 0.1f)
            {
                _rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                _rigidbody.useGravity = true;
                agent.enabled = false;
            }

            CheckGroundDistance();

            if (!agent.enabled && !actions && !rolling && groundDistance < 0.3f)
            {
                _rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                _rigidbody.useGravity = false;
                //agent.enabled = true;
            }
        }

        /// Control Attack Behaviour
        void MeleeATKAnimation()
        {
            if (meleeManager == null) return;
            // always reset the attack trigger on the last attack,
            // to make sure the character does not attack again when finish the last attack
            // if you created more attack states for different weapons like two handed, add the last clip here
            if (stateInfo.IsName("OneHandedSword.Attack C"))
                animator.ResetTrigger("MeleeAttack");

            if (actions) attackCount = 0;

            animator.SetBool("InAttack", meleeManager.inAttack);
            animator.SetInteger("ATK_ID", meleeManager.currentMeleeWeapon != null ? meleeManager.currentMeleeWeapon.ATK_ID : 0);
        }

        /// Trigger the Attack animation
        public void MeleeAttack()
        {
            if (animator != null && animator.enabled)
                animator.SetTrigger("MeleeAttack");
        }

        /// Trigger the HitReaction animation
        void HitReactionAnimation(){
            hitReaction = stateInfo.IsName("Action.HitReaction");

            if (stateInfo.IsName("Action.HitReaction")){
                animator.ResetTrigger("HitReaction");

                if (stateInfo.normalizedTime > 0.1f){
                    _rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                    _rigidbody.useGravity = true;
                    agent.enabled = false;
                }

                CheckGroundDistance();

                if (!agent.enabled && !actions && !rolling && groundDistance < 0.3f)
                {
                    _rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                    _rigidbody.useGravity = false;
                    agent.enabled = true;
                }
            }
        }

        /// HIT RECOIL = trigger a recoil animation when you hit wall or shield, check the RecoilReaction script at the weapon
        void HitRecoilAnimation()
        {
            hitRecoil = stateInfo.IsName("Action.HitRecoil");

            if (stateInfo.IsName("Action.HitRecoil"))
            {
                animator.ResetTrigger("HitRecoil");

                if (stateInfo.normalizedTime > 0.1f)
                {
                    _rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                    _rigidbody.useGravity = true;
                    agent.enabled = false;
                }
                CheckGroundDistance();

                if (!agent.enabled && !actions && !rolling && groundDistance < 0.3f)
                {
                    _rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                    _rigidbody.useGravity = false;
                    agent.enabled = true;
                }
            }
        }

        /// Trigger Recoil Animation - It's Called at the MeleeWeapon script using SendMessage
        public void TriggerRecoil(int recoil_id){
            animator.SetFloat("Recoil_ID", (float)recoil_id);
            animator.SetTrigger("HitRecoil");
            canAttack = true;
        }

        void QuickTurnAnimation(float _direction){
            animator.SetBool("QuickTurn180", quickTurn);

            if (stateInfo.IsName("Action.QuickTurn180"))
            {
                // exit state
                if (stateInfo.normalizedTime > 0.9f)
                    quickTurn = false;
            }
        }

        void StepUpAnimation()
        {
            animator.SetBool("StepUp", stepUp);

            if (stateInfo.IsName("Action.StepUp"))
            {
                // enter state
                if (stateInfo.normalizedTime > 0.1f && stateInfo.normalizedTime < 0.3f)
                {
                    agent.enabled = false;
                    quickTurn = false;
                    _capsuleCollider.isTrigger = true;
                    _rigidbody.useGravity = false;
                }

                // match target to find the target pos/rot                 
                if (!animator.IsInTransition(0))
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation, AvatarTarget.LeftHand,
                                                          new MatchTargetWeightMask(new Vector3(0, 1, 1), 0f), 0f, 0.5f);

                // exit state
                if (stateInfo.normalizedTime > 0.9f)
                {
                    _capsuleCollider.isTrigger = false;
                    _rigidbody.useGravity = true;
                    stepUp = false;
                    agent.enabled = true;
                    quickTurn = false;
                }
            }
        }

        void JumpOverAnimation(){
            animator.SetBool("JumpOver", jumpOver);

            if (stateInfo.IsName("Action.JumpOver"))
            {
                // enter state
                if (stateInfo.normalizedTime > 0.01f && stateInfo.normalizedTime < 0.3f)
                {
                    agent.enabled = false;
                    quickTurn = false;
                    _capsuleCollider.isTrigger = true;
                    _rigidbody.useGravity = false;
                }

                // match target to find the target pos/rot 
                if (!animator.IsInTransition(0))
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation,
                                AvatarTarget.LeftHand, new MatchTargetWeightMask
                                (new Vector3(0, 1, 1), 0), 0.1f * (1 - stateInfo.normalizedTime), 0.3f * (1 - stateInfo.normalizedTime));

                // exit state
                if (stateInfo.normalizedTime >= 0.7f)
                {
                    _rigidbody.useGravity = true;
                    _capsuleCollider.isTrigger = false;
                    jumpOver = false;
                    agent.enabled = true;
                    quickTurn = false;                    
                }
            }
        }

        void ClimbUpAnimation(){
            animator.SetBool("ClimbUp", climbUp);

            if (stateInfo.IsName("Action.ClimbUp"))
            {
                // enter state
                if (stateInfo.normalizedTime > 0.01f && stateInfo.normalizedTime < 0.3f)
                {
                    agent.enabled = false;
                    quickTurn = false;
                    _rigidbody.useGravity = false;
                    _capsuleCollider.isTrigger = true;
                }

                // match target to find the target pos/rot 
                if (!animator.IsInTransition(0))
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation,
                               AvatarTarget.LeftHand, new MatchTargetWeightMask
                               (new Vector3(0, 1, 1), 0), 0f, 0.2f);
                // exit state
                if (crouch)
                {
                    if (stateInfo.normalizedTime >= 0.7f)
                    {
                        climbUp = false;
                        _capsuleCollider.isTrigger = false;
                        _rigidbody.useGravity = true;
                        agent.enabled = true;
                        quickTurn = false;
                    }
                }
                else
                {
                    if (stateInfo.normalizedTime >= 0.9f)
                    {
                        climbUp = false;
                        _capsuleCollider.isTrigger = false;
                        _rigidbody.useGravity = true;
                        agent.enabled = true;
                        quickTurn = false;
                    }
                }
            }
        }

        void CrouchAnimation(){
            animator.SetBool("Crouch", crouch);

            if (animator != null && animator.enabled)
                CheckAutoCrouch();
        }

        protected void RollAnimation(){
            if (animator == null || animator.enabled == false) return;

            animator.SetBool("Roll", rolling);
            if (stateInfo.IsName("Action.Roll")){
                // reset the rigidbody a little ealier for the character fall while on air              
                if (stateInfo.normalizedTime > 0.3f)
                {
                    _rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                    _rigidbody.useGravity = true;
                    agent.enabled = false;
                }

                // exit state
                if (!crouch && stateInfo.normalizedTime > 0.85f)
                {
                    rolling = false;
                    ResetAIRotation();
                }
                else if (crouch && stateInfo.normalizedTime > 0.75f)
                {
                    rolling = false;
                    ResetAIRotation();
                }
            }

            CheckGroundDistance();

            if (!agent.enabled && !actions && !rolling && groundDistance < 0.3f)
            {
                _rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
                _rigidbody.useGravity = false;
                agent.enabled = true;
            }
        }

        void ResetAIRotation(){
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }

        protected void CheckForwardAction(){
            var hitObject = CheckActionObject();
            if (hitObject != null){
                try
                {
                    if (hitObject.CompareTag("ClimbUp"))
                        DoAction(hitObject, ref climbUp);
                    else if (hitObject.CompareTag("StepUp"))
                        DoAction(hitObject, ref stepUp);
                    else if (hitObject.CompareTag("JumpOver"))
                        DoAction(hitObject, ref jumpOver);
                }
                catch (UnityException e)
                {
                    Debug.LogWarning(e.Message);
                }
            }
            else
            {
                if (agent.enabled)
                {
                    agent.ResetPath();
                    method = OffMeshLinkMoveMethod.Grounded;
                }
            }
        }

        void DoAction(GameObject hitObject, ref bool action){
            var triggerAction = hitObject.transform.GetComponent<TriggerAction>();
            if (!triggerAction){
                Debug.LogWarning("Missing TriggerAction Component on " + hitObject.transform.name + "Object");
                return;
            }

            if (!actions){
                matchTarget = triggerAction.target;
                var rot = hitObject.transform.rotation;
                transform.rotation = rot;
                action = true;
            }
        }

        void OnAnimatorMove(){
            if (agent.enabled)
                agent.velocity = animator.deltaPosition / Time.deltaTime;

            if (!_rigidbody.useGravity && !actions)
                _rigidbody.velocity = animator.deltaPosition;

            // Strafe Movement
            if (meleeManager != null && OnStrafeArea && !actions && target != null){
                Vector3 targetDir = target.position - transform.position;
                float step = meleeManager.inAttack ? attackRotationSpeed * Time.deltaTime : (strafeRotationSpeed * Time.deltaTime);
                Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0F);
                var rot = Quaternion.LookRotation(newDir);
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, rot.eulerAngles.y, transform.eulerAngles.z);
            }
            // Rotate the Character to the OffMeshLink End
            else if (agent.isOnOffMeshLink && !actions){
                var pos = agent.nextOffMeshLinkData.endPos;
                targetPos = pos;
                OffMeshLinkData data = agent.currentOffMeshLinkData;
                desiredRotation = Quaternion.LookRotation(new Vector3(data.endPos.x, transform.position.y, data.endPos.z) - transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, (agent.angularSpeed * 2f) * Time.deltaTime);
            }
            // Free Movement
            else if (agent.desiredVelocity.magnitude > 0.1f && !actions && agent.enabled){
                if(meleeManager.inAttack){
                    desiredRotation = Quaternion.LookRotation(agent.desiredVelocity);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, agent.angularSpeed * attackRotationSpeed * Time.deltaTime);
                }
                else{
                    desiredRotation = Quaternion.LookRotation(agent.desiredVelocity);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, agent.angularSpeed * Time.deltaTime);
                }                
            }
            // Use the Animator rotation while doing an Action
            else if (actions || currentHealth <= 0f){
                if (rolling){
                    desiredRotation = Quaternion.LookRotation(rollDirection - transform.position);
                    transform.rotation = desiredRotation;
                }                                  
                else               
                    transform.rotation = animator.rootRotation;                
                // Use the Animator position while doing an Action
                if (!agent.enabled) transform.position = animator.rootPosition;
            }
        }

        /// Head Track IK
        void OnAnimatorIK(){
            if (currentState == AIStates.Chase && onFovAngle() && !actions)
                lookAtWeight += 1f * Time.deltaTime;
            else
                lookAtWeight -= 1f * Time.deltaTime;

            lookAtWeight = Mathf.Clamp(lookAtWeight, 0f, 1f);
            animator.SetLookAtWeight(lookAtWeight, 0.1f, 0.3f);
            if (headTarget != null)
                animator.SetLookAtPosition(headTarget.position);
        }
    }
}
