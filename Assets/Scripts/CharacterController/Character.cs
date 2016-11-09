using UnityEngine;
using System.Collections;

namespace Invector{
    [System.Serializable]
    public abstract class Character : MonoBehaviour, ICharacter{
        #region Character Variables
        [HideInInspector]   public TPCamera tpCamera;// acess camera info
        [HideInInspector]   public HUDController hud;// acess hud controller 
        [HideInInspector]   public Animator animator;// get the animator component of character
        [HideInInspector]   public MeleeEquipmentManager meleeManager;// acess melee weapon information
        [HideInInspector]   public PhysicMaterial frictionPhysics, slippyPhysics;// physics material
        [HideInInspector]   public CapsuleCollider _capsuleCollider;// get capsule collider information
        [HideInInspector]   public float colliderRadius, colliderHeight;// storage capsule collider extra information
        [HideInInspector]   public Vector3 colliderCenter;// storage the center of the capsule collider info
        [HideInInspector]   public Rigidbody _rigidbody;// access the rigidbody component
        [HideInInspector]   public Vector2 input;// generate input for the controller
        [HideInInspector]   public bool lockPlayer;// lock all the character locomotion 
        [HideInInspector]   public float speed, direction, verticalVelocity;// general variables to the locomotion
        [HideInInspector]   public float offSetPivot;// create a offSet base on the character hips 
        [HideInInspector]   public bool ragdolled { get; set; }// know if the character is ragdolled or not

        public Transform cameraTransform{
            get{
                Transform cT = transform;
                if (Camera.main != null)
                    cT = Camera.main.transform;
                if (tpCamera)
                    cT = tpCamera.transform;
                if (cT == transform){
                    Debug.LogWarning("Invector : Missing TPCamera or MainCamera");
                    this.enabled = false;
                }
                return cT;
            }
        }

        [HideInInspector] public ActionsController actionsController;

        [Header("---! Layers !---")]        
        [Tooltip("Layers that the character can walk on")]
        public LayerMask groundLayer = 1<<0;
        [Tooltip("What objects can make the character auto crouch")]
        public LayerMask autoCrouchLayer = 1<<0;
        [Tooltip("Select the layers the your character will stop moving when close to")]
        [SerializeField]
        protected LayerMask stopMoveLayer;
        [Tooltip("BLUE Raycast - Stop the walking/run animation when hit a wall")]
        public float stopMoveDistance = 0.5f;

        [Header("--- Health & Stamina ---")]        
        [SerializeField]
        private float startHealth = 100f;
        public float healthRecovery = 0f;
        public float healthRecoveryDelay = 0f;
        public float startingHealth { get { return startHealth; }}
        public float currentHealth { get; set; }
        public float startingStamina = 100f;
        public float staminaRecovery = 1.2f;


        protected bool isDead;
        protected float recoveryDelay;
        protected bool recoveringStamina;
        protected bool canRecovery;
        protected bool canUseStamina;
        protected float currentStamina;
        protected float currentHealthRecoveryDelay;

        public enum DeathBy{
            Animation,
            AnimationWithRagdoll,
            Ragdoll
        }

        public DeathBy deathBy = DeathBy.Animation;

        #endregion

        /// INITIAL SETUP - prepare the initial information like camera, capsule collider, physics materials, etc...
        public void InitialSetup(){
            animator = GetComponent<Animator>();

            tpCamera = TPCamera.instance;
            hud = HUDController.instance;
            meleeManager = GetComponent<MeleeEquipmentManager>();
            // create a offset pivot to the character, to align camera position when transition to ragdoll
            var hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            offSetPivot = Vector3.Distance(transform.position, hips.position);

            if (tpCamera != null){
                tpCamera.offSetPlayerPivot = offSetPivot;
                tpCamera.target = transform;
            }

            if (hud == null) Debug.LogWarning("Invector : Missing HUDController, please assign on ThirdPersonController");

            // prevents the collider from slipping on ramps
            frictionPhysics = new PhysicMaterial();
            frictionPhysics.name = "frictionPhysics";
            frictionPhysics.staticFriction = 1f;
            frictionPhysics.dynamicFriction = 1f;            

            // default physics 
            slippyPhysics = new PhysicMaterial();
            slippyPhysics.name = "slippyPhysics";
            slippyPhysics.staticFriction = 0f;
            slippyPhysics.dynamicFriction = 0f;            

            _rigidbody = GetComponent<Rigidbody>();// rigidbody info
            _capsuleCollider = GetComponent<CapsuleCollider>();// capsule collider 

            // save your collider preferences 
            colliderCenter = GetComponent<CapsuleCollider>().center;
            colliderRadius = GetComponent<CapsuleCollider>().radius;
            colliderHeight = GetComponent<CapsuleCollider>().height;

            currentHealth = startingHealth;
            currentHealthRecoveryDelay = healthRecoveryDelay;
            currentStamina = startingStamina;            

            if (hud == null)
                return;

            //hud.damageImage.color = new Color(0f, 0f, 0f, 0f);

            cameraTransform.SendMessage("Init", SendMessageOptions.DontRequireReceiver);
            UpdateHUD();
        }

        /// UPDATE HUD - sycronize the stamina value with the stamina slider and show/hide the damageHUD image effect
        public void UpdateHUD(){
            if (hud == null)
                return;

            hud.healthSlider.maxValue = startingHealth;
            hud.healthSlider.value = currentHealth;
            hud.staminaSlider.maxValue = startingStamina;
            hud.staminaSlider.value = currentStamina;

            //if (hud.damaged)
            //    hud.damageImage.color = hud.flashColour;
            //else
            //    hud.damageImage.color = Color.Lerp(hud.damageImage.color, Color.clear, hud.flashSpeed * Time.deltaTime);

            //hud.damaged = false;
        }

        /// GAMEPAD VIBRATION - call this method to use vibration on the gamepad
        /// <param name="vibTime">duration of the vibration</param>
        #if !UNITY_WEBPLAYER && !UNITY_ANDROID && !UNITY_IOS && !UNITY_IPHONE
        public IEnumerator GamepadVibration(float vibTime){
            if (inputType == InputType.Controller){
                XInputDotNetPure.GamePad.SetVibration(0, 1, 1);
                yield return new WaitForSeconds(vibTime);
                XInputDotNetPure.GamePad.SetVibration(0, 0, 0);
            }
        }
        #endif

        /// INPUT TYPE - check in real time if you are using the controller ou mouse/keyboard
        [HideInInspector]
        public enum InputType{
            MouseKeyboard,
            Controller
        };
        [HideInInspector]
        public InputType inputType = InputType.MouseKeyboard;

        void OnGUI(){
            switch (inputType){
                case InputType.MouseKeyboard:
                    if (isControlerInput()){
                        inputType = InputType.Controller;
                        hud.controllerInput = true;
                        if (hud != null)
                            hud.FadeText("Control scheme changed to Controller", 2f, 0.5f);
                    }
                    break;
                case InputType.Controller:
                    if (isMouseKeyboard()){
                        inputType = InputType.MouseKeyboard;
                        hud.controllerInput = false;
                        if (hud != null)
                            hud.FadeText("Control scheme changed to Keyboard/Mouse", 2f, 0.5f);
                    }
                    break;
            }            
        }

        public InputType GetInputState() { return inputType; }

        private bool isMouseKeyboard(){
            // mouse & keyboard buttons
            if (Event.current.isKey || Event.current.isMouse)
                return true;
            // mouse movement
            if (Input.GetAxis("Mouse X") != 0.0f || Input.GetAxis("Mouse Y") != 0.0f)
                return true;
        
            return false;
        }

        private bool isControlerInput(){
            // joystick buttons
            if (Input.GetKey(KeyCode.Joystick1Button0) || Input.GetKey(KeyCode.Joystick1Button1) ||
               Input.GetKey(KeyCode.Joystick1Button2) || Input.GetKey(KeyCode.Joystick1Button3) ||
               Input.GetKey(KeyCode.Joystick1Button4) || Input.GetKey(KeyCode.Joystick1Button5) ||
               Input.GetKey(KeyCode.Joystick1Button6) || Input.GetKey(KeyCode.Joystick1Button7) ||
               Input.GetKey(KeyCode.Joystick1Button8) || Input.GetKey(KeyCode.Joystick1Button9) ||
               Input.GetKey(KeyCode.Joystick1Button10) || Input.GetKey(KeyCode.Joystick1Button11) ||
               Input.GetKey(KeyCode.Joystick1Button12) || Input.GetKey(KeyCode.Joystick1Button13) ||
               Input.GetKey(KeyCode.Joystick1Button14) || Input.GetKey(KeyCode.Joystick1Button15) ||
               Input.GetKey(KeyCode.Joystick1Button16) || Input.GetKey(KeyCode.Joystick1Button17) ||
               Input.GetKey(KeyCode.Joystick1Button18) || Input.GetKey(KeyCode.Joystick1Button19))
            {
                return true;
            }

            // joystick axis
            if (Input.GetAxis("LeftAnalogHorizontal") != 0.0f || Input.GetAxis("LeftAnalogVertical") != 0.0f || Input.GetAxis("Triggers") != 0.0f || Input.GetAxis("RightAnalogHorizontal") != 0.0f || Input.GetAxis("RightAnalogVertical") != 0.0f)
                return true;
            return false;
        }

        public void ResetRagdoll(){
            tpCamera.offSetPlayerPivot = offSetPivot;
            tpCamera.SetTarget(this.transform);            
            lockPlayer = false;
            verticalVelocity = 0f;
            ragdolled = false;
        }

        public void RagdollGettingUp(){
            _rigidbody.useGravity = true;
            _rigidbody.isKinematic = false;
            _capsuleCollider.enabled = true;
        }

        public void EnableRagdoll(){
            tpCamera.offSetPlayerPivot = 0f;
            tpCamera.SetTarget(animator.GetBoneTransform(HumanBodyBones.Hips));
            ragdolled = true;
            _capsuleCollider.enabled = false;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
            lockPlayer = true;
        }
    }
}