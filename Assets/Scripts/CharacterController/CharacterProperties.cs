using UnityEngine;

namespace Invector{    
    [System.Serializable]
    public class LocomotionSetup{
        public enum LocomotionType{
            FreeWithStrafe, OnlyStrafe, OnlyFree
        }

        [Header("--- Locomotion Setup ---")]

        public LocomotionType locomotionType = LocomotionType.FreeWithStrafe;

        [Tooltip("The character Head will follow where you look at, UNCHECK if you are using TopDown or 2.5D")]
        public bool headTracking = true;

        [Tooltip("Press the button to crouch or just hit the button once - leave this bool UNCHECK if you are using MOBILE.")]
        public bool pressToCrouch = true;

        [Tooltip("Check to control the character while jumping")]
        public bool jumpAirControl = true;

        [Tooltip("Add Extra jump speed, based on your speed input the character will move forward")]
        public float jumpForward = 4f;

        [Tooltip("Add Extra jump height, if you want to jump only with Root Motion leave the value with 0.")]
        public float jumpForce = 4f;

        [Tooltip("Speed of the rotation on free directional movement")]
        public float rotationSpeed = 8f;

        [Tooltip("Use this to rotate the character using the World axis, or false to use the camera axis - CHECK for Isometric Camera")]
        public bool rotateByWorld = false;

        [Tooltip("Add extra speed for the locomotion movement, keep this value at 0 if you want to use only root motion speed.")]
        public float extraMoveSpeed = 0f;

        [Tooltip("Add extra speed for the strafe movement, keep this value at 0 if you want to use only root motion speed.")]
        public float extraStrafeSpeed = 0f;

        [Tooltip("ADJUST IN PLAY MODE - GREEN Raycast until you see it above the head, this will make the character Auto-Crouch if something hit the raycast.")]
        public float headDetect = 0.96f;

        [Tooltip("How much the character will curve the spine while Aiming")]
        public float spineCurvature = 0.7f;

        [Tooltip("Put your Random Idle animations at the AnimatorController and select a value to randomize, 0 is disable.")]
        public float randomIdleTime = 0f;

        [Tooltip("Change the MoveSet animation.")]
        [HideInInspector] public float moveSet;

        // general bools of movement
        [HideInInspector]
        public bool
            onGround, stopMove, autoCrouch,
            quickStop, quickTurn180, canSprint,
            crouch, strafing, landHigh,
            jump, isJumping;

        // actions bools, used to turn on/off actions animations *check ThirdPersonAnimator*	
        [HideInInspector]
        public bool
            jumpOver,
            stepUp,
            climbUp,
            rolling,
            enterLadderBottom,
            enterLadderTop,
            usingLadder,
            exitLadderBottom,
            exitLadderTop,
            inAttack,
            hitReaction,
            hitRecoil;

        // one bool to rule then all
        public bool actions{
            get{
                return jumpOver || stepUp || climbUp || rolling || usingLadder || quickStop || quickTurn180 || jump || hitReaction || hitRecoil;
            }
        }
    }

    [System.Serializable]
    public class EnergySetup
    {
        [Header("--- Health & Stamina ---")]
        public bool isDead;
        public float startHealth = 100f;
        public float startingHealth { get { return startHealth; } }
        public float currentHealth { get; set; }
        public float startingStamina = 100f;
        public float staminaRecovery = 1f;
        [HideInInspector]
        public float recoveryDelay;
        [HideInInspector]
        public bool recoveringStamina;
        [HideInInspector]
        public bool canRecovery;
        [HideInInspector]
        public float currentStamina;
    }

    [System.Serializable]
    public class GroundedSetup
    {
        [Header("--- Grounded Setup ---")]
        [Tooltip("Distance to became not grounded")]
        public float groundCheckDistance = 0.5f;
        [HideInInspector]
        public float groundDistance;

        [Tooltip("ADJUST IN PLAY MODE - Offset height limit for sters - GREY Raycast in front of the legs")]
        public float stepOffsetEnd = 0.36f;
        [Tooltip("ADJUST IN PLAY MODE - Offset height origin for sters, make sure to keep slight above the floor - GREY Raycast in front of the legs")]
        public float stepOffsetStart = 0.05f;
        [Tooltip("ADJUST IN PLAY MODE - Offset forward to detect sters - GREY Raycast in front of the legs")]
        public float stepOffsetFwd = 0.05f;

        [Tooltip("Max angle to walk")]
        public float slopeLimit = 45f;

        [Tooltip("Apply extra gravity when the character is not grounded")]
        public float extraGravity = 4f;

        [Tooltip("Select a VerticalVelocity to turn on Land High animation")]
        public float landHighVel = -5f;

        [Tooltip("Select a VerticalVelocity to turn on the Ragdoll")]
        public float ragdollVel = -8f;

        [Tooltip("Prevent the character of walking in place when hit a wall")]
        public float stopMoveDistance = 0.5f;

        [Tooltip("Choose the layers the your character will stop moving when hit with the BLUE Raycast")]
        public LayerMask stopMoveLayer;
        public LayerMask groundLayer;
        public RaycastHit groundHit;

        [Header("--- Debug Info ---")]
        public bool debugWindow;
        [Range(0f, 1f)]
        public float timeScale = 1f;
    } 
}