using UnityEngine;

namespace Invector{
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Rigidbody))]

    public class HitBox : MonoBehaviour{
        public HitBarPoints hitBarPoint;
        [HideInInspector] public MeleeWeapon hitControl;
        void Start(){
            var _rigidbody = GetComponent<Rigidbody>();
            var box = GetComponent<BoxCollider>();
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
            _rigidbody.constraints = RigidbodyConstraints.FreezeAll;            
            box.isTrigger = true;
        } 

        void OnTriggerEnter(Collider other){            
            if (hitControl != null && hitControl.isActive)
                CheckHitPropertys(other);          
        }

        void OnTriggerStary(Collider other){            
            if (hitControl!=null&&hitControl.isActive)
                CheckHitPropertys(other);
        }

        protected void CheckHitPropertys(Collider other){
            var inDamage = false;
            var inRecoil = false;

            if (hitControl.hitProperties.hitRecoilLayer == (hitControl.hitProperties.hitRecoilLayer | (1 << other.gameObject.layer)))
                inRecoil = true ;
            else
                inRecoil = false;

            if (hitControl.hitProperties.hitDamageTags == null || hitControl.hitProperties.hitDamageTags.Count == 0)
                inDamage = true;                      
            else if (hitControl.hitProperties.hitDamageTags.Contains(other.tag))
                inDamage = true;
            else
                inDamage = false;

            if(inDamage == true)
                SendMessageUpwards("OnDamageHit", new HitInfo(other, transform.position, hitBarPoint), SendMessageOptions.DontRequireReceiver);
            if (inRecoil == true){
                if (hitBarPoint == HitBarPoints.Bottom)
                    SendMessageUpwards("OnRecoilHit", new HitInfo(other,transform.position,hitBarPoint), SendMessageOptions.DontRequireReceiver);
            }               
        }        

        [System.Serializable]       
        public class HitInfo{
            public HitBarPoints hitBarPoint;
            public Vector3 hitPoint;
            public Collider hitCollider;
            public HitInfo (Collider hitCollider,Vector3 hitPoint,HitBarPoints hitBarPoint){
                this.hitCollider = hitCollider;
                this.hitPoint = hitPoint;
                this.hitBarPoint = hitBarPoint;
            }
        }
    }
}