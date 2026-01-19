using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeCamera : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 10f;         // Velocidad base
    public float sprintMultiplier = 2f;   // Multiplicador al mantener Shift
    public float acceleration = 10f;      // Qué tan rápido alcanza la velocidad máxima
    public float deceleration = 15f;      // Qué tan rápido se frena

    [Header("Rotación")]
    public float mouseSensitivity = 2f;   // Sensibilidad del ratón
    public bool invertY = false;

    private Vector3 currentVelocity = Vector3.zero; // Para suavizar movimiento
    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; // Bloquea el cursor
        Cursor.visible = false;

        // Inicializamos rotación con la orientación actual
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch += invertY ? mouseY : -mouseY;
        pitch = Mathf.Clamp(pitch, -89f, 89f); // Evita rotación completa vertical

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandleMovement()
    {
        // Entrada del teclado
        Vector3 input = new Vector3(
            Input.GetAxisRaw("Horizontal"), // A/D
          //  Input.GetAxisRaw("Jump") - Input.GetAxisRaw("Crouch"), // Espacio/Control opcional
            Input.GetAxisRaw("Vertical")   // W/S
        );

        // Normaliza para no ir más rápido en diagonales
        Vector3 targetVelocity = input.normalized * moveSpeed;

        // Sprint
        if (Input.GetKey(KeyCode.LeftShift))
            targetVelocity *= sprintMultiplier;

        // Convierte del espacio local al global
        targetVelocity = transform.TransformDirection(targetVelocity);

        // Suavizado de movimiento
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);

        transform.position += currentVelocity * Time.deltaTime;

        // Deceleración extra si no se presiona ninguna tecla
        if (input.magnitude < 0.1f)
            currentVelocity = Vector3.MoveTowards(currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
    }
}
