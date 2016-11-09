using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Invector{
    /// LockOnTarget Abstract class inherited MonoBehaviour
    public abstract class LockOnTarget : MonoBehaviour{
        #region properties
        private Transform watcher;
        [Tooltip("Tags of objects that can be found")]
        public string[] tagsToFind = new string[] { "Enemy" };
        [Tooltip("Layer of Obscatacles to prevent the find for targets")]
        public LayerMask layerOfObstacles;
        [Range(0, 1)]
        [Tooltip("Use this to set a margin of Aim point ")]
        public float screenMarginX = 0.8f;
        [Range(0, 1)]
        [Tooltip("Use this to set a margin of Aim point ")]
        public float screenMarginY = 0.1f;
        [Tooltip("Range of the search for targets")]
        public float range = 10f;
        [Tooltip("Create a offset for the sprite based at the center of the target")]
        [Range(-0.5f, 0.5f)]
        public float spriteHeight = 0.25f;
        [Tooltip("Show the Gizmos and helpers")]
        public bool showDebug;
        public float timeToChangeTarget = 0.25f;

        protected Camera cam;
        private int index = 0;
        private List<Transform> visibles;
        private Transform target;
        private Rect rect;
        private bool _inLockOn;
        protected bool changingTarget;
        #endregion

        #region delegates

        #endregion

        #region public methods

        /// <summary>
        /// lock on target 
        /// </summary>
        /// <param name="value">use true or false to On/Off the lockOn</param>
        public virtual void UpdateLockOn(bool value)
        {
            if (value == true && value != _inLockOn)
            {
                _inLockOn = value;
                visibles = GetPossibleTargets();
                index = 0;
                if (visibles != null && visibles.Count > 0)
                    target = visibles[index];
            }
            else if (value == false && value != _inLockOn)
            {
                _inLockOn = value;
                index = 0;
                target = null;
                visibles.Clear();
            }
        }
        /// <summary>
        /// change the current target to next target of possibles target
        /// if exist more than 1 target in list
        /// </summary>
        public virtual void ChangeTarget(int value)
        {
            StartCoroutine(ChangeTargetRoutine(value));
        }

        public virtual void SetTarget(){}

        IEnumerator ChangeTargetRoutine(int value)
        {
            if (!changingTarget)
            {
                changingTarget = true;
                visibles = GetPossibleTargets();
                if ((_inLockOn == true && visibles != null && visibles.Count > 1))
                {
                    if (index + value > visibles.Count - 1)
                        index = 0;
                    else if (index + value < 0)
                        index = visibles.Count - 1;
                    else
                        index += value;
                    target = visibles[index];
                    SetTarget();
                }
                yield return new WaitForSeconds(timeToChangeTarget);
                changingTarget = false;
            }
        }

        /// Get current target
        public virtual Transform currentTarget{
            get { return target; }
        }
        public virtual bool isCharacterAlive()
        {
            if (currentTarget == null) return false;
            var ichar = currentTarget.GetComponent<ICharacter>();
            if (ichar == null) return false;
            if (ichar.ragdolled) return false;
            if (ichar.currentHealth > 0) return true;
            return false;
        }
        public virtual bool isCharacterAlive(Transform other)
        {
            var ichar = other.GetComponent<ICharacter>();
            if (ichar == null) return false;
            if (ichar.ragdolled) return false;
            if (ichar.currentHealth > 0) return true;
            return false;
        }
        public virtual void ResetLockOn()
        {
            target = null;
            _inLockOn = false;
        }

        /// Get all target possibles
        public virtual List<Transform> allTargets{
            get { if (visibles != null && visibles.Count > 0) return visibles; return null; }
        }

        #endregion

        #region protected methods
        /// Draw GUI of Rect
        protected void OnGUI()
        {
            if (showDebug)
            {
                var width = Screen.width - (Screen.width * screenMarginX);
                var height = Screen.height - (Screen.height * screenMarginY);
                var posX = (Screen.width * 0.5f) - (width * 0.5f);
                var posY = (Screen.height * 0.5f) - (height * 0.5f);
                rect = new Rect(posX, posY, width, height);
                GUI.Box(rect, "");
            }
        }

        /// Init the properts
        protected void Init()
        {
            if (cam == null)
                cam = GetComponent<Camera>();
            if (cam == null) this.enabled = false;
            var width = Screen.width - (Screen.width * screenMarginX);
            var height = Screen.height - (Screen.height * screenMarginY);
            var posX = (Screen.width * 0.5f) - (width * 0.5f);
            var posY = (Screen.height * 0.5f) - (height * 0.5f);
            rect = new Rect(posX, posY, width, height);
        }

        /// Draw the range gizmo
        protected void OnDrawGizmos()
        {
            if (showDebug && watcher)
            {
                Gizmos.color = new Color(0, 1, 0, 0.2f);
                Gizmos.DrawSphere(watcher.position, range + 0.1f);
                if (visibles != null && visibles.Count > 0 && target != null)
                {
                    visibles.ForEach(delegate (Transform _transform)
                    {
                        Gizmos.color = _transform.Equals(currentTarget) ? Color.red : Color.yellow;
                        Gizmos.DrawSphere(_transform.GetComponent<Collider>().bounds.center, .5f);
                    });
                }
            }
        }

        /// get all possibles targets
        protected List<Transform> GetPossibleTargets()
        {
            if (TPCamera.instance != null && TPCamera.instance.target != null)
                watcher = TPCamera.instance.target;
            else
                watcher = transform;
            var listPrimary = new List<Transform>();
            var targets = Physics.SphereCastAll(watcher.position, range, watcher.forward, .01f);
            System.Array.ForEach<RaycastHit>(targets, delegate (RaycastHit hitOther)
            {
                if (tagsToFind.Contains(hitOther.transform.tag))
                {
                    if (isCharacterAlive(hitOther.transform.GetComponent<Transform>()))
                    {
                        RaycastHit hit;
                        foreach (Vector3 point in BoundPoints(hitOther.transform.GetComponent<Collider>()))
                        {
                            if (Physics.Linecast(transform.position, point, out hit, layerOfObstacles))
                            {
                                if (hit.transform == hitOther.transform)
                                {
                                    listPrimary.Add(hitOther.transform);
                                    if (showDebug)
                                        Debug.DrawLine(transform.position, point, Color.green, 2);
                                    break;
                                }
                                else if (showDebug)
                                {
                                    Debug.DrawLine(transform.position, point, Color.red, 2);
                                }
                            }
                            else
                            {
                                listPrimary.Add(hitOther.transform);
                                if (showDebug)
                                    Debug.DrawLine(transform.position, point, Color.green, 2);
                                break;
                            }
                        }
                    }
                }
            });
            SortTargets(ref listPrimary);
            return listPrimary;
        }

        /// Sort the targets of possible targets by order of priority
        protected void SortTargets(ref List<Transform> list)
        {
            var lpriority_01 = new List<Transform>();
            var lpriority_02 = new List<Transform>();
            var lpriority_03 = new List<Transform>();
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            list.ForEach(delegate (Transform _transform)
            {
                Vector2 screenPoint = cam.WorldToScreenPoint(_transform.transform.position);
                if (GeometryUtility.TestPlanesAABB(planes, _transform.GetComponent<Collider>().bounds) && rect.Contains(screenPoint))
                    lpriority_01.Add(_transform);
                else if (GeometryUtility.TestPlanesAABB(planes, _transform.GetComponent<Collider>().bounds))
                    lpriority_02.Add(_transform);
                else
                    lpriority_03.Add(_transform);
            });
            lpriority_01.Sort(delegate (Transform t1, Transform t2)
            {
                Vector2 screenPoint_01 = cam.WorldToScreenPoint(t1.transform.position);
                Vector2 screenPoint_02 = cam.WorldToScreenPoint(t2.transform.position);
                return Vector2.Distance(screenPoint_01, rect.center)
                    .CompareTo(Vector2.Distance(screenPoint_02, rect.center));
            });
            lpriority_02.Sort(delegate (Transform t1, Transform t2)
            {
                Vector2 screenPoint_01 = cam.WorldToScreenPoint(t1.transform.position);
                Vector2 screenPoint_02 = cam.WorldToScreenPoint(t2.transform.position);
                return Vector2.Distance(screenPoint_01, rect.center)
                    .CompareTo(Vector2.Distance(screenPoint_02, rect.center));
            });
            lpriority_03.Sort(delegate (Transform t1, Transform t2)
            {
                return Vector3.Distance(t1.transform.position, transform.position)
                    .CompareTo(Vector3.Distance(t2.transform.position, transform.position));
            });

            list = lpriority_01.Union(lpriority_02).Union(lpriority_03).ToList();
        }

        /// return 8 bound points
        protected Vector3[] BoundPoints(Collider collider)
        {
            var boundPoint1 = collider.bounds.min;
            var boundPoint2 = collider.bounds.max;
            var boundPoint3 = new Vector3(boundPoint1.x, boundPoint1.y, boundPoint2.z);
            var boundPoint4 = new Vector3(boundPoint1.x, boundPoint2.y, boundPoint1.z);
            var boundPoint5 = new Vector3(boundPoint2.x, boundPoint1.y, boundPoint1.z);
            var boundPoint6 = new Vector3(boundPoint1.x, boundPoint2.y, boundPoint2.z);
            var boundPoint7 = new Vector3(boundPoint2.x, boundPoint1.y, boundPoint2.z);
            var boundPoint8 = new Vector3(boundPoint2.x, boundPoint2.y, boundPoint1.z);
            var lineColor = Color.white;
            if (showDebug)
            {
                Debug.DrawLine(boundPoint6, boundPoint2, lineColor, 1);
                Debug.DrawLine(boundPoint2, boundPoint8, lineColor, 1);
                Debug.DrawLine(boundPoint8, boundPoint4, lineColor, 1);
                Debug.DrawLine(boundPoint4, boundPoint6, lineColor, 1);
                // bottom of rectangular cuboid (3-7-5-1)
                Debug.DrawLine(boundPoint3, boundPoint7, lineColor, 1);
                Debug.DrawLine(boundPoint7, boundPoint5, lineColor, 1);
                Debug.DrawLine(boundPoint5, boundPoint1, lineColor, 1);
                Debug.DrawLine(boundPoint1, boundPoint3, lineColor, 1);
                // legs (6-3, 2-7, 8-5, 4-1)
                Debug.DrawLine(boundPoint6, boundPoint3, lineColor, 1);
                Debug.DrawLine(boundPoint2, boundPoint7, lineColor, 1);
                Debug.DrawLine(boundPoint8, boundPoint5, lineColor, 1);
                Debug.DrawLine(boundPoint4, boundPoint1, lineColor, 1);
            }
            return new Vector3[] { boundPoint1, boundPoint2, boundPoint3, boundPoint4, boundPoint5, boundPoint6, boundPoint7, boundPoint8 };
        }
        #endregion
    }

    /// Extencions for Help 
    public static class LockOnHelper{
        /// <summary>
        /// Get point of Target relative to Screen
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="targetPoint"></param>
        /// <returns></returns>
        public static Vector2 GetScreenPointOffBoundsCenter(this Transform target, Canvas canvas, Camera cam, float _heightOffset){
            var bounds = target.GetComponent<Collider>().bounds;
            var middle = bounds.center;
            var height = Vector3.Distance(bounds.min, bounds.max);

            var point = middle + new Vector3(0, height * _heightOffset, 0);
            var rectTransform = canvas.transform as RectTransform;
            Vector2 ViewportPosition = cam.WorldToViewportPoint(point);
            Vector2 WorldObject_ScreenPosition = new Vector2(
             ((ViewportPosition.x * rectTransform.sizeDelta.x) - (rectTransform.sizeDelta.x * 0.5f)),
             ((ViewportPosition.y * rectTransform.sizeDelta.y) - (rectTransform.sizeDelta.y * 0.5f)));
            return WorldObject_ScreenPosition;
        }

        public static Vector3 GetPointOffBoundsCenter(this Transform target, float _heightOffset){
            var bounds = target.GetComponent<Collider>().bounds;
            var middle = bounds.center;
            var height = Vector3.Distance(bounds.min, bounds.max);

            var point = middle + new Vector3(0, height * _heightOffset, 0);
            return point;
        }
    }
}