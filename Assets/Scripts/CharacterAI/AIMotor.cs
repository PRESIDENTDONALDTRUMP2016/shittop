using UnityEngine;
using System.Collections;

namespace Invector
{
    public class AIMotor : MonoBehaviour, ICharacter
    {
        #region public variables        
        [Header("--- Locomotion ---")]
        [Tooltip("Layer of the Ground")]
        public LayerMask groundLayer = 1 << 0;
        public LayerMask autoCrouchLayer = 1 << 0;
        public AIStates currentState = AIStates.PatrolPoints;
        public enum DeathBy
        {
            Animation,
            AnimationWithRagdoll,
            Ragdoll
        }

        public DeathBy deathBy = DeathBy.Animation;

        public float startHealth = 100;
        [Tooltip("Time to recovery the health")]
        public float healthRecovery = 0f;
        [Tooltip("Time to wait before the health starts recovery")]
        public float healthRecoveryDelay = 0f;
        protected float currentHealthRecoveryDelay;
        public float startingHealth { get { return startHealth; } }
        public float currentHealth { get; set; }
        [HideInInspector]
        public bool isDead;

        [Tooltip("Use to limit your locomotion animation, if you want to patrol walking set this value to 0.5f")]
        [Range(0f, 1.5f)]
        public float patrolSpeed = 0.5f;
        [Tooltip("Use to limit your locomotion animation, if you want to chase the target walking set this value to 0.5f")]
        [Range(0f, 1.5f)]
        public float chaseSpeed = 1f;
        [Tooltip("Use to limit your locomotion animation, if you want to strafe the target walking set this value to 0.5f")]
        [Range(0f, 1.5f)]
        public float strafeSpeed = 1f;
        [Range(0f, 180f)]
        public float fieldOfView = 95f;
        [Tooltip("Max Distance to detect the Player with FOV")]
        public float maxDetectDistance = 10f;
        [Tooltip("Min Distance to noticed the Player without FOV")]
        public float minDetectDistance = 4f;
        [Tooltip("Distance to stop when chasing the Player")]
        public float chaseStopDistance = 1f;
        [Tooltip("WHITE SphereCast - make sure to put the sphere right below the characters head, and check your AutoCrouchLayer")]
        public float headDetect = 0.96f;
        [Tooltip("WHITE Raycast - Distance to check TriggerActions, make sure that this raycast reach the Trigger to perform an Action")]
        public float checkTriggerDistance = 0.8f;
        public bool drawAgentPath = false;
        public bool displayGizmos;

        [Header("--- Strafe ---")]
        [Tooltip("Strafe around the target")]
        public bool strafeSideways;
        [Tooltip("Strafe a few steps backwards")]
        public bool strafeBackward;
        [Tooltip("Min time to change the strafe direction")]
        public float minStrafeSwape = 2f;
        [Tooltip("Max time to change the strafe direction")]
        public float maxStrafeSwape = 5f;
        [Tooltip("Distance to switch to the strafe locomotion, leave with 0 if you don't want your character to strafe")]
        public float strafeDistance = 3f;
        [Tooltip("Velocity to rotate the character while strafing")]
        public float strafeRotationSpeed = 2f;

        [Header("--- Combat ---")]
        [Tooltip("Distance to start attack the target")]
        public float distanceToAttack = 2f;
        [Tooltip("Velocity to rotate the character while attacking")]
        public float attackRotationSpeed = 0.5f;
        [Tooltip("Min frequency to attack")]
        public float minTimeToAttack = 1f;
        [Tooltip("Max frequency to attack")]
        public float maxTimeToAttack = 5f;
        [Tooltip("How many attacks the AI will make on a combo")]
        public int maxAttackCount = 3;
        [Tooltip("Randomly attacks based on the maxAttackCount")]
        public bool randomAttackCount = true;
        [Range(0f, 1f)]
        public float chanceToRoll = .1f;
        [Range(0f, 1f)]
        public float chanceToBlockInStrafe = .1f;
        [Range(0f, 1f)]
        public float chanceToBlockAttack = 0f;
        [Tooltip("How much time the character will stand up the shield")]
        public float raiseShield = 4f;
        [Tooltip("How much time the character will lower the shield")]
        public float lowerShield = 2f;

        [Header("--- Waypoint ---")]
        [Tooltip("Max Distance to change waypoint")]
        [Range(0.5f, 2f)]
        public float distanceToChangeWaypoint = 1f;
        [Tooltip("Min Distance to stop when Patrolling through waypoints")]
        [Range(0.5f, 2f)]
        public float patrollingStopDistance = 0.5f;
        public vWaypointArea pathArea;
        public bool randomWaypoints;

        public bool ragdolled { get; set; }
        public FisherYatesRandom randomWaypoint = new FisherYatesRandom();
        public FisherYatesRandom randomPatrolPoint = new FisherYatesRandom();
        [HideInInspector] public CapsuleCollider _capsuleCollider;
        [HideInInspector] public SpriteHealth healthSlider;
        [HideInInspector] public MeleeEquipmentManager meleeManager;
        [HideInInspector] public OffMeshLinkMoveMethod method = OffMeshLinkMoveMethod.Grounded;

        public enum OffMeshLinkMoveMethod
        {
            Teleport,
            DropDown,
            JumpAcross,
            Action,
            Grounded
        }

        public enum AIStates
        {
            Idle,
            PatrolPoints,
            PatrolWaypoints,
            Chase
        }
        #endregion

        #region protected variables
        protected Transform target;
        protected Vector3 targetPos;
        protected Vector3 destination;
        protected bool onGround;
        protected bool strafing;
        protected int attackCount;
        protected int currentWaypoint;
        protected int currentPatrolPoint;
        protected float direction;
        protected float timer, wait;
        protected float fovAngle;
        protected float moveSet_ID;
        protected float sideMovement, fwdMovement = 0;
        protected float strafeSwapeFrequency;
        protected float groundDistance;
        protected RaycastHit groundHit;
        protected Animator animator;
        protected NavMeshAgent agent;
        protected NavMeshPath agentPath;
        protected Quaternion freeRotation;
        protected Quaternion desiredRotation;
        protected Vector3 oldPosition;
        protected Vector3 combatMovement;
        protected Vector3 rollDirection;
        protected Rigidbody _rigidbody;
        protected PhysicMaterial frictionPhysics;
        protected Transform head;
        protected Collider colliderTarget;
        protected vWaypoint targetWaypoint;
        protected VPoint targetPatrolPoint;
        protected System.Collections.Generic.List<VPoint> visitedPatrolPoint = new System.Collections.Generic.List<VPoint>();
        protected System.Collections.Generic.List<vWaypoint> visitedWaypoint = new System.Collections.Generic.List<vWaypoint>();
        #endregion

        #region action bools
        protected bool
           quickTurn,
           jumpOver,
           stepUp,
           climbUp,
           hitReaction,
           hitRecoil,
           crouch,
           canAttack,
           blocking,
           rolling;

        // one bool to rule then all
        public bool actions { get { return stepUp || climbUp || jumpOver || quickTurn || hitReaction || hitRecoil || rolling; } }

        /// <summary>
        ///  DISABLE ACTIONS - call this method in canse you need to turn every action bool false
        /// </summary>
        protected void DisableActions()
        {
            blocking = false;
            strafing = false;
            jumpOver = false;
            stepUp = false;
            crouch = false;
            climbUp = false;
            quickTurn = false;
            rolling = false;
        }
        #endregion       

        public void Init()
        {
            if (randomWaypoint == null)
                randomWaypoint = new FisherYatesRandom();
            if (randomPatrolPoint == null)
                randomPatrolPoint = new FisherYatesRandom();
            currentWaypoint = -1;
            currentPatrolPoint = -1;

            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agentPath = new NavMeshPath();

            animator = GetComponent<Animator>();
            meleeManager = GetComponent<MeleeEquipmentManager>();
            canAttack = true;
            sideMovement = GetRandonSide();
            destination = transform.position;

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = false;
            _rigidbody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            _capsuleCollider = GetComponent<CapsuleCollider>();

            healthSlider = GetComponentInChildren<SpriteHealth>();
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            oldPosition = transform.position;
            currentHealth = startingHealth;

            method = OffMeshLinkMoveMethod.Grounded;

            if (vGameController.instance != null && vGameController.instance.currentPlayer != null)
                target = vGameController.instance.currentPlayer.transform;
        }

        /// <summary>
        /// Calculate Fov Angle
        /// </summary>
        /// <returns></returns>
        public bool onFovAngle()
        {
            var freeRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            var newAngle = freeRotation.eulerAngles - transform.eulerAngles;
            fovAngle = newAngle.NormalizeAngle().y;

            if (fovAngle > fieldOfView || fovAngle < -fieldOfView)
                return false;

            return true;
        }

        /// <summary>
        /// Target Detection
        /// </summary>
        /// <param name="_target"></param>
        /// <returns></returns>
        public bool canSeeTarget()
        {
            if (target == null)
                return false;
            if (TargetDistance > maxDetectDistance)
                return false;
            if (colliderTarget == null) colliderTarget = target.GetComponent<Collider>();
            if (colliderTarget == null) return false;
            var top = new Vector3(colliderTarget.bounds.center.x, colliderTarget.bounds.max.y, colliderTarget.bounds.center.z);
            var bottom = new Vector3(colliderTarget.bounds.center.x, colliderTarget.bounds.min.y, colliderTarget.bounds.center.z);
            var offset = Vector3.Distance(top, bottom) * 0.15f;
            top.y -= offset;
            bottom.y += offset;

            if (!onFovAngle() && TargetDistance > minDetectDistance)
                return false;

            //Debug.DrawLine(head.position, top);
            //Debug.DrawLine(head.position, bottom);
            //Debug.DrawLine(head.position, colliderTarget.bounds.center);

            RaycastHit hit;
            if (Physics.Linecast(head.position, top, out hit))
                if (hit.transform.gameObject.tag.Equals("Player"))
                    return true;
            if (Physics.Linecast(head.position, bottom, out hit))
                if (hit.transform.gameObject.tag.Equals("Player"))
                    return true;
            if (Physics.Linecast(head.position, colliderTarget.bounds.center, out hit))
                if (hit.transform.gameObject.tag.Equals("Player"))
                    return true;

            return false;
        }

        public bool OnCombatArea
        {
            get
            {
                if (target == null) return false;
                var inFloor = Vector3.Distance(new Vector3(0, transform.position.y, 0), new Vector3(0, target.position.y, 0)) < 1.5f;
                return (inFloor && (currentState.Equals(AIStates.Chase) && TargetDistance < strafeDistance && !agent.isOnOffMeshLink));
            }
        }

        public bool OnStrafeArea
        {
            get
            {
                if (target == null) return false;
                if (currentState.Equals(AIStates.PatrolPoints) || quickTurn) return false;

                var inFloor = Vector3.Distance(new Vector3(0, transform.position.y, 0), new Vector3(0, target.position.y, 0)) < 1.5f;
                if (strafing)
                    strafing = !(currentState.Equals(AIStates.Chase) && TargetDistance > (strafeDistance + 2f));
                else
                    strafing = OnCombatArea;
                return inFloor ? strafing : false;
            }
        }

        public float TargetDistance
        {
            get
            {
                if (target != null)
                    return Vector3.Distance(transform.position, target.position);
                return Mathf.Infinity;
            }
        }

        public GameObject CheckActionObject()
        {
            GameObject _object = null;

            RaycastHit hitInfoAction;
            Vector3 yOffSet = new Vector3(0f, 0.5f, 0f);
            OffMeshLinkData data = agent.currentOffMeshLinkData;
            Ray ray = new Ray(transform.position + yOffSet, new Vector3(data.endPos.x, transform.position.y, data.endPos.z) - transform.position);

            //Debug.DrawRay(transform.position + yOffSet, ray.direction*distanceOfRayActionTrigger, Color.blue, 0.1f);
            if (Physics.Raycast(ray, out hitInfoAction, distanceOfRayActionTrigger))
            {
                _object = hitInfoAction.transform.gameObject;
            }

            return _object;
        }

        public float distanceOfRayActionTrigger
        {
            get
            {
                if (_capsuleCollider == null) return 0f;
                var dist = _capsuleCollider.radius + checkTriggerDistance;
                return dist;
            }
        }

        /// <summary>
        /// Waypoint WaitTime
        /// </summary>
        /// <param name="waitTime"></param>
        public void ReturnWaitTime(float waitTime)
        {
            wait = waitTime;
        }

        public bool AgentDone()
        {
            if (!agent.enabled)
                return true;
            return !agent.pathPending && AgentStopping();
        }

        public bool AgentStopping()
        {
            if (!agent.enabled || !agent.isOnNavMesh)
                return true;
            return agent.remainingDistance <= agent.stoppingDistance;
        }

        public void ResetRagdoll()
        {
            oldPosition = transform.position;
            ragdolled = false;
        }

        public void EnableRagdoll()
        {
            agent.enabled = false;
            ragdolled = true;
            _capsuleCollider.isTrigger = true;

        }

        public void RagdollGettingUp()
        {
            agent.enabled = true;

            _capsuleCollider.isTrigger = false;

        }

        public Transform headTarget
        {
            get
            {
                if (target != null && target.GetComponent<Animator>() != null)
                    return target.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
                else
                    return null;
            }
        }

        public int GetRandonSide()
        {
            var side = Random.Range(-1, 1);
            if (side < 0)
            {
                side = -1;
            }
            else side = 1;
            return side;
        }

        protected bool CheckChanceToRoll()
        {
            System.Random random = new System.Random();

            var randomRoll = (float)random.NextDouble();
            if (randomRoll < chanceToRoll && randomRoll > 0 && target != null)
            {
                sideMovement = GetRandonSide();
                Ray ray = new Ray(target.position, target.right * sideMovement);
                ray = new Ray(transform.position, ray.GetPoint(2f) - transform.position);

                rollDirection = ray.GetPoint(10f);
                //Debug.DrawLine(transform.position, rollDirection, Color.red, 10f);
                rolling = true;
                return true;
            }
            return false;
        }

        protected IEnumerator CheckChanceToBlock(float chance, float timeToEnter)
        {
            System.Random random = new System.Random();

            var randomBlock = (float)random.NextDouble();
            if (randomBlock < chance && randomBlock > 0 && !blocking)
            {
                if (timeToEnter > 0)
                    yield return new WaitForSeconds(timeToEnter);
                blocking = true;
                StartCoroutine(ResetBlock());
            }
        }

        protected IEnumerator ResetBlock()
        {
            yield return new WaitForSeconds(raiseShield);
            blocking = false;
        }

        protected void CheckGroundDistance()
        {
            if (_capsuleCollider != null)
            {
                // radius of the SphereCast
                float radius = _capsuleCollider.radius * 0.9f;
                var dist = Mathf.Infinity;
                // position of the SphereCast origin starting at the base of the capsule
                Vector3 pos = transform.position + Vector3.up * (_capsuleCollider.radius);
                // ray for RayCast
                Ray ray1 = new Ray(transform.position + new Vector3(0, _capsuleCollider.height / 2, 0), Vector3.down);
                // ray for SphereCast
                Ray ray2 = new Ray(pos, -Vector3.up);
                // raycast for check the ground distance
                if (Physics.Raycast(ray1, out groundHit, Mathf.Infinity, groundLayer))
                    dist = transform.position.y - groundHit.point.y;

                // sphere cast around the base of the capsule to check the ground distance
                if (Physics.SphereCast(ray2, radius, out groundHit, Mathf.Infinity, groundLayer))
                {
                    // check if sphereCast distance is small than the ray cast distance
                    if (dist > (groundHit.distance - _capsuleCollider.radius * 0.1f))
                        dist = (groundHit.distance - _capsuleCollider.radius * 0.1f);
                }
                groundDistance = dist;
            }
        }

        public void CheckAutoCrouch()
        {
            // radius of SphereCast
            float radius = _capsuleCollider.radius * 0.9f;
            // Position of SphereCast origin stating in base of capsule
            Vector3 pos = transform.position + Vector3.up * ((_capsuleCollider.height * 0.5f) - _capsuleCollider.radius);
            // ray for SphereCast
            Ray ray2 = new Ray(pos, Vector3.up);
            RaycastHit groundHit;
            // sphere cast around the base of capsule for check ground distance
            //if (Physics.SphereCast(ray2, radius, out groundHit, _capsuleCollider.bounds.max.y - (_capsuleCollider.radius * 0.1f), groundLayer))
            if (Physics.SphereCast(ray2, radius, out groundHit, headDetect - (_capsuleCollider.radius * 0.1f), autoCrouchLayer))
                crouch = true;
            else
                crouch = false;
        }

        public void CheckHealth()
        {
            // If the player has lost all it's health and the death flag hasn't been set yet...
            if (currentHealth <= 0 && !isDead)
            {
                isDead = true;
                DisableActions();
                // drop the equipment when die
                transform.SendMessage("DropWeapon", SendMessageOptions.DontRequireReceiver);
                transform.SendMessage("DropShield", SendMessageOptions.DontRequireReceiver);
            }
        }

        public void HealthRecovery()
        {
            if (currentHealth <= 0) return;
            if (currentHealthRecoveryDelay > 0)
            {
                currentHealthRecoveryDelay -= Time.deltaTime;
            }
            else
            {
                if (currentHealth > startingHealth)
                    currentHealth = startingHealth;
                if (currentHealth < startingHealth)
                    currentHealth = Mathf.Lerp(currentHealth, startingHealth, healthRecovery * Time.deltaTime);
            }
        }

        public void TakeDamage(Damage damage)
        {
            // ignore damage if the character is rolling
            if (rolling || currentHealth <= 0) return;
            if (!damage.ignoreDefense && !actions && CheckChanceToRoll()) return;

            // change to block an attack
            StartCoroutine(CheckChanceToBlock(chanceToBlockAttack, 0));
            var defenseRangeConditions = meleeManager != null ? (meleeManager.currentMeleeShield != null ? meleeManager.currentMeleeShield.AttackInDefenseRange(damage.sender) : false) : false;
            // trigger the hitReaction animation if the character is blocking
            if (blocking && (animator != null && animator.enabled) && !damage.ignoreDefense && defenseRangeConditions)
            {
                var recoil_id = (int)damage.recoil_id - (int)(meleeManager.currentMeleeShield != null ? meleeManager.currentMeleeShield.Recoil_ID : 0);
                var targetRecoil_id = (int)damage.recoil_id + (int)(meleeManager.currentMeleeShield != null ? meleeManager.currentMeleeShield.Recoil_ID : 0);
                SendMessage("TriggerRecoil", recoil_id, SendMessageOptions.DontRequireReceiver);
                if (meleeManager.currentMeleeShield.breakAttack) damage.sender.SendMessage("TriggerRecoil", targetRecoil_id, SendMessageOptions.DontRequireReceiver);
                var damageResult = GetDamageResult(damage.value, meleeManager.currentMeleeShield.defenseRate);
                currentHealth -= damageResult;
                if (healthSlider != null)
                    healthSlider.Damage(damageResult);
                currentHealthRecoveryDelay = healthRecoveryDelay;
                CheckHealth();
                return;
            }
            // reduce the current health by the damage value.
            currentHealth -= damage.value;
            currentHealthRecoveryDelay = healthRecoveryDelay;
            // update the HUD display
            if (healthSlider != null)
                healthSlider.Damage(damage.value);
            // trigger the HitReaction if the character take the damage
            if (animator != null && animator.enabled && !damage.activeRagdoll && !actions)
                animator.SetTrigger("HitReaction");
            if (damage.activeRagdoll)
                transform.SendMessage("ActivateRagdoll", SendMessageOptions.DontRequireReceiver);

            CheckHealth();
        }

        protected void RemoveComponents()
        {
            if (_capsuleCollider != null) Destroy(_capsuleCollider);
            if (_rigidbody != null) Destroy(_rigidbody);
            if (animator != null) Destroy(animator);
            if (agent != null) Destroy(agent);
            var comps = GetComponents<MonoBehaviour>();
            foreach (Component comp in comps) Destroy(comp);
        }

        int GetDamageResult(int damage, float defenseRate)
        {
            int result = (int)(damage - ((damage * defenseRate) / 100));
            return result;
        }
    }
}
