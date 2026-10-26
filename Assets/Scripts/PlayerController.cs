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
    [Header("Player Attack")]
    [SerializeField] private GameObject gearAttack1;
    [SerializeField] private GameObject gearAttack2;
    [SerializeField] private GameObject gearAttack3;    

    private InputAction mouseDir;
    private InputAction inputMoveDir;

    private Vector2 movementDir;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        mouseDir = InputSystem.actions.FindAction("MousePos");
        inputMoveDir = InputSystem.actions.FindAction("Move");
    }

    void FaceMouse()
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

    private void Movement()
    {
        movementDir = inputMoveDir.ReadValue<Vector2>();
        if(movementDir.x == 0 && movementDir.y ==0)
        {return;}
        movementDir.Normalize();
        Vector3 playerTrans = new Vector3(movementDir.x * speed * Time.deltaTime, 0, movementDir.y * speed * Time.deltaTime);
        transform.Translate(playerTrans, Space.World);
    }


    // Update is called once per frame
    void Update()
    {
            
        FaceMouse();
        Movement();
    }
}
