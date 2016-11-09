using UnityEngine;

public class CollectableMelee : MonoBehaviour{
    public bool startUsing;
    public bool destroyOnDrop;   
    public string message = "Pick up Weapon";
    public string handler = "handler@weaponName";

    private bool usingPhysics;
    SphereCollider _sphere;
    Collider _collider;
    Rigidbody _rigidbody;    

    [HideInInspector] public MeleeItem _meleeItem;

	void Start (){
        _meleeItem = GetComponent<MeleeItem>();
        if(_meleeItem == null)
            _meleeItem = GetComponentInChildren<MeleeItem>();

        _rigidbody = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        _sphere = GetComponent<SphereCollider>();
        
        if (startUsing)
            EnableMeleeItem();
        else
            DisableMeleeItem();
    }	
	
	void Update (){
        if (_rigidbody.IsSleeping() && usingPhysics){
            usingPhysics = false;
            _collider.enabled = false;
            _rigidbody.isKinematic = true;
            _rigidbody.useGravity = false;
        }
	}

    public void EnableMeleeItem(){        
        _sphere.enabled = false;
        _rigidbody.isKinematic = true;
        _rigidbody.useGravity = false;
        _collider.enabled = false;
        _collider.isTrigger = true;        
    }

    public void DisableMeleeItem(){
        _sphere.enabled = true;
        _collider.enabled = true;
        _collider.isTrigger = false;
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        usingPhysics = true;
        _meleeItem.SetActive(false);        
    }
}
