using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 120f;
    public Transform playerBody;

    float xRotation;
    float yRotation;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // Agrega esto: Si hay un Rigidbody, dile que su rotación es esta ahora mismo
        if (playerBody.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.rotation = playerBody.rotation;
        }
    }

 
}
