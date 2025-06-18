//MouseMovement.cs
using UnityEngine;

public class MouseMovement: MonoBehaviour
{
    public float mouseSensitivity = 400f;

    float xRotate = 0f;
   
    float yRotate = 0f;

    public float minClamp = -90f;
    public float maxClamp = 90f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime * -1;

        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotate -= mouseY;
        
        xRotate = Mathf.Clamp(xRotate, minClamp, maxClamp);

        yRotate -= mouseX;

        transform.localRotation = Quaternion.Euler(xRotate, yRotate, 0f);
    }
}
