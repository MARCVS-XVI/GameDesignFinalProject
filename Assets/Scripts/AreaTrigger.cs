using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaTrigger : MonoBehaviour
{
    public int levelIndex; // index of level this area belongs to
    public Door[] doorsToClose; // doors that shut behind player

    [Header("Debug")]
    public bool debugLogs = true;
    private bool handled = false;

    // initialize the area trigger component
    void Start()
    {
        // ensure the collider is set as a trigger so it can detect the player entering the area
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
            Debug.LogWarning("[AreaTrigger] '" + name + "': collider is not a trigger. Tick Is Trigger.", this);

        if (transform.lossyScale != Vector3.one)
            Debug.LogWarning("[AreaTrigger] '" + name + "': scale is " + transform.lossyScale + ". Large scales make the zone bigger than it looks.", this);
    }

    // handle player entering, staying, and exiting the area
    void OnTriggerEnter(Collider other) { Handle(other); }
    void OnTriggerStay(Collider other)  { Handle(other); }

    // handle player exiting the area
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            handled = false;
    }

    // handle the logic above
    private void Handle(Collider other)
    {
        if (handled) return;
        if (!other.CompareTag("Player")) return;

        if (WaveManager.Instance == null)
        {
            Debug.LogError("[AreaTrigger] WaveManager.Instance is null.", this);
            return;
        }

        if (!WaveManager.Instance.IsAwaitingArea(levelIndex))
            return;

        handled = true;
        if (debugLogs)
            Debug.Log("[AreaTrigger] '" + name + "' entered by '" + other.name + "' (level " + levelIndex + ")", this);

        foreach (Door d in doorsToClose)
        {
            if (d == null)
                Debug.LogWarning("[AreaTrigger] '" + name + "' has an empty slot in Doors To Close.", this);
            else
                d.Close();
        }

        WaveManager.Instance.PlayerEnteredArea(levelIndex);
    }

    // reset the collider to be a trigger
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}