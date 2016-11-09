using UnityEngine;
using UnityEngine.UI;

namespace Invector{
    public abstract class ThirdPersonMotor : Character{
        #region Character Variables

        public enum LocomotionType{
            FreeWithStrafe,
            OnlyStrafe,
            OnlyFree
        }

        [Header("--- Locomotion Setup ---")]

        public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;

        [Tooltip("The character Head will follow where you look at, UNCHECK if you are using TopDown or 2.5D")]
        [SerializeField]
        protected bool headTracking = true;            

        [Tooltip("Use this to rotate the character using the World axis, or false to use the camera axis - CHECK for Isometric Camera")]
        [SerializeField]
        protected bool rotateByWorld = false;

        [Tooltip("Speed of the rotation on free directional movement")]
        [SerializeField]
        protected float rotationSpeed = 8f;

        [Tooltip("Add extra speed for the locomotion movement, keep this value at 0 if you want to use only root motion speed.")]
        [SerializeField]
        protected float extraMoveSpeed = 0f;

        [Tooltip("Add extra speed for the strafe movement, keep this value at 0 if you want to use only root motion speed.")]
        [SerializeField]
        protected float extraStrafeSpeed = 0f;

        [Tooltip("ADJUST IN PLAY MODE - White Spherecast put just above the head, this will make the character Auto-Crouch if something hit the sphere.")]
        public float headDetect = 0.96f;

        [Tooltip("How much the character will curve the spine while Strafing")]
        [SerializeField]
        protected float spineCurvature = 0.5f;

        [Tooltip("Put your Random Idle animations at the AnimatorController and select a value to randomize, 0 is disable.")]
        [SerializeField]
        protected float randomIdleTime = 0f;

        [Header("--- Grounded Setup ---")]
	    [Tooltip("Distance to became not grounded")]
        [SerializeField]
        protected float groundCheckDistance = 0.5f;
        protected float groundDistance;
        public RaycastHit groundHit;

        [Tooltip("ADJUST IN PLAY MODE - Offset height limit for sters - GREY Raycast in front of the legs")]
        public float stepOffsetEnd = 0.45f;
        [Tooltip("ADJUST IN PLAY MODE - Offset height origin for sters, make sure to keep slight above the floor - GREY Raycast in front of the legs")]
        public float stepOffsetStart = 0.05f;
        [Tooltip("Higher value will result jittering on ramps, lower values will have difficulty on steps")]
        public float stepSmooth = 2f;

        [Tooltip("Max angle to walk")]
        [SerializeField]
        protected float slopeLimit = 45f;
	
	    [Tooltip("Apply extra gravity when the character is not grounded")]
        [SerializeField]
        protected float extraGravity = 4f;
	
	    [Tooltip("Select a VerticalVelocity to turn on Land High animation")]
        [SerializeField]
        protected float landHighVel = -5f;
	
	    [Tooltip("Turn the Ragdoll On when falling at high speed (check VerticalVelocity) - leave the value with 0 if you don't want this feature")]
        [SerializeField]
        protected float ragdollVel = -8f;		            
    
        [Header("--- Debug Info ---")]
        [SerializeField]
        protected bool debugWindow;
	    [Range(0f, 1f)] public float timeScale = 1f;

        protected float moveSet_ID;
        protected GameObject currentCollectable;
        protected AIController targetEnemy;
        #endregion

        #region Actions

        // general bools of movement        
        protected bool 
		    onGround, 		stopMove, 		autoCrouch, 
		    quickStop,		quickTurn180,   canSprint,		
            crouch, 		strafing,       landHigh,
            jump,           isJumping,      sliding;

        // actions bools, used to turn on/off actions animations *check ThirdPersonAnimator*	        
        protected bool
            jumpOver,
            stepUp,
            climbUp,
            roll,
            enterLadderBottom,
            enterLadderTop,
            usingLadder,
            exitLadderBottom,
            exitLadderTop,
            inAttack,
            blocking, 
            hitReaction,
            hitRecoil;

        // one bool to rule then all
        protected bool actions
        {
            get{
                return jumpOver || stepUp || climbUp || roll || usingLadder || quickStop || quickTurn180 || hitReaction || hitRecoil;
            }
        }

        ///  DISABLE ACTIONS - call this method in canse you need to turn every action bool false
        protected void DisableActions() {            
            inAttack = false;
            blocking = false;
            quickTurn180 = false;
            hitReaction = false;
            hitRecoil = false;
            quickStop = false;
            canSprint = false;
            strafing = false;
            landHigh = false;
            jumpOver = false;
            roll = false;
            stepUp = false;
            crouch = false;
            jump = false;
            isJumping = false;
        }

        #endregion

        #region Camera Variables
        
        [HideInInspector] public string customCameraState;// generic string to change the CameraState
        
        [HideInInspector] public string customlookAtPoint;// generic string to change the CameraPoint of the Fixed Point Mode
        
        [HideInInspector] public bool changeCameraState;// generic bool to change the CameraState
        
        [HideInInspector] public bool smoothCameraState;// generic bool to know if the state will change with or without lerp
        // generic variables to find the correct direction 
        [HideInInspector] public Quaternion freeRotation;
        [HideInInspector] public bool keepDirection;
        [HideInInspector] public Vector2 oldInput;
        [HideInInspector] public Vector3 cameraForward;
        [HideInInspector] public Vector3 cameraRight;
        #endregion

        /// UPDATE MOTOR - call this method on ThirdPersonController FixedUpdate
        public void UpdateMotor(){
            CheckHealth();
            CheckGround();
            CheckRagdoll();
            ControlHeight();
            ControlLocomotion();
            StaminaRecovery();
            HealthRecovery();
        }

        /// CHECK HEALTH - if you have a gameController in the scene, your character will respawn in 4f, otherwise the scene will reload
        void CheckHealth(){
            if (currentHealth <= 0 && !isDead){
                transform.root.SendMessage("DropWeapon", SendMessageOptions.DontRequireReceiver);
                transform.root.SendMessage("DropShield", SendMessageOptions.DontRequireReceiver);

                tpCamera.ClearTargetLockOn();                
                isDead = true;                
                
                if (hud != null)
                    hud.FadeText("You are Dead!", 2f, 0.5f);

                if (vGameController.instance != null && vGameController.instance.playerPrefab != null)
                    vGameController.instance.Invoke("Spawn", vGameController.instance.respawnTimer);
                else
                    vGameController.instance.Invoke("ResetScene", vGameController.instance.respawnTimer);
            }
        }

        /// Call this method when executing a action to reduce the stamina 
        public void ReduceStamina(float value){                        
            currentStamina -= value;
            if (currentStamina < 0)
                currentStamina = 0;
        }

        /// After calling the ReduceStamina, you need to pass a value to the recoveryDelay variable 
        /// example > recoveryDelay = actionsController.jump.recoveryDelay;
        public void StaminaRecovery(){           
            if(recoveryDelay > 0)
                recoveryDelay -= Time.deltaTime;
            else{
                if (!blocking) {
                    if (currentStamina > startingStamina)
                        currentStamina = startingStamina;
                    if (currentStamina < startingStamina)
                        currentStamina += staminaRecovery;
                }
            }           
        }
      
        /// Slowly recovery Health
        public void HealthRecovery()
        {
            if (currentHealth <= 0)
                return;
            if (currentHealthRecoveryDelay > 0)
                currentHealthRecoveryDelay -= Time.deltaTime;
            else
            {
                if (currentHealth > startingHealth)
                    currentHealth = startingHealth;
                if (currentHealth < startingHealth)
                    currentHealth = Mathf.Lerp(currentHealth, startingHealth, healthRecovery * Time.deltaTime);
            }
        }

        /// knows when you have enter the Attack state and apply the ReduceStamina
        public void OnAttackEnter()
        {
            inAttack = true;
            ReduceStamina(meleeManager.currentMeleeWeapon.staminaCost);
            recoveryDelay = meleeManager.currentMeleeWeapon.staminaRecoveryDelay;
        }

        /// knows when you have exit the Attack state
        public void OnAttackExit(){
            inAttack = false;
        }

        /// <summary>
        /// APPLY DAMAGE - call this method by a SendMessage with the damage value
        /// </summary>
        /// <param name="damage"> damage to apply </param>
        public void TakeDamage(Damage damage)
        {
            // don't apply damage if the character is rolling, you can add more conditions here
            if (!damage.ignoreDefense && roll || isDead)
                return;
            var defenceRangeCondictions = (meleeManager != null && meleeManager.currentMeleeShield != null ? meleeManager.currentMeleeShield.AttackInDefenseRange(damage.sender) : false);
            
            // don't apply damage if the character is blocking
            if (blocking && currentStamina > damage.value && !damage.ignoreDefense && defenceRangeCondictions)
            {
                var recoil_id = (int) damage.recoil_id - (int) (meleeManager.currentMeleeShield != null ? meleeManager.currentMeleeShield.Recoil_ID : 0);
                var targetRecoil_id = (int) damage.recoil_id +  (int) (meleeManager.currentMeleeShield != null ? meleeManager.currentMeleeShield.Recoil_ID : 0);

                SendMessage("TriggerRecoil", recoil_id, SendMessageOptions.DontRequireReceiver);
                if (meleeManager.currentMeleeShield.breakAttack) damage.sender.SendMessage("TriggerRecoil", targetRecoil_id, SendMessageOptions.DontRequireReceiver);
                ReduceStamina(damage.value);
                recoveryDelay = meleeManager.currentMeleeShield.staminaRecoveryDelay;
                currentHealth -= GetDamageResult(damage.value, meleeManager.currentMeleeShield.defenseRate);
                currentHealthRecoveryDelay = healthRecoveryDelay;
                return;
            }
            if((!actions || (usingLadder)) && !isJumping)
            {
                // trigger recoil animation
                animator.SetFloat("Recoil_ID", (float)damage.recoil_id);
                animator.SetTrigger("HitReaction");
            }            
            // reduce the current health by the damage amount.
            currentHealth -= damage.value;
            currentHealthRecoveryDelay = healthRecoveryDelay;
            // update the hud graphics 
            if (hud != null){
                // set the damaged flag so the screen will flash.
                //hud.damageImage.enabled = true;
                //hud.damaged = true;
                // set the health bar's value to the current health.
                hud.healthSlider.value = currentHealth;
            }
            // apply vibration on the gamepad             
            transform.SendMessage("GamepadVibration", 0.25f, SendMessageOptions.DontRequireReceiver);
            if(damage.activeRagdoll)            
                transform.SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);
        }

        int GetDamageResult(int damage, float defenseRate){
            int result = (int)(damage - ((damage * defenseRate) / 100));
            return result;
        }

        public void FindTargetLockOn(Transform target){
            var _targetEnemy = target.GetComponent<AIController>();
            if (_targetEnemy != null)
                targetEnemy = _targetEnemy;
        }

        public void LostTargetLockOn() {
            targetEnemy = null;
            strafing = false;
        }

        /// CONTROL LOCOMOTION BEHAVIOUR - change between strafe and free directional movement
        void ControlLocomotion(){
            if (lockPlayer) return;
            // free directional movement
            if (freeLocomotionConditions){
                // set speed to both vertical and horizontal inputs
                speed = Mathf.Abs(input.x) + Mathf.Abs(input.y);
                speed = Mathf.Clamp(speed, 0, 1);
                // add 0.5f on sprint to change the animation on animator
                if (canSprint) speed += 0.5f;
                if (stopMove) speed = 0f;
                if (input == Vector2.zero && !quickTurn180) direction = Mathf.Lerp(direction, 0f, 20f * Time.fixedDeltaTime);                
                                                
                if ((!actions || quickTurn180 || quickStop) && !inAttack) FreeRotationMovement();
            }
            // move forward, backwards, strafe left and right
            else{
                speed = input.y;
                direction = input.x;
            }
        }

        /// Conditions for Free Directional Movement
        public bool freeLocomotionConditions{
            get{
                if (locomotionType.Equals(LocomotionType.OnlyStrafe)) strafing = true;
                return !strafing && !usingLadder && !landHigh && !locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.OnlyFree);
            }
        }

        /// FREE ROTATION - handle the character rotation when on a free directional movement, also activate the turn180 animations.
        void FreeRotationMovement(){
            if (input != Vector2.zero && !quickTurn180 && !lockPlayer && targetDirection.magnitude > 0.1f){
                freeRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                Vector3 velocity = Quaternion.Inverse(transform.rotation) * targetDirection.normalized;
                direction = Mathf.Atan2(velocity.x, velocity.z) * 180.0f / 3.14159f;

                // activate quickTurn180 based on the directional angle
                var quickTurn180Conditions =  !crouch && direction >= 165 && !jump && onGround
                                           || !crouch && direction <= -165 && !jump && onGround;
                if (quickTurn180Conditions)                                
                    quickTurn180 = true;                                

                // apply free directional rotation while not turning180 animations
                if ((!quickTurn180 && !isJumping) || (isJumping && actionsController.Jump.jumpAirControl))
                {
                    Vector3 lookDirection = targetDirection.normalized;
                    freeRotation = Quaternion.LookRotation(lookDirection, transform.up);
                    var euler = new Vector3(0, freeRotation.eulerAngles.y, 0);
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(euler), rotationSpeed * Time.fixedDeltaTime);
                }
                if (!keepDirection)
                    oldInput = input;
                if (Vector2.Distance(oldInput, input) > 0.9f && keepDirection)
                    keepDirection = false;
            }
        }

        /// ACTIVATE RAGDOLL - check your verticalVelocity and assign a value on the variable RagdollVel
        void CheckRagdoll(){
            if (ragdollVel == 0) return;

		    if(verticalVelocity <= ragdollVel && groundDistance <= 0.1f)
                transform.SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);
        }

        /// CAPSULE COLLIDER HEIGHT CONTROL - controls height, position and radius of CapsuleCollider
        void ControlHeight(){
		    if (crouch || roll){
                _capsuleCollider.center = colliderCenter / 1.4f;
                _capsuleCollider.height = colliderHeight / 1.4f;
		    }		    
            else if (usingLadder)
                _capsuleCollider.radius = colliderRadius / 1.25f;
            else{
			    // back to the original values
			    _capsuleCollider.center = colliderCenter;
			    _capsuleCollider.radius = colliderRadius;
			    _capsuleCollider.height = colliderHeight;
		    }
	    }

        /// GROUND CHECKER - check if the character is grounded or not
        void CheckGround(){
		    CheckGroundDistance();            

            // change the physics material to very slip when not grounded
            _capsuleCollider.material = (onGround && GroundAngle() < slopeLimit) ? frictionPhysics : slippyPhysics;				
		    // we don't want to stick the character grounded if one of these bools is true
		    bool groundStickConditions = !jumpOver && !stepUp && !climbUp && !usingLadder && !hitReaction;

		    if (groundStickConditions)
		    {
			    var onStep = StepOffset();

			    if (groundDistance <= 0.05f)
			    {                   
                    onGround = true;
				    // keeps the character grounded and prevents bounceness on ramps
				    //if (!onStep) _rigidbody.velocity = Vector3.ProjectOnPlane(_rigidbody.velocity, groundHit.normal);
                    Sliding();
                }
			    else
			    {
				    if (groundDistance >= groundCheckDistance)
				    {
					    onGround = false;
					    // check vertical velocity
					    verticalVelocity = _rigidbody.velocity.y;
                        // apply extra gravity when falling
                        if (!onStep && !roll)
                            transform.position -= Vector3.up * (extraGravity * Time.deltaTime);
				    }
				    else if (!onStep && !roll && !jump)
                        transform.position -= Vector3.up * (extraGravity * Time.deltaTime);
			    }
		    }
	    }
        
        void Sliding(){
            var onStep = StepOffset();
            var groundAngleTwo = 0f;
            RaycastHit hitinfo;
            Ray ray = new Ray(transform.position, -transform.up);

            if (Physics.Raycast(ray, out hitinfo, Mathf.Infinity, groundLayer))            
                groundAngleTwo = Vector3.Angle(Vector3.up, hitinfo.normal);            

            if (GroundAngle() > slopeLimit + 1f && GroundAngle() <= 85 &&
                groundAngleTwo > slopeLimit + 1f && groundAngleTwo <= 85 && 
                groundDistance <= 0.05f && !onStep)
            {                                                
                sliding = true;
                onGround = false;
                var slideVelocity = (GroundAngle() - slopeLimit) * 5f;
                slideVelocity = Mathf.Clamp(slideVelocity, 0, 10);
                _rigidbody.velocity = new Vector3(_rigidbody.velocity.x, -slideVelocity, _rigidbody.velocity.z);               
            }
            else{
                sliding = false;
                onGround = true;
            }
        }

        /// GROUND DISTANCE	- get the distance between the middle of the character to the ground
        void CheckGroundDistance(){
            if (_capsuleCollider != null){
                float radius = _capsuleCollider.radius * 0.9f;// radius of the SphereCast
                var dist = Mathf.Infinity;
                // position of the SphereCast origin starting at the base of the capsule
                Vector3 pos = transform.position + Vector3.up * (_capsuleCollider.radius);
                Ray ray1 = new Ray(transform.position + new Vector3(0, colliderHeight / 2, 0), Vector3.down);// ray for RayCast
                Ray ray2 = new Ray(pos, -Vector3.up);// ray for SphereCast
                // raycast for check the ground distance
                if (Physics.Raycast(ray1, out groundHit, Mathf.Infinity, groundLayer))
                    dist = transform.position.y - groundHit.point.y;

                // sphere cast around the base of the capsule to check the ground distance
                if (Physics.SphereCast(ray2, radius, out groundHit, Mathf.Infinity, groundLayer)){
                    // check if sphereCast distance is small than the ray cast distance
                    if (dist > (groundHit.distance - _capsuleCollider.radius * 0.1f))
                        dist = (groundHit.distance - _capsuleCollider.radius * 0.1f);
                }
                groundDistance = dist;
            }
        }

        /// STEP OFFSET LIMIT - check the height of the object ahead, control by stepOffSet
        bool StepOffset(){
		    if(input.sqrMagnitude < 0.1 || !onGround) return false;
		
		    var hit = new RaycastHit();
            Ray rayStep = new Ray((transform.position + new Vector3(0, stepOffsetEnd, 0) + transform.forward * ((_capsuleCollider).radius + 0.05f)), Vector3.down);
            
            if (Physics.Raycast(rayStep, out hit, stepOffsetEnd - stepOffsetStart, groundLayer)){
                if (!stopMove && hit.point.y >= (transform.position.y) && hit.point.y <= (transform.position.y + stepOffsetEnd)){
                    var heightPoint = new Vector3(transform.position.x, hit.point.y + 0.1f, transform.position.z);
                    transform.position = Vector3.Lerp(transform.position, heightPoint, (speed * stepSmooth) * Time.fixedDeltaTime);
                    //var heightPoint = new Vector3(_rigidbody.velocity.x, hit.point.y + 0.1f, _rigidbody.velocity.z);
                    //_rigidbody.velocity = heightPoint * 10f * Time.fixedDeltaTime;
                    return true;
                }
            }			    
		    return false;
	    }

        /// CHECK GROUND ANGLE 
        float GroundAngle(){            
            var groundAngle = Vector3.Angle(groundHit.normal, Vector3.up);
            return groundAngle;            
        }

        /// STOP MOVE - stop the character if hits a wall and apply slope limit to ramps
        public void StopMove(){
            if (input.sqrMagnitude < 0.1 || !onGround) return;

            RaycastHit hitinfo;
            Ray ray = new Ray(transform.position + new Vector3(0, colliderHeight / 3, 0), transform.forward);

            if (Physics.Raycast(ray, out hitinfo, _capsuleCollider.radius + stopMoveDistance, stopMoveLayer) && !usingLadder){
                var hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);

                if (hitinfo.distance <= stopMoveDistance && hitAngle > 85)
                    stopMove = true;
                else if (hitAngle >= slopeLimit + 1f && hitAngle <= 85)
                    stopMove = true;
            }
            else if (Physics.Raycast(ray, out hitinfo, 1f, groundLayer) && !usingLadder){
                var hitAngle = Vector3.Angle(Vector3.up, hitinfo.normal);
                if (hitAngle >= slopeLimit + 1f && hitAngle <= 85)
                    stopMove = true;
            }
            else
                stopMove = false;
        }

        /// AUTO CROUCH - keep it crouched while you have something above
        public void CheckAutoCrouch(){
            float radius = _capsuleCollider.radius * 0.9f;// radius of SphereCast
            Vector3 pos = transform.position + Vector3.up * ((colliderHeight * 0.5f) - colliderRadius);// Position of SphereCast origin stating in base of capsule
            Ray ray2 = new Ray(pos, Vector3.up);// ray for SphereCast

            // sphere cast around the base of capsule for check ground distance
            if (Physics.SphereCast(ray2, radius, out groundHit, headDetect - (colliderRadius * 0.1f), autoCrouchLayer)){
                autoCrouch = true;
                crouch = true;
            }
            else
                autoCrouch = false;
        }

        /// ACTIONS - raycast to check if there is anything interactable ahead
        public GameObject CheckActionObject(){
            bool checkConditions = onGround && !landHigh && !actions && !inAttack;
            GameObject _object = null;
                        
            if (checkConditions){
                RaycastHit hitInfoAction;
                Vector3 yOffSet = new Vector3(0f, -0.5f, 0f);
                Vector3 fwd = transform.TransformDirection(Vector3.forward);
                
                if(Physics.Raycast(transform.position - yOffSet, fwd, out hitInfoAction, distanceOfRayActionTrigger))                  
                    _object = hitInfoAction.transform.gameObject;
            }
            return currentCollectable != null ? currentCollectable : _object;
        }

        public float distanceOfRayActionTrigger{
            get{
                if (_capsuleCollider == null) return 0f;
                var dist = _capsuleCollider.radius + 0.1f;
                return dist;
            }
        }

        /// ROTATE WITH CAMERA - rotate the character with the camera
        public virtual void RotateWithCamera(){
            if (strafing && !actions && !lockPlayer && input != Vector2.zero){
                // smooth align character with aim position
                if (tpCamera != null && tpCamera.lockTarget){
                    Quaternion rot = Quaternion.LookRotation(tpCamera.lockTarget.position - transform.position);
                    Quaternion newPos = Quaternion.Euler(transform.eulerAngles.x, rot.eulerAngles.y, transform.eulerAngles.z);
                    transform.rotation = Quaternion.Slerp(transform.rotation, newPos, 20f * Time.fixedDeltaTime);
                }
                else{
                    Quaternion newPos = Quaternion.Euler(transform.eulerAngles.x, cameraTransform.eulerAngles.y, transform.eulerAngles.z);
                    transform.rotation = Quaternion.Slerp(transform.rotation, newPos, 20f * Time.fixedDeltaTime);
                }
            }
        }

        /// Find the correct direction based on the camera direction
        public Vector3 targetDirection{
            get{
                Vector3 refDir = Vector3.zero;
                cameraForward = keepDirection ? cameraForward : cameraTransform.TransformDirection(Vector3.forward);
                cameraForward.y = 0;

                if (tpCamera == null || !tpCamera.currentState.cameraMode.Equals(TPCameraMode.FixedAngle) || !rotateByWorld)
                {
                    //cameraForward = tpCamera.transform.TransformDirection(Vector3.forward);
                    cameraForward = keepDirection ? cameraForward : cameraTransform.TransformDirection(Vector3.forward);
                    cameraForward.y = 0; //set to 0 because of camera rotation on the X axis
                    
                    cameraRight = keepDirection ? cameraRight : cameraTransform.TransformDirection(Vector3.right);// get the right-facing direction of the camera
                    refDir = input.x * cameraRight + input.y * cameraForward;// determine the direction the player will face based on input and the camera's right and forward directions
                }
                else
                    refDir = new Vector3(input.x, 0, input.y);
                return refDir;
            }
        }

        /// DEBUG MODE - display information about the controller in real time
        public void DebugMode(){
		    Time.timeScale = timeScale;
		
		    if (hud == null)
			    return;
		
		    if (hud.debugPanel != null)
		    {
			    if (debugWindow)
			    {
				    hud.debugPanel.SetActive(true);
				    float delta = Time.smoothDeltaTime;
				    float fps = 1 / delta;
				
				    hud.debugPanel.GetComponentInChildren<Text>().text =
					    "FPS " + fps.ToString("#,##0 fps") + "\n" +
					    "CameraState = " + tpCamera.currentStateName.ToString() + "\n" +
					    "Input = " + Mathf.Clamp(input.sqrMagnitude, 0, 1f).ToString("0.0") + "\n" +
					    "Speed = " + speed.ToString("0.0") + "\n" +
					    "Direction = " + direction.ToString("0.0") + "\n" +
					    "VerticalVelocity = " + verticalVelocity.ToString("0.00") + "\n" +
					    "GroundDistance = " + groundDistance.ToString("0.00") + "\n" +
                        "GroundAngle = " + GroundAngle().ToString("0.00") + "\n" +
                        "\n" + "--- Movement Bools ---" + "\n" +
                        "MoveSet_ID = " + moveSet_ID.ToString() + "\n" +
                        "LockPlayer = " + lockPlayer.ToString() + "\n" +
					    "StopMove = " + stopMove.ToString() + "\n" +
                        "Ragdoll = " + ragdolled.ToString() + "\n" +
                        "onGround = " + onGround.ToString() + "\n" +
					    "canSprint = " + canSprint.ToString() + "\n" +
					    "crouch = " + crouch.ToString() + "\n" +
					    "strafe = " + strafing.ToString() + "\n" +
					    "turn180 = " + quickTurn180.ToString() + "\n" +
					    "quickStop = " + quickStop.ToString() + "\n" +
					    "landHigh = " + landHigh.ToString() + "\n" +
                        "jump = " + jump.ToString() + "\n" +
                        "\n" + "--- Actions Bools ---" + "\n" +
					    "roll = " + roll.ToString() + "\n" +					
					    "stepUp = " + stepUp.ToString() + "\n" +
					    "jumpOver = " + jumpOver.ToString() + "\n" +
					    "climbUp = " + climbUp.ToString() + "\n" +                        
                        "usingLadder = " + usingLadder.ToString() + "\n" +
                        "blocking = " + blocking.ToString() + "\n" +
                        "InAttack = " + inAttack.ToString() + "\n";

                }
			    else
				    hud.debugPanel.SetActive(false);
		    }
	    }
    }
}