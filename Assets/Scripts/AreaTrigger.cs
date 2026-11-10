using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaTrigger : MonoBehaviour
{
    [SerializeField]
    private GearPullManager _gearPullManager;

    [SerializeField]
    private GearPullManager.GameArea _area =
        GearPullManager.GameArea.CastleInterior;

    // reset the collider to be a trigger
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    // handle player entering, staying, and exiting the area
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        if (_gearPullManager == null)
        {
            Debug.LogWarning(
                "GearAreaTrigger: Assign GearPullManager.",
                this
            );
            return;
        }
        _gearPullManager.SetArea(_area);
    }
}