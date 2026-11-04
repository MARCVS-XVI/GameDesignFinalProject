using UnityEngine;

// Put on the door parent (the object with the Animator).
// Animator drives the animation; a separate static blocker handles solidity.
public class Door : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string openParameter = "Open";
    [SerializeField] private AudioSource openSound;   // optional
    [SerializeField] private GameObject blocker;      // solid wall in the doorway (optional)

    public bool IsOpen { get; private set; }

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (blocker != null)
            blocker.SetActive(false);

        animator.SetBool(openParameter, true);

        if (openSound != null)
            openSound.Play();
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (blocker != null)
            blocker.SetActive(true);

        animator.SetBool(openParameter, false);
    }
}