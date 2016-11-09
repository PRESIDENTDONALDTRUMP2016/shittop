using UnityEngine;

public class FootStepTrigger : MonoBehaviour{
    protected Collider _trigger;
    protected FootStepFromTexture _fT;
    void Start(){
        _fT = GetComponentInParent<FootStepFromTexture>();
        if(_fT == null)        {
            Debug.Log(gameObject.name + " can't find the FootStepFromTexture");
            gameObject.SetActive(false);
        }
    }
    public Collider trigger{
        get{
            if(_trigger == null) _trigger = GetComponent<Collider>();
            return _trigger;
        }
    }   
    void OnTriggerEnter(Collider other){        
        if (other.GetComponent<Terrain>() != null) //Check if trigger objet is a terrain
            _fT.StepOnTerrain(new FootStepObject(transform));
        else{
            var stepHandle = other.GetComponent<FootStepHandler>();
            var renderer = other.GetComponent<Renderer>();
            //Check renderer
            if (renderer != null && renderer.material != null){
                var index = 0;
                var _name = string.Empty;
                if (stepHandle != null && stepHandle.material_ID > 0)// if trigger contains a StepHandler to pass material ID. Default is (0)
                    index = stepHandle.material_ID;
                if (stepHandle)
                {   
                    // check  stepHandlerType
                    switch (stepHandle.stepHandleType)
                    {
                        case FootStepHandler.StepHandleType.materialName:
                            _name = renderer.materials[index].name;
                            break;
                        case FootStepHandler.StepHandleType.textureName:
                            _name = renderer.materials[index].mainTexture.name;
                            break;                            
                    }
                }
                else
                    _name = renderer.materials[index].name;                    
                _fT.StepOnMesh(new FootStepObject(transform, _name));
            }
        }        
    }
}

/// Foot step Object work with FootStepFromTexture
public class FootStepObject{
    public string name;
    public Transform sender;
    public FootStepObject(Transform sender, string name = ""){
        this.name = name;
        this.sender = sender;
    }
}