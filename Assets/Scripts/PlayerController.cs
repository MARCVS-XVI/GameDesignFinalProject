using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
public class PlayerController : MonoBehaviour
{
    [Header("Main Camera")]
    [SerializeField] private Camera mainCam;
    [Header("Player Movement")]
    [SerializeField] private float speed;
    [SerializeField] private GameObject mainWeapon;

    private InputAction mouseDir;
    private InputAction inputMoveDir;
    private Vector2 movementDir;

    [Header("Player Sword")]
    [SerializeField] private float swordWeight = 50f;
    [SerializeField] private float maxSwordInirtiaAddition = 5.0f;
    [SerializeField] private float swordInertiaDecay = 18f;

    private Quaternion prevSwordRotation;
    private Vector3 swordInertia;
    private float prevSwingSpeed;
    [Header("Player Gear")]
    [SerializeField] private GameObject gearAttack1;
    [SerializeField] private GameObject gearAttack2;
    [SerializeField] private GameObject gearAttack3;    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        mouseDir = InputSystem.actions.FindAction("MousePos");
        inputMoveDir = InputSystem.actions.FindAction("Move");

        prevSwordRotation = mainWeapon.transform.rotation;
        swordInertia = Vector3.zero;
        prevSwingSpeed = 0.0f;
    }

    //sword faces mouse
    void SwordFaceMouse()
    {
        if (Mouse.current == null || mainCam == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = mainCam.ScreenPointToRay(mousePosition);

        Plane plane = new Plane(Vector3.up, transform.position);

        if (plane.Raycast(ray, out float distance))
        {
            Vector3 target = ray.GetPoint(distance);

            if ((target - transform.position).sqrMagnitude > 0.001f)
                transform.LookAt(target);
        }
    }

    private Vector3 GetSwordWeightTrans()
    {
        float dt = Time.deltaTime;
        float swingSpeed = Quaternion.Angle(prevSwordRotation, mainWeapon.transform.rotation) * Mathf.Deg2Rad / dt;
        prevSwordRotation = mainWeapon.transform.rotation;

        // Player's forward direction 
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        
        swordInertia *= Mathf.Exp(-swordInertiaDecay * dt);

        //testing clamp version
        swingSpeed = Mathf.Clamp(swingSpeed, 0.0f, 100.0f);

        if(swingSpeed < prevSwingSpeed)
        {
    
            Vector3 addToSwordInirtia = forward * Mathf.Sin(Mathf.PI /2.0f * 0.01f*(prevSwingSpeed - swingSpeed)) * swordWeight * 10.0f * dt;
            if(addToSwordInirtia.magnitude > maxSwordInirtiaAddition)
            {
                addToSwordInirtia *= maxSwordInirtiaAddition/addToSwordInirtia.magnitude;
            }
            swordInertia += addToSwordInirtia;
        }
        // Faster swings produce a stronger forward pull.
        prevSwingSpeed = swingSpeed;
        return swordInertia;
    }

    private void Movement()
    {
        //player input movement
        movementDir = inputMoveDir.ReadValue<Vector2>();
        movementDir.Normalize();
        Vector3 playerTrans = new Vector3(movementDir.x * speed * Time.deltaTime, 0, movementDir.y * speed * Time.deltaTime);

        transform.Translate(playerTrans + GetSwordWeightTrans() * Time.deltaTime, Space.World);
    }


    // Update is called once per frame
    void Update()
    {
        //should be ordered swordFaceMouse then Movement    
        SwordFaceMouse();
        Movement();
    }
}
