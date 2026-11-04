using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string openParameter = "Open";
    [SerializeField] private AudioSource openSound;
    [SerializeField] private GameObject blocker;

    public bool IsOpen { get; private set; }

    // initialize the door component
    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("[Door] '" + name + "' has no Animator. Open() will throw an error.", this);
            return;
        }

        if (animator.runtimeAnimatorController == null)
            Debug.LogError("[Door] '" + name + "' has an Animator with no Controller assigned.", this);
        else if (!HasParameter(openParameter))
            Debug.LogError("[Door] '" + name + "': controller has no bool parameter '" + openParameter + "'.", this);

        if (blocker == null)
            Debug.Log("[Door] '" + name + "' has no blocker assigned (optional).", this);
    }

    // check if the animator has a specific parameter
    private bool HasParameter(string paramName)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == paramName) return true;
        return false;
    }

    // open the door
    public void Open()
    {
        Debug.Log("[Door] Open called on " + name + " (already open: " + IsOpen + ")", this);
        if (IsOpen) return;
        IsOpen = true;

        if (blocker != null)
        {
            blocker.SetActive(false);
            Debug.Log("[Door] '" + name + "': blocker '" + blocker.name + "' switched off", this);
        }

        if (animator != null)
            animator.SetBool(openParameter, true);

        if (openSound != null)
            openSound.Play();
    }

    // close the door
    public void Close()
    {
        if (!IsOpen)
        {
            Debug.Log("[Door] Close called on " + name + " but it isn't open, ignoring", this);
            return;
        }
        IsOpen = false;
        Debug.Log("[Door] Closing " + name, this);

        if (blocker != null)
        {
            blocker.SetActive(true);
            Debug.Log("[Door] '" + name + "': blocker '" + blocker.name + "' switched on", this);
        }

        if (animator != null)
            animator.SetBool(openParameter, false);
    }
}