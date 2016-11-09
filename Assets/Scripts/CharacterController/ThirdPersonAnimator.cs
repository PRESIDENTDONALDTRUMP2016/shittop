using UnityEngine;

namespace Invector
{
    public abstract class ThirdPersonAnimator : ThirdPersonMotor
    {
        #region Variables
        // generate a random idle animation
        private float randomIdleCount, randomIdle;
	    // used to lerp the head track
	    private Vector3 lookPosition;
        // match cursorObject to help animation to reach their cursorObject
        [HideInInspector] public Transform matchTarget;
	    // head track control, if you want to turn off at some point, make it 0
	    [HideInInspector] public float lookAtWeight;
	    // access the animator states (layers)
	    [HideInInspector] public AnimatorStateInfo stateInfo, upperBodyInfo;
        [HideInInspector] public float oldSpeed;
        public float speedTime
        {
            get
            {
                var _speed = animator.GetFloat("Speed");
                var acceleration = (_speed - oldSpeed) / Time.fixedDeltaTime;
                oldSpeed = _speed;
                return Mathf.Round(acceleration);
            }
        }

        #endregion

        /// ANIMATOR - update animations at the animator controller (Mecanim)
        public void UpdateAnimator()
        {
            if (ragdolled)        
                DisableActions();

            if (animator == null || !animator.enabled) return;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);

                RandomIdle();
                QuickTurn180Animation();                                
                RollAnimation();
                LandHighAnimation();
                JumpOverAnimation();
                ClimbUpAnimation();
                StepUpAnimation();
                JumpAnimation();
                LadderAnimation();                                
                QuickStopAnimation();
                HitReactionAnimation();
                HitRecoilAnimation();
                MeleeATKAnimation();
                ExtraMoveSpeed();
                MoveSetIDControl();                                
                LocomotionAnimation();
                DeadAnimation();                     
        }

        //public float lerpTransition;
        /// <summary>
        /// MOVE SET ID - check the Animator to see what MoveSet the character will move, also check your weapon to see if the moveset matches
        /// ps* Move Set is the way your character will move, ATK_ID is the way your character will attack. You can have different locomotion animations and attacks.
        /// </summary>
        void MoveSetIDControl(){
            if (!meleeManager) return;            

            moveSet_ID = (blocking && meleeManager.currentMeleeShield != null) ? meleeManager.currentMeleeShield.MoveSet_ID :
                         (meleeManager.currentMeleeWeapon != null ? meleeManager.currentMeleeWeapon.MoveSet_ID : 0);
        }

        void DeadAnimation(){
            if (!isDead) return;
            lockPlayer = true;

            if(deathBy == DeathBy.Animation){
                animator.SetBool("Dead", true);                                                    

                if (stateInfo.IsName("Action.Dead"))                
                    DisableActions();
            }
            else if(deathBy == DeathBy.AnimationWithRagdoll){
                animator.SetBool("Dead", true);
                if (stateInfo.IsName("Action.Dead")){
                    DisableActions();

                    // activate the ragdoll after the animation finish played
                    if (stateInfo.normalizedTime >= 0.8f)                    
                        SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);
                }
            }
            else if(deathBy == DeathBy.Ragdoll)
                SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);
        }

        /// HIT REACTION - it's activate on the TakeDamage() method by a trigger
        void HitReactionAnimation(){            
            hitReaction = stateInfo.IsName("Action.HitReaction");

            if (stateInfo.IsName("Action.HitReaction"))
            {
                jump = false; isJumping = false;
                if (stateInfo.normalizedTime < 0.2f)
                   animator.ResetTrigger("HitReaction");
            }            
        }

        /// HIT RECOIL = trigger a recoil animation when you hit a wall, check the RecoilReaction script at the weapon
        void HitRecoilAnimation(){
            hitRecoil = stateInfo.IsName("Action.HitRecoil");

            if (stateInfo.IsName("Action.HitRecoil"))
                animator.ResetTrigger("HitRecoil");
        }

        /// Trigger Recoil Animation - It's Called at the MeleeWeapon script using SendMessage
        public void TriggerRecoil(int recoil_id){
            animator.SetFloat("Recoil_ID", (float)recoil_id);
            animator.SetTrigger("HitRecoil");
        }

        /// ATTACK MELEE ANIMATION - it's activate by the AttackInput() method at the TPController by a trigger
        void MeleeATKAnimation()
        {
            if (meleeManager == null) return;
            // always reset the attack trigger on the last attack,
            // to make sure the character does not attack again when finish the last attack
            // if you created more attack states for different weapons like two handed, add the last clip here
            if (stateInfo.IsName("OneHandedSword.Attack C"))
                animator.ResetTrigger("MeleeAttack");

            animator.SetBool("InAttack", inAttack);            
            animator.SetInteger("ATK_ID", meleeManager.currentMeleeWeapon != null ? meleeManager.currentMeleeWeapon.ATK_ID : 0);
        }
  
        /// <summary>
        /// RANDOM IDLE	- assign the animations at the Layer IdleVariations on the Animator	
        /// </summary>
        void RandomIdle()
        {
            if(randomIdleTime > 0)
            {
                if (input.sqrMagnitude == 0 && !strafing && !crouch && _capsuleCollider.enabled && onGround)
                {
                    randomIdleCount += Time.fixedDeltaTime;
                    if (randomIdleCount > 6)
                    {
                        randomIdleCount = 0;
                        animator.SetTrigger("IdleRandomTrigger");
                        animator.SetInteger("IdleRandom", Random.Range(1, 4));
                    }
                }
                else
                {
                    randomIdleCount = 0;
                    animator.SetInteger("IdleRandom", 0);
                }
            }        
        }

        /// <summary>
        /// CONTROL LOCOMOTION
        /// </summary>
        void LocomotionAnimation()
        {
            animator.SetBool("Strafing", strafing);
            animator.SetBool("Crouch", crouch);
            animator.SetBool("OnGround", onGround);
            animator.SetFloat("GroundDistance", groundDistance);
            animator.SetFloat("VerticalVelocity", verticalVelocity);
            animator.SetFloat("MoveSet_ID", moveSet_ID, 0.1f, Time.fixedDeltaTime);

            if (freeLocomotionConditions)
                // free directional movement get the directional angle
                animator.SetFloat("Direction", lockPlayer ? 0f : direction, 0.1f, Time.fixedDeltaTime);
            else
                // strafe movement get the input 1 or -1
                animator.SetFloat("Direction", lockPlayer ? 0f : direction, 0.15f, Time.fixedDeltaTime);
            
            animator.SetFloat("Speed", !stopMove || lockPlayer ? speed : 0f, 0.2f, Time.fixedDeltaTime);
        }

        /// <summary>
        /// EXTRA MOVE SPEED - apply extra speed for the the free directional movement or the strafe movement
        /// </summary>
        void ExtraMoveSpeed()
        {
            if (!inAttack)
            {
                if (stateInfo.IsName("Grounded.Strafing Movement") || stateInfo.IsName("Grounded.Strafing Crouch"))
                {
                    var newSpeed_Y = (extraStrafeSpeed * speed);
                    var newSpeed_X = (extraStrafeSpeed * direction);
                    newSpeed_Y = Mathf.Clamp(newSpeed_Y, -extraStrafeSpeed, extraStrafeSpeed);
                    newSpeed_X = Mathf.Clamp(newSpeed_X, -extraStrafeSpeed, extraStrafeSpeed);
                    transform.position += transform.forward * (newSpeed_Y * Time.fixedDeltaTime);
                    transform.position += transform.right * (newSpeed_X * Time.fixedDeltaTime);
                }
                else if (stateInfo.IsName("Grounded.Free Movement") || stateInfo.IsName("Grounded.Free Crouch"))
                {
                    var newSpeed = (extraMoveSpeed * speed);
                    transform.position += transform.forward * (newSpeed * Time.fixedDeltaTime);
                }
            }
            else
                speed = 0f;  
        }

        /// QUICK TURN 180 ANIMATION
        void QuickTurn180Animation()
        {
            animator.SetBool("QuickTurn180", quickTurn180);

            // complete the 180 with matchTarget and disable quickTurn180 after completed
            if (stateInfo.IsName("Action.QuickTurn180"))
            {                
                if (!animator.IsInTransition(0) && !ragdolled)
                    animator.MatchTarget(Vector3.one, freeRotation, AvatarTarget.Root,
                                 new MatchTargetWeightMask(Vector3.zero, 1f),
                                 animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 0.9f);

                if (stateInfo.normalizedTime >= 0.9f)
                    quickTurn180 = false;                
            }
        }

        /// <summary>
        /// QUICK STOP ANIMATION
        /// </summary>
        void QuickStopAnimation()
        {
            animator.SetBool("QuickStop", quickStop);             

            bool quickStopConditions = !actions && onGround && !inAttack;
            
            if (inputType == InputType.MouseKeyboard)
            {
                // make a quickStop when release the key while running
                if (speedTime <= -3f && quickStopConditions)
                    quickStop = true;
            }
            else if (inputType == InputType.Controller)
            {
                // make a quickStop when release the analogue while running
                if (speedTime <= -6f && quickStopConditions)
                    quickStop = true;
            }                       

            // disable quickStop
            if (quickStop && input.sqrMagnitude >= 0.1f || quickTurn180 || inAttack)
                quickStop = false;
            else if (stateInfo.IsName("Action.QuickStop"))
            {
                if (stateInfo.normalizedTime > 0.9f || input.sqrMagnitude >= 0.1f || stopMove || inAttack)
                    quickStop = false;
            }
        }

        /// <summary>
        /// LADDER ANIMATION
        /// </summary>
        void LadderAnimation()
        {
            // resume the states of the ladder in one bool 
            usingLadder = 
                stateInfo.IsName("Ladder.EnterLadderBottom") ||
                stateInfo.IsName("Ladder.ExitLadderBottom") ||
                stateInfo.IsName("Ladder.ExitLadderTop") ||
                stateInfo.IsName("Ladder.EnterLadderTop") ||
                stateInfo.IsName("Ladder.ClimbLadder");

            // just to prevent any wierd blend between this animations
            if(usingLadder)
            {
                jump = false;
                quickTurn180 = false;
            }

            // make sure to lock the player when entering or exiting a ladder
            var lockOnLadder = 
                stateInfo.IsName("Ladder.EnterLadderBottom") ||
                stateInfo.IsName("Ladder.ExitLadderBottom") ||
                stateInfo.IsName("Ladder.ExitLadderTop") ||
                stateInfo.IsName("Ladder.EnterLadderTop");

            lockPlayer = lockOnLadder;

            LadderBottom();
            LadderTop();
        }

        void LadderBottom()
        {
            animator.SetBool("EnterLadderBottom", enterLadderBottom);
            animator.SetBool("ExitLadderBottom", exitLadderBottom);

            // enter ladder from bottom
            if (stateInfo.IsName("Ladder.EnterLadderBottom"))
            {
                _capsuleCollider.isTrigger = true;
                _rigidbody.useGravity = false;

                // we are using matchtarget to find the correct X & Z to start climb the ladder
                // this information is provided by the cursorObject on the object, that use the script TriggerAction 
                // in this state we are sync the position based on the AvatarTarget.Root, but you can use leftHand, left Foot, etc.
                if (!animator.IsInTransition(0))
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation,
                               AvatarTarget.Root, new MatchTargetWeightMask
                                (new Vector3(1, 1, 1), 1), 0.25f, 0.9f);

                if (stateInfo.normalizedTime >= 0.75f || hitReaction)
                    enterLadderBottom = false;                
            }

            // exit ladder bottom
            if (stateInfo.IsName("Ladder.ExitLadderBottom") || hitReaction)
            {
                _capsuleCollider.isTrigger = false;
                _rigidbody.useGravity = true;

                if (stateInfo.normalizedTime >= 0.4f || hitReaction)
                {
                    exitLadderBottom = false;
                    usingLadder = false;
                }
            }
        }

        void LadderTop()
        {
            animator.SetBool("EnterLadderTop", enterLadderTop);
            animator.SetBool("ExitLadderTop", exitLadderTop);

            // enter ladder from top            
            if (stateInfo.IsName("Ladder.EnterLadderTop"))
            {
                _capsuleCollider.isTrigger = true;
                _rigidbody.useGravity = false;

                // we are using matchtarget to find the correct X & Z to start climb the ladder
                // this information is provided by the cursorObject on the object, that use the script TriggerAction 
                // in this state we are sync the position based on the AvatarTarget.Root, but you can use leftHand, left Foot, etc.
                if (stateInfo.normalizedTime < 0.25f && !animator.IsInTransition(0))
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation,
                                AvatarTarget.Root, new MatchTargetWeightMask
                                (new Vector3(1, 0, 0.1f), 1), 0f, 0.25f);
                else if (!animator.IsInTransition(0))
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation,
                                AvatarTarget.Root, new MatchTargetWeightMask
                                (new Vector3(1, 1, 1), 1), 0.25f, 0.7f);

                if (stateInfo.normalizedTime >= 0.7f || hitReaction)
                    enterLadderTop = false;
            }

            // exit ladder top
            if (stateInfo.IsName("Ladder.ExitLadderTop") || hitReaction)
            {
                if (stateInfo.normalizedTime >= 0.85f || hitReaction)
                {
                    _capsuleCollider.isTrigger = false;
                    _rigidbody.useGravity = true;
                    exitLadderTop = false;
                    usingLadder = false;
                }
            }
        }

        /// ROLL ANIMATION
        void RollAnimation()
        {
            animator.SetBool("Roll", roll);

            // rollFwd
            if (stateInfo.IsName("Action.Roll"))
            {
                lockPlayer = true;
                _rigidbody.useGravity = false;

			    // prevent the character to rolling up 
                if (verticalVelocity >= 1)
                    _rigidbody.velocity = Vector3.ProjectOnPlane(_rigidbody.velocity, groundHit.normal);

                // reset the rigidbody a little ealier to the character fall while on air
                if (stateInfo.normalizedTime > 0.3f)
                    _rigidbody.useGravity = true;

                // transition back if the character is not crouching
                if (!crouch && stateInfo.normalizedTime > 0.85f)
                {
                    lockPlayer = false;
                    roll = false;
                }
                // transition back if the character is crouching
                else if (crouch && stateInfo.normalizedTime > 0.75f)
			    {
				    lockPlayer = false;
				    roll = false;
			    }
            }
        }

        /// ROLLING BEHAVIOUR
	    public void Rolling()
	    {
            bool staminaCondition = currentStamina > actionsController.Roll.staminaCost;
		    bool conditions = (strafing && input != Vector2.zero || speed > 0.25f) && !stopMove && onGround && !actions && !landHigh && staminaCondition;
		
		    if (conditions && !animator.IsInTransition(0))
		    {			
			    if (strafing)
			    {
				    // check the right direction for rolling if you are strafing
				    freeRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
				    var newAngle = freeRotation.eulerAngles - transform.eulerAngles;
				    direction = newAngle.NormalizeAngle().y;
                    transform.Rotate(0, direction, 0, Space.Self);
                }
			    roll = true;               
                ReduceStamina(actionsController.Roll.staminaCost);
                recoveryDelay = actionsController.Roll.recoveryDelay;
                quickTurn180 = false;
		    }
	    }

        /// JUMP ANIMATION AND BEHAVIOUR
        void JumpAnimation(){
            animator.SetBool("Jump", jump);

            var jumpAirControl = actionsController.Jump.jumpAirControl;
            var jumpForce = actionsController.Jump.jumpForce;
            var jumpForward = actionsController.Jump.jumpForward;
            var newSpeed = (jumpForward * speed);           

            isJumping = stateInfo.IsName("Action.Jump") || stateInfo.IsName("Action.JumpMove") || stateInfo.IsName("Airborne.FallingFromJump");
            animator.SetBool("IsJumping", isJumping);

            if (stateInfo.IsName("Action.Jump")){
                // apply extra height to the jump
                if (stateInfo.normalizedTime < 0.85f){
                    _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, jumpForce, _rigidbody.velocity.z);
                    transform.position += transform.up * (jumpForce * Time.fixedDeltaTime);
                }
                // apply extra speed forward
                if (stateInfo.normalizedTime >= 0.65f && jumpAirControl)                    
                    transform.position += transform.forward * (newSpeed * Time.fixedDeltaTime);
                else if (stateInfo.normalizedTime >= 0.65f && !jumpAirControl)
                    transform.position += transform.forward * Time.fixedDeltaTime;
                // end jump animation
                if (stateInfo.normalizedTime >= 0.6f || hitReaction)
                    jump = false;
            }

            if (stateInfo.IsName("Action.JumpMove"))
            {
                // apply extra height to the jump
                if (stateInfo.normalizedTime < 0.85f)
                {
                    _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, jumpForce, _rigidbody.velocity.z);
                    transform.position += transform.up * (jumpForce * Time.fixedDeltaTime);
                }               
                // apply extra speed forward
                if (jumpAirControl)
                    //_rigidbody.velocity = transform.forward * (newSpeed * Time.fixedDeltaTime);
                    transform.position += transform.forward * (newSpeed * Time.fixedDeltaTime);
                else                    
                    transform.position += transform.forward * Time.fixedDeltaTime;

                // end jump animation
                if (stateInfo.normalizedTime >= 0.55f || hitReaction)
                    jump = false;
            }

            // apply extra speed forward when falling
            if (stateInfo.IsName("Airborne.FallingFromJump") && jumpAirControl)                
                transform.position += transform.forward * (newSpeed * Time.fixedDeltaTime);
            else if (stateInfo.IsName("Airborne.FallingFromJump") && !jumpAirControl)                
                transform.position += transform.forward * Time.fixedDeltaTime;
        }

        /// HARD LANDING ANIMATION
        void LandHighAnimation(){
            animator.SetBool("LandHigh", landHigh);

		    // if the character fall from a great height, landhigh animation
		    if (!onGround && verticalVelocity <= landHighVel && groundDistance <= 0.5f)
                landHigh = true;

            if (landHigh && stateInfo.IsName("Airborne.LandHigh")){
                quickStop = false;
                if (stateInfo.normalizedTime >= 0.1f && stateInfo.normalizedTime <= 0.2f)
			    {
				    // vibrate the controller 
				    #if !UNITY_WEBPLAYER && !UNITY_ANDROID
				    StartCoroutine(GamepadVibration(0.15f));
				    #endif	
			    }
			
			    if (stateInfo.normalizedTime > 0.9f || hitReaction)
                    landHigh = false;
            }
        }

        /// STEP UP ANIMATION
        void StepUpAnimation(){
            animator.SetBool("StepUp", stepUp);

            if (stateInfo.IsName("Action.StepUp")){
                if (stateInfo.normalizedTime > 0.1f && stateInfo.normalizedTime < 0.3f)
                {
                    _capsuleCollider.isTrigger = true;
                    _rigidbody.useGravity = false;
                }

			    // we are using matchtarget to find the correct height of the object                
                if (!animator.IsInTransition(0))
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation,
                                AvatarTarget.LeftHand, new MatchTargetWeightMask
                                (new Vector3(0, 1, 1), 0), 0f, 0.5f);

                if (stateInfo.normalizedTime > 0.9f || hitReaction)
                {
                    _capsuleCollider.isTrigger = false;
                    _rigidbody.useGravity = true;
                    stepUp = false;
                }
            }
        }

        /// JUMP OVER ANIMATION
        void JumpOverAnimation(){
            animator.SetBool("JumpOver", jumpOver);

		    if (stateInfo.IsName("Action.JumpOver"))
            {
                quickTurn180 = false;
			    if (stateInfo.normalizedTime > 0.1f && stateInfo.normalizedTime < 0.3f)
                {
                    _rigidbody.useGravity = false;
                    _capsuleCollider.isTrigger = true;
                }				    

                // we are using matchtarget to find the correct height of the object
                if (!animator.IsInTransition(0))
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation,
                                AvatarTarget.LeftHand, new MatchTargetWeightMask
                                (new Vector3(0, 1, 1), 0), 0.1f*(1-stateInfo.normalizedTime), 0.3f*(1 - stateInfo.normalizedTime));

                if (stateInfo.normalizedTime >= 0.7f || hitReaction)
                {
				    _rigidbody.useGravity = true;
                    _capsuleCollider.isTrigger = false;
                    jumpOver = false;
			    }                
            }
        }

        /// CLIMB ANIMATION
        void ClimbUpAnimation()
        {
            animator.SetBool("ClimbUp", climbUp);

            if (stateInfo.IsName("Action.ClimbUp"))
            {
                if (stateInfo.normalizedTime > 0.1f && stateInfo.normalizedTime < 0.3f)
                {
				    _rigidbody.useGravity = false;
                    _capsuleCollider.isTrigger = true;               
                }

			    // we are using matchtarget to find the correct height of the object
                if (!animator.IsInTransition(0))
                    animator.MatchTarget(matchTarget.position, matchTarget.rotation,
                               AvatarTarget.LeftHand, new MatchTargetWeightMask
                               (new Vector3(0, 1, 1), 0), 0f, 0.2f);

                if(crouch)
                {
                    if (stateInfo.normalizedTime >= 0.7f || hitReaction)
                    {
                        _capsuleCollider.isTrigger = false;
                        _rigidbody.useGravity = true;
                        climbUp = false;
                    }
                }
                else
                {
                    if (stateInfo.normalizedTime >= 0.9f || hitReaction)
                    {
                        _capsuleCollider.isTrigger = false;
                        _rigidbody.useGravity = true;
                        climbUp = false;
                    }
                }                
            }
        }
               
        Vector3 lookPoint(float distance)
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            var point = ray.GetPoint(distance);
            if (tpCamera != null && tpCamera.lockTarget != null)            
                point = tpCamera.lockTarget.GetPointOffBoundsCenter(0.2f);
            
            return point;
        }

        /// HEAD TRACK - head follow where you point at and curve the spine while aiming
        void OnAnimatorIK()
       {                        
            var rot = Quaternion.LookRotation(lookPoint(1000f) - transform.position, Vector3.up);
            var newAngle = rot.eulerAngles - transform.eulerAngles;
            var ang = newAngle.NormalizeAngle().y;
            var angLimit = ang <= 70 && ang >= 0 || ang > -70 && ang <= 0;

            // Go to your Animator Controller and apply the Tag "HeadTrack" on the States that you want the character to have the headtrack effect
            bool headTrackConditions = headTracking && angLimit && !inAttack && !actions && !lockPlayer && stateInfo.IsTag("HeadTrack");

            if (!strafing)
            {
                if (headTrackConditions)
                    lookAtWeight += 1f * Time.deltaTime;
                else
                    lookAtWeight -= 1f * Time.deltaTime;

                lookAtWeight = Mathf.Clamp(lookAtWeight, 0f, 1f);
                animator.SetLookAtWeight(lookAtWeight, 0f, 1f);
            }
            else
            {
                if (headTrackConditions)
                    lookAtWeight += 1f * Time.deltaTime;
                else
                    lookAtWeight -= 1f * Time.deltaTime;

                lookAtWeight = Mathf.Clamp(lookAtWeight, 0f, 1f);
                animator.SetLookAtWeight(lookAtWeight, spineCurvature, 1f);
            }

            lookPosition = Vector3.Lerp(lookPosition, lookPoint(1000f), 10f * Time.fixedDeltaTime);
            animator.SetLookAtPosition(lookPosition);                      
        }       
    }
}