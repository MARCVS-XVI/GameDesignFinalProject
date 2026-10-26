using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// TEMPORARY player movement for testing the WaveManager. 
// REMOVE when implementing real player movement
[RequireComponent(typeof(Rigidbody))]
public class TempPlayerMove : MonoBehaviour
{
    public float moveSpeed = 6f;

    private Rigidbody rb;
    private Vector3 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        moveInput = GetMoveInput();

        if (moveInput != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(moveInput);
        }
    }

    void FixedUpdate()
    {
        Vector3 velocity = moveInput * moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private Vector3 GetMoveInput()
    {
        float x = 0f;
        float z = 0f;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector3.zero;

        if (keyboard.dKey.isPressed) x += 1f;
        if (keyboard.aKey.isPressed) x -= 1f;
        if (keyboard.wKey.isPressed) z += 1f;
        if (keyboard.sKey.isPressed) z -= 1f;
#else
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
#endif

        return new Vector3(x, 0f, z).normalized;
    }
}