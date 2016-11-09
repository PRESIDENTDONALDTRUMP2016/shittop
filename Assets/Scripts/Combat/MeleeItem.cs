using UnityEngine;

public abstract class MeleeItem : MonoBehaviour {
    protected bool active;
    public void SetActive(bool value) {
        active = value;
    }
}
