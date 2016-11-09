using UnityEngine;

public class CharacterTemplate : ScriptableObject{
    public enum TemplateType{ThirdPerson}
    public TemplateType templateType;
    public RuntimeAnimatorController animatorController;
    public TPCameraListData cameraListData;
    public HUDController hud;
}