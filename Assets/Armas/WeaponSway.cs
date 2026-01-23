using UnityEngine;

public class WeaponSway : MonoBehaviour
{
    [Header("Position Settings")]
    public float amount = 0.02f;
    public float smoothAmount = 0.06f;
    public float maxAmount = 6f;

    [Header("Rotation Settings (Mouse)")]
    public float rotationAmount = 5f;
    public float rotationSmoothIn = 12f;
    public float rotationSmoothOut = 6f;
    public float maxRotationAmount = 10f;

    [Header("Vertical Tilt (Jump/Fall)")]
    [Tooltip("Sensibilidad al subir (orienta arma hacia abajo)")]
    public float tiltOnAscending = 2.0f;
    [Tooltip("Sensibilidad al bajar (orienta arma hacia arriba)")]
    public float tiltOnDescending = 4.0f;
    [Space]
    public float maxAscendTilt = 15f;
    public float maxDescendTilt = 25f;

    [Header("Horizontal & Roll Tilt (Strafe)")]
    [Tooltip("Rotación en Y al moverse lateralmente")]
    public float tiltOnStrafeY = 2.0f;
    [Tooltip("Inclinación en Z (Roll) al moverse lateralmente")]
    public float tiltOnStrafeZ = 5.0f;
    [Space]
    public float maxStrafeTiltY = 10f;
    public float maxStrafeTiltZ = 15f;

    [Header("Smooth Settings")]
    [Tooltip("Respuesta base del tilt por movimiento")]
    public float tiltSmooth = 12f;
    [Tooltip("Multiplicador de velocidad cuando el arma vuelve al centro")]
    public float returnSpeedMultiplier = 6f;

    [Header("References")]
    public Movement playerController;

    [Space]
    public bool rotationX = true;
    public bool rotationY = true;
    public bool rotationZ = true;

    // Variables privadas
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float inputX;
    private float inputY;

    // Referencias para el SmoothDamp
    private float currentVelocityTiltX;
    private float tiltVelocityRefX;

    private float currentVelocityTiltY;
    private float tiltVelocityRefY;

    private float currentVelocityTiltZ;
    private float tiltVelocityRefZ;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        if (playerController == null)
            playerController = GetComponentInParent<Movement>();
    }

    void LateUpdate()
    {
        CalculateInput();
        UpdateVerticalTilt();   // Inercia Salto/Caída (Eje X)
        UpdateMovementTilt();   // Inercia Movimiento Lateral (Ejes Y y Z)
        MoveSway();
        TiltSway();
    }

    private void CalculateInput()
    {
        inputX = -Input.GetAxis("Mouse X");
        inputY = -Input.GetAxis("Mouse Y");
    }

    private void UpdateVerticalTilt()
    {
        if (playerController == null || playerController.rb == null) return;

        float vy = playerController.rb.linearVelocity.y;
        float targetTilt = 0f;

        if (vy > 0.1f)
            targetTilt = Mathf.Clamp(vy * tiltOnAscending, 0f, maxAscendTilt);
        else if (vy < -0.1f)
            targetTilt = Mathf.Clamp(vy * tiltOnDescending, -maxDescendTilt, 0f);

        float smoothTime = 1f / tiltSmooth;
        if (Mathf.Abs(targetTilt) < 0.05f) smoothTime /= returnSpeedMultiplier;
        if (playerController.justLanded) smoothTime = 0.05f;

        currentVelocityTiltX = Mathf.SmoothDamp(currentVelocityTiltX, targetTilt, ref tiltVelocityRefX, smoothTime);
    }

    private void UpdateMovementTilt()
    {
        if (playerController == null || playerController.rb == null) return;

        // Velocidad local del jugador
        Vector3 localVelocity = playerController.transform.InverseTransformDirection(playerController.rb.linearVelocity);
        float vx = localVelocity.x;

        // TARGETS
        // Y: El arma "mira" hacia el lado contrario del movimiento (inercia)
        float targetTiltY = -vx * tiltOnStrafeY;
        targetTiltY = Mathf.Clamp(targetTiltY, -maxStrafeTiltY, maxStrafeTiltY);

        // Z: El arma se inclina lateralmente (roll)
        float targetTiltZ = -vx * tiltOnStrafeZ;
        targetTiltZ = Mathf.Clamp(targetTiltZ, -maxStrafeTiltZ, maxStrafeTiltZ);

        float smoothTime = 1f / tiltSmooth;
        if (Mathf.Abs(vx) < 0.1f) smoothTime /= returnSpeedMultiplier;

        // Aplicar suavizado
        currentVelocityTiltY = Mathf.SmoothDamp(currentVelocityTiltY, targetTiltY, ref tiltVelocityRefY, smoothTime);
        currentVelocityTiltZ = Mathf.SmoothDamp(currentVelocityTiltZ, targetTiltZ, ref tiltVelocityRefZ, smoothTime);
    }

    private void MoveSway()
    {
        float moveY = Mathf.Clamp(inputY * amount, -maxAmount, maxAmount);
        Vector3 finalPosition = new Vector3(0f, moveY, 0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            finalPosition + initialPosition,
            Time.deltaTime * smoothAmount
        );
    }

    private void TiltSway()
    {
        // Sway del ratón
        float mouseTiltY = Mathf.Clamp(inputX * rotationAmount, -maxRotationAmount, maxRotationAmount);
        float mouseTiltX = Mathf.Clamp(inputY * rotationAmount, -maxRotationAmount, maxRotationAmount);

        // Combinación final de fuerzas
        float finalTiltX = -mouseTiltX + currentVelocityTiltX;
        float finalTiltY = mouseTiltY + currentVelocityTiltY;
        float finalTiltZ = mouseTiltY + currentVelocityTiltZ; // El ratón también afecta levemente al Z

        Quaternion finalRotation = Quaternion.Euler(new Vector3(
            rotationX ? finalTiltX : 0f,
            rotationY ? finalTiltY : 0f,
            rotationZ ? finalTiltZ : 0f
        ));

        float smooth = (Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f)
            ? rotationSmoothIn
            : rotationSmoothOut;

        if (playerController != null && playerController.justLanded)
            smooth *= 4f;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            finalRotation * initialRotation,
            Time.deltaTime * smooth
        );
    }
}