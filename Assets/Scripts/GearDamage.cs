using UnityEngine;

public class GearDamage : MonoBehaviour
{
    public float damage = 1f;

    // initialize the gear's damage
    void Start()
    {
        if (gameObject.tag != "Gear")
            Debug.LogWarning("[GearDamage] '" + name + "' is tagged '" + gameObject.tag + "', not 'Gear'. Enemies won't register hits.", this);

        if (GetComponentInChildren<Collider>() == null && GetComponent<Collider>() == null)
            Debug.LogWarning("[GearDamage] '" + name + "' has no collider, so it can't hit anything.", this);

        Debug.Log("[GearDamage] '" + name + "' ready. Damage = " + damage, this);
    }
}