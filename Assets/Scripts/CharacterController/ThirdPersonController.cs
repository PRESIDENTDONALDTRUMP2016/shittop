using UnityEngine;
using System.Collections;

namespace Invector.CharacterController{
    public class ThirdPersonController : ThirdPersonAnimator{
        private static ThirdPersonController _instance;
        public static ThirdPersonController instance{
            get{
                if (_instance == null) {
                    _instance = GameObject.FindObjectOfType<ThirdPersonController>();
                    //Tell unity not to destroy this object when loading a new scene
                    //DontDestroyOnLoad(_instance.gameObject);
                }
                return _instance;
            }
        }

        private bool isAxisInUse;

        void Awake(){
            StartCoroutine("UpdateRaycast");// limit raycasts calls for better performance            
        }

        void Start(){
            InitialSetup();					// setup the basic information, created on Character.cs	
        }

        void FixedUpdate(){
		    UpdateMotor();					// call ThirdPersonMotor methods
		    UpdateAnimator();				// update animations on the Animator and their methods
		    UpdateHUD();                    // update HUD elements like health bar, texts, etc
            ControlCameraState();			// change CameraStates            
        }

        void LateUpdate(){            
            InputHandle();					// handle input from controller, keyboard&mouse or mobile touch             
		    DebugMode();					// display information about the character on PlayMode
        }

        /// INPUT - every input is been handle in this script, you can change the input map here or directly on the InputManager
        void InputHandle() {
            CameraInput();
            if (!lockPlayer && !ragdolled) {                
                ControllerInput();

                // we have mapped the 360 controller as our Default gamepad, 
                // you can change the keyboard inputs by chaging the Alternative Button on the InputManager.                
                InteractInput();
                JumpInput();
                RollInput();                
                CrouchInput();                
                AttackInput();                
                DefenseInput();                
                SprintInput();
                LockOnInput();
                DropWeaponInput("D-Pad Horizontal");
            }
            else            
                LockPlayer();            
        }

        /// Conditions to lock the Controller Input's and reset some variables.
        void LockPlayer(){            
            input = Vector2.zero;
            speed = 0f;
            canSprint = false;
            if (hud != null) hud.DisableSprite();
        }

        /// UPDATE RAYCASTS - handles a separate update for better performance
        public IEnumerator UpdateRaycast(){
		    while (true){
			    yield return new WaitForEndOfFrame();
			
			    CheckAutoCrouch();			    
			    StopMove();
            }
	    }

        /// CAMERA STATE - you can change de CameraState here, the bool means if you want lerp of not, make sure to use the same CameraState String that you named on TPCameraListData
        void ControlCameraState(){
            if (tpCamera == null)
                return;

            if (changeCameraState && !strafing)
                tpCamera.ChangeState(customCameraState, customlookAtPoint, smoothCameraState);
            else if (crouch)            
                tpCamera.ChangeState("Crouch", true);
            else if (strafing)
                tpCamera.ChangeState("Strafing", true);
            else
                tpCamera.ChangeState("Default", true);
        }

        /// CONTROLLER INPUT - gets input from keyboard, gamepad or mobile touch to move the character
        void ControllerInput(){
            if (inputType == InputType.MouseKeyboard)
                input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            else if (inputType == InputType.Controller){
                float deadzone = 0.25f;
                input = new Vector2(Input.GetAxis("LeftAnalogHorizontal"), Input.GetAxis("LeftAnalogVertical"));
                if (input.magnitude < deadzone)
                    input = Vector2.zero;
                else
                    input = input.normalized * ((input.magnitude - deadzone) / (1 - deadzone));
            }
        }

        void CameraInput() {
            if (tpCamera == null)
                return;
                     
            if (inputType == InputType.MouseKeyboard) {
                tpCamera.RotateCamera(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                tpCamera.Zoom(Input.GetAxis("Mouse ScrollWheel"));
            }
            else if (inputType == InputType.Controller)            
                tpCamera.RotateCamera(Input.GetAxis("RightAnalogHorizontal"), Input.GetAxis("RightAnalogVertical"));            
            
            RotateWithCamera();     // rotate the character with the camera while strafing
        }

        void SprintInput() {
            if (!actionsController.Sprint.use) return;
            var _input = actionsController.Sprint.input.ToString();

            if (inputType == InputType.MouseKeyboard){
                if (Input.GetButtonDown(_input) && currentStamina > 0 && input.sqrMagnitude > 0.1f){
                    strafing = false;
                    if (onGround && !strafing && !crouch)
                        canSprint = !canSprint;
                }
                else if (currentStamina <= 0 || input.sqrMagnitude < 0.1f || strafing)
                    canSprint = false;
            }

		    if (canSprint){
                recoveryDelay = actionsController.Sprint.recoveryDelay;
                ReduceStamina(actionsController.Sprint.staminaCost);
		    }
        }

        void CrouchInput(){
            if (!actionsController.Crouch.use) return;
            var _input = actionsController.Crouch.input.ToString();

            if (autoCrouch)
                crouch = true;
            else if (actionsController.Crouch.pressToCrouch){
                if (inputType == InputType.MouseKeyboard)
                    crouch = Input.GetButton(_input) && onGround && !actions;			
            }
            else{
                if (inputType == InputType.MouseKeyboard)
                {
                    if (Input.GetButtonDown(_input) && onGround && !actions)
                        crouch = !crouch;
                }			
            }
        }

        void JumpInput(){
            if (!actionsController.Jump.use) return;
            var _input = actionsController.Jump.input.ToString();

            bool staminaConditions = currentStamina > actionsController.Jump.staminaCost;
            bool jumpConditions = !crouch && onGround && !actions && staminaConditions && !strafing;

            if (inputType == InputType.MouseKeyboard){
                if (Input.GetButtonDown(_input) && jumpConditions){
                    jump = true;
                    ReduceStamina(actionsController.Jump.staminaCost);
                    recoveryDelay = actionsController.Jump.recoveryDelay;
                }
            }
        }

        /// AIMING INPUT
        void LockOnInput(){
            if (!actionsController.LockOn.use) return;
            var _input = actionsController.LockOn.input.ToString();

            if (!locomotionType.Equals(LocomotionType.OnlyFree)){
                if (inputType == InputType.MouseKeyboard){
                    if (Input.GetButtonDown(_input) && !actions){
                        animator.SetFloat("Direction", 0f);
                        strafing = !strafing;
                        tpCamera.gameObject.SendMessage("UpdateLockOn", strafing, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }

            if(tpCamera.lockTarget){
                // Switch between targets using Keyboard
                if (inputType == InputType.MouseKeyboard)
                {                                       
                    if (Input.GetKey(KeyCode.X))
                        tpCamera.gameObject.SendMessage("ChangeTarget", 1, SendMessageOptions.DontRequireReceiver);
                    else if (Input.GetKey(KeyCode.Z))
                        tpCamera.gameObject.SendMessage("ChangeTarget", -1, SendMessageOptions.DontRequireReceiver);
                }
                // Switch between targets using GamePad
                else if (inputType == InputType.Controller){
                    var value = Input.GetAxisRaw("RightAnalogHorizontal");
                    if (value == 1)                    
                        tpCamera.gameObject.SendMessage("ChangeTarget", 1, SendMessageOptions.DontRequireReceiver);                                      
                    else if(value == -1f)                                           
                        tpCamera.gameObject.SendMessage("ChangeTarget", -1, SendMessageOptions.DontRequireReceiver);                                           
                }
            }
        }

        /// ATTACK INPUT - trigger a attack animation if the character are equipped with a attack weapon   
        void AttackInput(){
            if (!actionsController.Attack.use) return;
            if (meleeManager == null) return;

            var _input = actionsController.Attack.input.ToString();

            // attack conditions
            bool weaponStaminaConditions = meleeManager.currentMeleeWeapon != null && currentStamina > meleeManager.currentMeleeWeapon.staminaCost;
            bool attackConditions = (!actions || roll) && onGround && weaponStaminaConditions && meleeManager.currentMeleeWeapon != null;
            
            // attack input
            if (inputType == InputType.MouseKeyboard){
                if (Input.GetButtonDown(_input) && attackConditions)
                    animator.SetTrigger("MeleeAttack");
            }
        }

        /// ATTACK INPUT - trigger a defense animation if the character are equipped with a defense weapon 
        void DefenseInput(){
            if (!actionsController.Defense.use) return;
            if (meleeManager == null) return;

            var _input = actionsController.Defense.input.ToString();

            // defense contitions    
            bool shieldStaminaConditions = meleeManager.currentMeleeShield != null;
            bool defenseConditions = (!actions || roll || hitRecoil) && onGround && shieldStaminaConditions && meleeManager.currentMeleeShield != null && !inAttack;

            // defense input
            if (inputType == InputType.MouseKeyboard){                
                if (Input.GetButton(_input) && defenseConditions)
                    blocking = true;
                else
                    blocking = false;
            }
        }

        /// DropWeapon - drops the current weapon
        void DropWeaponInput(string _input){            
            if (Input.GetAxisRaw(_input) > 0.1f){
                if (isAxisInUse == false){
                    if(meleeManager != null && meleeManager.currentMeleeWeapon != null)                    
                        transform.root.SendMessage("DropWeapon", SendMessageOptions.DontRequireReceiver);                    
                    
                    isAxisInUse = true;
                }                
            }
            else if (Input.GetAxisRaw(_input) < -0.1f){
                if (isAxisInUse == false){
                    if (meleeManager != null && meleeManager.currentMeleeShield != null)
                        transform.root.SendMessage("DropShield", SendMessageOptions.DontRequireReceiver);
                    isAxisInUse = true;
                }
            }

            if (Input.GetAxisRaw(_input) == 0)
                isAxisInUse = false;
        }

        void RollInput(){
            if (!actionsController.Roll.use) return;
            var _input = actionsController.Roll.input.ToString();

            if (inputType == InputType.MouseKeyboard){
                if (Input.GetButtonDown(_input))
                    Rolling();
            }           
        }

        /// ACTIONS - WHITE raycast to check if there is anything interactable ahead
        void InteractInput(){
            if (!actionsController.Interact.use) return;            
            var _input = actionsController.Interact.input.ToString();

            var hitObject = CheckActionObject();
            if (hitObject != null){
                try{
                    if (hitObject.CompareTag("ClimbUp"))
                        DoAction(hitObject, ref climbUp, _input);
                    else if (hitObject.CompareTag("StepUp"))
                        DoAction(hitObject, ref stepUp, _input);
                    else if (hitObject.CompareTag("JumpOver"))
                        DoAction(hitObject, ref jumpOver, _input);
                    else if (hitObject.CompareTag("AutoCrouch"))
                        autoCrouch = true;
                    else if (hitObject.CompareTag("EnterLadderBottom") && !jump)
                        DoAction(hitObject, ref enterLadderBottom, _input);
                    else if (hitObject.CompareTag("EnterLadderTop") && !jump)
                        DoAction(hitObject, ref enterLadderTop, _input);
                    else if (hitObject.CompareTag("Weapon"))
                        PickUpEquipmentInput(hitObject, _input);
                }
                catch (UnityException e){
                    Debug.LogWarning(e.Message);
                }
            }
            else if (hud != null){
                if (hud.showInteractiveText && !currentCollectable)
                    hud.DisableSprite();
            }
        }

        /// DO ACTION - execute a action when press the action button, use with TriggerAction script
        /// <param name="hitObject">Gameobject with the component TriggerAction</param>
        void DoAction(GameObject hitObject, ref bool action, string _input){
            var triggerAction = hitObject.transform.GetComponent<TriggerAction>();
            if (!triggerAction)
            {
                Debug.LogWarning("Missing TriggerAction Component on " + hitObject.transform.name + "Object");
                return;
            }
            if (hud != null && !triggerAction.autoAction) hud.EnableSprite(triggerAction.message);

            if (inputType == InputType.MouseKeyboard)
            {
                if (Input.GetButtonDown(_input) && !actions || triggerAction.autoAction && !actions)
                {
                    action = true;// turn the action bool true and call the animation
                    if (hud != null) hud.DisableSprite();// disable the text and sprite 
                    matchTarget = triggerAction.target;// find the cursorObject height to match with the character animation
                    // align the character rotation with the object rotation
                    var rot = hitObject.transform.rotation;
                    transform.rotation = rot;
                }
            }
        }

        /// The method CheckActionObject() will return a gameobject type of CollectableItem if you stay inside the Trigger area
        void PickUpEquipmentInput(GameObject collectableItem, string _input){
            if (inputType == InputType.MouseKeyboard){
                if (Input.GetButtonDown(_input) && !actions)
                    PickUpEquip(collectableItem);
            }                
        }

        void PickUpEquip(GameObject collectableItem){
            Debug.Log("PickUpEquip()");
            var collectable = collectableItem.GetComponent<CollectableMelee>();

            if (collectable._meleeItem.GetType().Equals(typeof(MeleeWeapon))) {
                SendMessage("SetWeaponHandler", collectable, SendMessageOptions.DontRequireReceiver);
                Debug.Log("MeleeWeapon");
            }

            else if (collectable._meleeItem.GetType().Equals(typeof(MeleeShield)))
                SendMessage("SetShieldHandler", collectable, SendMessageOptions.DontRequireReceiver);

            if (hud != null)
                hud.DisableSprite();

            currentCollectable = null;
        }

        void OnTriggerStay(Collider other){
            try{
                // if you enter a trigger area of a CollectableItem, there is a verification to know if it's a weapon or a shield
                if(other.gameObject.CompareTag("Weapon")){
                    var collectable = other.gameObject.GetComponent<CollectableMelee>();
                    if (collectable != null){
                        currentCollectable = collectable.gameObject;
                        if (collectable._meleeItem.GetType().Equals(typeof(MeleeWeapon))){
                            var _meleeWeapon = collectable.gameObject.GetComponent<MeleeWeapon>();
                            if(_meleeWeapon == null)                            
                                _meleeWeapon = collectable.gameObject.GetComponentInChildren<MeleeWeapon>();
                            
                            if (hud != null) hud.EnableSprite(collectable.message + " (" + _meleeWeapon.damage.value + " ATK)");
                        }
                        else if (collectable._meleeItem.GetType().Equals(typeof(MeleeShield))){
                            var _meleeShield = collectable.gameObject.GetComponent<MeleeShield>();
                            if (_meleeShield == null)
                                _meleeShield = collectable.gameObject.GetComponentInChildren<MeleeShield>();
                            if (hud != null) hud.EnableSprite(collectable.message + " (" + _meleeShield.defenseRate + " DEF)");
                        }
                    }
                }                
                // if you are using the ladder and reach the exit from the bottom
                if (other.gameObject.CompareTag("ExitLadderBottom") && usingLadder){
                    if (inputType == InputType.MouseKeyboard){
                        if (Input.GetButtonDown("B") || speed <= -0.05f && !enterLadderBottom)
                            exitLadderBottom = true;
                    }
                }
                // if you are using the ladder and reach the exit from the top
                if (other.gameObject.CompareTag("ExitLadderTop") && usingLadder && !enterLadderTop){
                    if (speed >= 0.05f)
                        exitLadderTop = true;
                }
            }
            catch (UnityException e){
                Debug.LogWarning(e.Message);
            }
        }

        void OnTriggerExit(Collider other){
            // reset the currentCollectable to null if you exit the trigger area 
            if (other.gameObject.CompareTag("Weapon")){
                var collectable = other.gameObject.GetComponent<CollectableMelee>();
                if (collectable != null)
                    currentCollectable = null;
            }
        }
    }    
}