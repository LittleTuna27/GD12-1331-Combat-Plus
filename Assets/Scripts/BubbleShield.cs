using UnityEngine;

public class BubbleShield : MonoBehaviour
{
    public GameObject owner;

    public void AttachToOwner(GameObject target)
    {
        owner = target;
        transform.SetParent(target.transform);
        transform.localPosition = Vector3.zero; // centers it on the player
    }
}