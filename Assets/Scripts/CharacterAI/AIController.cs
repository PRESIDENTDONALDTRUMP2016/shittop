using UnityEngine;
using System.Collections;

namespace Invector
{
    public class AIController : AIAnimator
    {
        void Start()
        {
            Init();
            StartCoroutine(StateRoutine());
            StartCoroutine(UpdateDestination());
            StartCoroutine(OffMeshLinkRoutine());
        }

        void LateUpdate()
        {
            SetUpTarget();
            ControlLocomotion();
            HealthRecovery();
        }

        /// Find the target (Player) using the vGameController
        void SetUpTarget()
        {
            if (vGameController.instance != null && vGameController.instance.currentPlayer != null && currentHealth > 0)
            {
                if (headTarget == null)
                {
                    target = vGameController.instance.currentPlayer.transform;
                    if (currentState.Equals(AIStates.Chase))
                    {
                        strafing = false;
                        currentState = AIStates.PatrolWaypoints;
                    }
                }
            }
            else if (currentHealth <= 0f || vGameController.instance.lockEnemies)
            {
                currentState = AIStates.Idle;
                destination = transform.position;
                target = null;
            }
        }

        /// Set up Agent Locomotion
        void ControlLocomotion()
        {
            MoveSetIDControl();
            
            if (agent.isOnOffMeshLink)
            {
                float speed = (method == OffMeshLinkMoveMethod.Action) ? 0f : agent.desiredVelocity.magnitude;
                combatMovement = Vector3.zero;
                OffMeshLinkData data = agent.currentOffMeshLinkData;
                Vector3 velocity = Quaternion.Inverse(transform.rotation) * (new Vector3(data.endPos.x, transform.position.y, data.endPos.z) - transform.position);
                direction = Mathf.Atan2(velocity.x, velocity.z) * 180.0f / 3.14159f;
                if (agent.pathStatus != NavMeshPathStatus.PathInvalid)
                {
                    if ((direction >= 165 || direction <= -165) && !actions && onGround && !crouch)
                        quickTurn = true;
                }
                UpdateAnimator(speed, direction);
            }
            else if (AgentDone())
            {
                agent.speed = 0f;
                combatMovement = Vector3.zero;
                UpdateAnimator(0, 0);
            }
            else
            {
                if (OnStrafeArea)
                {
                    var destin = transform.InverseTransformDirection(agent.desiredVelocity).normalized;
                    combatMovement = Vector3.Lerp(combatMovement, destin, 2f * Time.fixedDeltaTime);
                    //Debug.Log(destin);     
                    UpdateAnimator(combatMovement.z, combatMovement.x);
                }
                else
                {
                    float speed = agent.desiredVelocity.magnitude;
                    combatMovement = Vector3.zero;
                    Vector3 velocity = Quaternion.Inverse(transform.rotation) * agent.desiredVelocity;
                    direction = Mathf.Atan2(velocity.x, velocity.z) * 180.0f / 3.14159f;

                    if (agent.pathStatus != NavMeshPathStatus.PathInvalid)
                    {                        
                        if ((direction >= 165 || direction <= -165) && !actions && onGround && !crouch)                           
                            quickTurn = true;                                                     
                    }
                    UpdateAnimator(speed, direction);
                }
            }
        }

        /// <summary>
        /// Change the AI States
        /// </summary>
        /// <returns></returns>
        IEnumerator StateRoutine()
        {
            while (this.enabled)
            {
                CheckIsOnNavMesh();
                CheckAutoCrouch();

                yield return new WaitForEndOfFrame();
                switch (currentState)
                {
                    case AIStates.Idle:
                        yield return StartCoroutine(Idle());
                        break;
                    case AIStates.Chase:
                        yield return StartCoroutine(Chase());
                        break;
                    case AIStates.PatrolPoints:
                        yield return StartCoroutine(Patrolling());
                        break;
                    case AIStates.PatrolWaypoints:
                        yield return StartCoroutine(Tour());
                        break;
                }
            }
        }

        /// <summary>
        /// Check if the AI is on a valid Navmesh, if not he dies
        /// </summary>
        void CheckIsOnNavMesh()
        {
            if (!agent.isOnNavMesh && agent.enabled && !ragdolled)
            {
                currentHealth = 0;
            }
        }

        /// <summary>
        /// Idle State
        /// </summary>
        /// <returns></returns>
        IEnumerator Idle()
        {
            while (!agent.enabled) yield return null;
            agent.speed = Mathf.Lerp(agent.speed, 0f, 2f * Time.deltaTime);
            if (pathArea && pathArea.waypoints.Count > 0)
                currentState = AIStates.PatrolWaypoints;
            else if (canSeeTarget())
                currentState = AIStates.Chase;
        }

        /// <summary>
        /// Patrolling States on PatrolPoints
        /// </summary>
        /// <returns></returns>
        IEnumerator Patrolling()
        {
            while (!agent.enabled) yield return null;

            if (targetWaypoint)
            {
                if (targetPatrolPoint == null || !targetPatrolPoint.isValid)
                {
                    targetPatrolPoint = GetPatrolPoint(targetWaypoint);
                }
                else
                {
                    agent.speed = Mathf.Lerp(agent.speed, (agent.hasPath && targetPatrolPoint.isValid) ? patrolSpeed : 0, 2f * Time.deltaTime);
                    agent.stoppingDistance = patrollingStopDistance;
                    destination = targetPatrolPoint.isValid ? targetPatrolPoint.position : transform.position;
                    if (Vector3.Distance(transform.position, destination) < targetPatrolPoint.areaRadius && targetPatrolPoint.CanEnter(transform) && !targetPatrolPoint.IsOnWay(transform))
                    {
                        targetPatrolPoint.Enter(transform);
                        wait = targetPatrolPoint.timeToStay;
                        visitedPatrolPoint.Add(targetPatrolPoint);
                    }
                    else if (Vector3.Distance(transform.position, destination) < targetPatrolPoint.areaRadius && (!targetPatrolPoint.CanEnter(transform) || !targetPatrolPoint.isValid)) targetPatrolPoint = GetPatrolPoint(targetWaypoint);

                    if (targetPatrolPoint != null && (targetPatrolPoint.IsOnWay(transform) && Vector3.Distance(transform.position, destination) < distanceToChangeWaypoint))
                    {
                        if (wait <= 0 || !targetPatrolPoint.isValid)
                        {
                            wait = 0;
                            if (visitedPatrolPoint.Count == pathArea.GetValidSubPoints(targetWaypoint).Count)
                            {
                                currentState = AIStates.PatrolWaypoints;
                                targetWaypoint.Exit(transform);
                                targetPatrolPoint.Exit(transform);
                                targetWaypoint = null;
                                targetPatrolPoint = null;
                                visitedPatrolPoint.Clear();
                            }
                            else
                            {
                                targetPatrolPoint.Exit(transform);
                                targetPatrolPoint = GetPatrolPoint(targetWaypoint);
                            }
                        }
                        else if (wait > 0)
                        {
                            if (agent.desiredVelocity.magnitude == 0)
                                wait -= Time.deltaTime;
                        }
                    }
                }
            }
            if (canSeeTarget())
                currentState = AIStates.Chase;
        }

        /// <summary>
        /// Patrol on Waypoints
        /// </summary>
        /// <returns></returns>
        IEnumerator Tour()
        {
            while (!agent.enabled) yield return null;

            if (pathArea != null && pathArea.waypoints.Count > 0)
            {
                if (targetWaypoint == null || !targetWaypoint.isValid)
                {
                    targetWaypoint = GetWaypoint();
                }
                else
                {
                    agent.speed = Mathf.Lerp(agent.speed, (agent.hasPath && targetWaypoint.isValid) ? patrolSpeed : 0, 2f * Time.deltaTime);
                    agent.stoppingDistance = patrollingStopDistance;

                    destination = targetWaypoint.position;
                    if (Vector3.Distance(transform.position, destination) < targetWaypoint.areaRadius && targetWaypoint.CanEnter(transform) && !targetWaypoint.IsOnWay(transform))
                    {
                        targetWaypoint.Enter(transform);
                        wait = targetWaypoint.timeToStay;
                    }
                    else if (Vector3.Distance(transform.position, destination) < targetWaypoint.areaRadius && (!targetWaypoint.CanEnter(transform) || !targetWaypoint.isValid)) targetWaypoint = GetWaypoint();

                    if (targetWaypoint != null && targetWaypoint.IsOnWay(transform) && Vector3.Distance(transform.position, destination) < distanceToChangeWaypoint)
                    {
                        if (wait <= 0 || !targetWaypoint.isValid)
                        {
                            wait = 0;
                            if (targetWaypoint.subPoints.Count > 0)
                                currentState = AIStates.PatrolPoints;
                            else
                            {
                                targetWaypoint.Exit(transform);
                                visitedPatrolPoint.Clear();
                                targetWaypoint = GetWaypoint();
                            }
                        }
                        else if (wait > 0)
                        {
                            if (agent.desiredVelocity.magnitude == 0)
                                wait -= Time.deltaTime;
                        }
                    }
                }
            }
            if (canSeeTarget())
                currentState = AIStates.Chase;
        }

        vWaypoint GetWaypoint()
        {
            var waypoints = pathArea.GetValidPoints();

            if (randomWaypoints) currentWaypoint = randomWaypoint.Next(waypoints.Count);
            else currentWaypoint++;

            if (currentWaypoint >= waypoints.Count) currentWaypoint = 0;
            if (waypoints.Count == 0)
            {
                agent.Stop();
                return null;
            }
            if (visitedWaypoint.Count == waypoints.Count) visitedWaypoint.Clear();

            if (visitedWaypoint.Contains(waypoints[currentWaypoint])) return null;

            agent.Resume();
            return waypoints[currentWaypoint];
        }

        VPoint GetPatrolPoint(vWaypoint waypoint)
        {
            var subPoints = pathArea.GetValidSubPoints(waypoint);
            if (waypoint.randomPatrolPoint) currentPatrolPoint = randomPatrolPoint.Next(subPoints.Count);
            else currentPatrolPoint++;

            if (currentPatrolPoint >= subPoints.Count) currentPatrolPoint = 0;
            if (subPoints.Count == 0)
            {
                agent.Stop();
                return null;
            }
            if (visitedPatrolPoint.Contains(subPoints[currentPatrolPoint])) return null;
            agent.Resume();
            return subPoints[currentPatrolPoint];
        }

        /// <summary>
        /// Chase a target 
        /// </summary>
        /// <returns></returns>
        IEnumerator Chase()
        {
            while (!agent.enabled || currentHealth <= 0) yield return null;
            agent.speed = Mathf.Lerp(agent.speed, chaseSpeed, 2f * Time.deltaTime);
            agent.stoppingDistance = chaseStopDistance;

            // Begin the Attack Routine when close to the Target
            if (TargetDistance <= distanceToAttack + agent.stoppingDistance && meleeManager != null && canAttack)
            {
                if (meleeManager.currentMeleeWeapon != null && !actions)
                    StartCoroutine(MeleeAttackRotine());
            }
            // Walk Strafing while close to the Target
            if (TargetDistance < strafeDistance + agent.stoppingDistance && strafeSideways)
            {               
                //Debug.DrawRay(transform.position, dir * 2, Color.red, 0.2f);
                if (strafeSwapeFrequency <= 0)
                {
                    sideMovement = GetRandonSide();
                    strafeSwapeFrequency = Random.Range(minStrafeSwape, maxStrafeSwape);
                }
                else
                {
                    strafeSwapeFrequency -= Time.fixedDeltaTime;
                }
                fwdMovement = Mathf.Lerp(fwdMovement, (TargetDistance < distanceToAttack-agent.stoppingDistance ) ? (strafeBackward ? -1 : 0) : (TargetDistance > distanceToAttack ) ? 1 : 0, 2 * Time.fixedDeltaTime);
                var dir = ((transform.right * sideMovement) + (transform.forward * fwdMovement));
                Ray ray = new Ray(new Vector3(transform.position.x, target.position.y, transform.position.z), dir);

                NavMeshHit hit;
                if (NavMesh.SamplePosition((OnCombatArea) ? ray.GetPoint(agent.stoppingDistance + 0.5f) : target.position, out hit, 0.5f, groundLayer))
                {
                    //Debug.DrawLine(transform.position, hit.position, Color.yellow, 0.1f);
                    destination = hit.position;
                }

                StartCoroutine(CheckChanceToBlock(chanceToBlockInStrafe, lowerShield));
            }
            // Chase the Target
            else
            {
                if (TargetDistance < strafeDistance)
                    StartCoroutine(CheckChanceToBlock(chanceToBlockInStrafe, lowerShield));
                if (!OnStrafeArea)
                    destination = target.position;
                else
                {
                    fwdMovement = (TargetDistance < distanceToAttack + agent.stoppingDistance) ? (strafeBackward ? -1 : 0) : TargetDistance > distanceToAttack + agent.stoppingDistance ? 1 : 0;
                    Ray ray = new Ray(transform.position, transform.forward * fwdMovement);
                    destination = (fwdMovement != 0) ? ray.GetPoint(agent.stoppingDistance + ((fwdMovement > 0) ? TargetDistance : 1f)) : transform.position;
                }
            }
        }

        /// <summary>
        /// Attack Routine
        /// </summary>
        /// <returns></returns>
        IEnumerator MeleeAttackRotine()
        {
            if (attackCount <= 0)
            {
                StartCoroutine(ResetAttack());
                yield return null;
            }
            else if (!meleeManager.inAttack)
            {
                sideMovement = GetRandonSide();
                var lookRotation = Quaternion.LookRotation(target.transform.position - transform.position);
                var euler = agent.transform.eulerAngles;
                euler.y = lookRotation.eulerAngles.y;
                agent.stoppingDistance = distanceToAttack;
                canAttack = false;

                MeleeAttack();

                yield return new WaitForSeconds(0.2f);
                attackCount--;
                canAttack = true;
            }
        }

        IEnumerator ResetAttack()
        {
            canAttack = false;
            var value = Random.Range(minTimeToAttack, maxTimeToAttack);
            yield return new WaitForSeconds(value);
            attackCount = randomAttackCount ? Random.Range(1, maxAttackCount + 1) : maxAttackCount;
            canAttack = true;
        }

        /// <summary>
        /// MOVE SET ID - check the Animator to see what MoveSet the character will move, also check your weapon to see if the moveset matches
        /// ps* Move Set is the way your character will move, ATK_ID is the way your character will attack. You can have different locomotion animations and attacks.
        /// </summary>
        void MoveSetIDControl()
        {
            if (!meleeManager) return;

            moveSet_ID = (blocking && meleeManager.currentMeleeShield != null) ? meleeManager.currentMeleeShield.MoveSet_ID :
                         (meleeManager.currentMeleeWeapon != null ? meleeManager.currentMeleeWeapon.MoveSet_ID : 0);
        }

        IEnumerator UpdateDestination()
        {
            while (this.enabled)
            {
                #region destination routine
                yield return null;
                if (agent.enabled && agent.isOnNavMesh && agent.CalculatePath(destination, agentPath) || !agent.pathPending)
                {
                    yield return new WaitForEndOfFrame();
                    if (agent.enabled && agentPath.status != NavMeshPathStatus.PathInvalid && agentPath.corners.Length > 0)
                        agent.destination = (agentPath.corners[agentPath.corners.Length - 1]);
                    else if (agent.enabled && agent.isOnNavMesh && !agent.isOnOffMeshLink) agent.ResetPath();
                }
                else if (agent.enabled && agent.hasPath && agent.path.status == NavMeshPathStatus.PathInvalid)
                {
                    NavMeshHit hit;
                    if (agent.FindClosestEdge(out hit))
                        agent.destination = hit.position;
                    else
                        agent.ResetPath();
                }

                #region debug Path                   
                if (agent.enabled && agent.hasPath)
                {

                    if (drawAgentPath)
                    {
                        Debug.DrawLine(transform.position, destination, Color.red, 0.5f);
                        var oldPos = transform.position;
                        for (int i = 0; i < agent.path.corners.Length; i++)
                        {
                            var pos = agent.path.corners[i];
                            Debug.DrawLine(oldPos, pos, Color.green, 0.5f);
                            oldPos = pos;
                        }
                    }
                }
                #endregion
                #endregion
            }
        }

        IEnumerator CheckOffMeshLink()
        {
            do
            {
                //Debug.Log(Vector3.Distance(transform.eulerAngles, desiredRotation.eulerAngles));
                method = OffMeshLinkMoveMethod.Grounded;
                yield return new WaitForEndOfFrame();
            }
            while (quickTurn);
            //while ((Vector3.Distance(transform.eulerAngles, desiredRotation.eulerAngles) > 0f) && actions);

            yield return new WaitForEndOfFrame();

            OffMeshLinkData data = agent.currentOffMeshLinkData;
            OffMeshLinkType linkType = data.linkType;
            string offMeshLinkTag = string.Empty;
            if (data.offMeshLink)
                offMeshLinkTag = data.offMeshLink.tag;

            switch (linkType)
            {
                case OffMeshLinkType.LinkTypeDropDown:
                    agent.autoTraverseOffMeshLink = false;
                    method = OffMeshLinkMoveMethod.DropDown;
                    break;
                case OffMeshLinkType.LinkTypeJumpAcross:
                    agent.autoTraverseOffMeshLink = false;
                    method = OffMeshLinkMoveMethod.JumpAcross;
                    break;
                case OffMeshLinkType.LinkTypeManual:
                    switch (offMeshLinkTag)
                    {
                        case "StepUp":
                            agent.autoTraverseOffMeshLink = false;
                            method = OffMeshLinkMoveMethod.Action;
                            break;
                        case "JumpOver":
                            agent.autoTraverseOffMeshLink = false;
                            method = OffMeshLinkMoveMethod.Action;
                            break;
                        case "ClimbUp":
                            agent.autoTraverseOffMeshLink = false;
                            method = OffMeshLinkMoveMethod.Action;
                            break;
                        default:
                            agent.autoTraverseOffMeshLink = false;
                            method = OffMeshLinkMoveMethod.Grounded;
                            break;
                    }
                    break;
            }
        }

        IEnumerator OffMeshLinkRoutine()
        {
            while (true)
            {
                if (agent.enabled && agent.isOnOffMeshLink)
                {
                    yield return StartCoroutine(CheckOffMeshLink());

                    yield return new WaitForEndOfFrame();

                    if (method == OffMeshLinkMoveMethod.DropDown)
                        yield return StartCoroutine(Parabola(agent, 1.0f, 0.5f));
                    else if (method == OffMeshLinkMoveMethod.JumpAcross)
                        yield return StartCoroutine(Parabola(agent, 1.0f, 0.5f));
                    else if (method == OffMeshLinkMoveMethod.Action)
                        CheckForwardAction();

                    while (agent.enabled == false)
                        yield return new WaitForEndOfFrame();
                }
                else
                    method = OffMeshLinkMoveMethod.Grounded;
                yield return null;
            }
        }

        IEnumerator NormalSpeed(NavMeshAgent agent)
        {
            OffMeshLinkData data = agent.currentOffMeshLinkData;
            Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
            while (agent.transform.position != endPos)
            {
                //endPos.y = transform.position.y;
                Debug.DrawLine(transform.position, endPos, Color.red);
                transform.position = Vector3.MoveTowards(transform.position, endPos, 6f * Time.deltaTime);
                agent.enabled = false;
                yield return new WaitForEndOfFrame();
            }
            if (agent.enabled && !agent.autoTraverseOffMeshLink)
                agent.CompleteOffMeshLink();
            agent.enabled = true;
        }

        IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
        {
            OffMeshLinkData data = agent.currentOffMeshLinkData;
            Vector3 startPos = agent.transform.position;
            Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

            float normalizedTime = 0.0f;
            while (normalizedTime < 1.0f)
            {
                float yOffset = height * 4.0f * (normalizedTime - normalizedTime * normalizedTime);
                agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
                normalizedTime += Time.deltaTime / duration;

                yield return null;
            }
            if (agent.enabled && !agent.autoTraverseOffMeshLink)
                agent.CompleteOffMeshLink();
        }
    }
}