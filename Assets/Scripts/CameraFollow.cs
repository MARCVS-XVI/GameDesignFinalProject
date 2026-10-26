using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; 
    public Vector3 offset = new Vector3(0, 10, -8); 

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;
        transform.LookAt(target.position);
    }
}