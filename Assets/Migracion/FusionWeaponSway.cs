using UnityEngine;
using Fusion; // <-- Necesario para comprobar la autoridad

public class FusionWeaponSway : MonoBehaviour
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
    public float tiltOnAscending = 2.0f;
    public float tiltOnDescending = 4.0f;
    public float maxAscendTilt = 15f;
    public float maxDescendTilt = 25f;

    [Header("Horizontal & Roll Tilt (Strafe)")]
    public float tiltOnStrafeY = 2.0f;
    public float tiltOnStrafeZ = 5.0f;
    public float maxStrafeTiltY = 10f;
    public float maxStrafeTiltZ = 15f;

    [Header("Smooth Settings")]
    public float tiltSmooth = 12f;
    public float returnSpeedMultiplier = 6f;

    [Header("References")]
    public FusionMovement playerController; // <-- CAMBIADO A FusionMovement

    [Space]
    public bool rotationX = true;
    public bool rotationY = true;
    public bool rotationZ = true;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private float inputX;
    private float inputY;

    private float currentVelocityTiltX, tiltVelocityRefX;
    private float currentVelocityTiltY, tiltVelocityRefY;
    private float currentVelocityTiltZ, tiltVelocityRefZ;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;

        if (playerController == null)
            playerController = GetComponentInParent<FusionMovement>();
    }

    void LateUpdate()
    {
        // SEGURIDAD: Solo aplicamos sway si somos el jugador local
        if (playerController == null || !playerController.HasInputAuthority)
            return;

        CalculateInput();
        UpdateVerticalTilt();
        UpdateMovementTilt();
        MoveSway();
        TiltSway();
    }

    private void CalculateInput()
    {
        // Al ser un efecto 100% visual y local, Input.GetAxis está bien aquí.
        inputX = -Input.GetAxis("Mouse X");
        inputY = -Input.GetAxis("Mouse Y");
    }

    private void UpdateVerticalTilt()
    {
        if (playerController.GetComponent<Rigidbody>() == null) return;

        float vy = playerController.GetComponent<Rigidbody>().linearVelocity.y;
        float targetTilt = 0f;

        if (vy > 0.1f) targetTilt = Mathf.Clamp(vy * tiltOnAscending, 0f, maxAscendTilt);
        else if (vy < -0.1f) targetTilt = Mathf.Clamp(vy * tiltOnDescending, -maxDescendTilt, 0f);

        float smoothTime = 1f / tiltSmooth;
        if (Mathf.Abs(targetTilt) < 0.05f) smoothTime /= returnSpeedMultiplier;
        if (playerController.justLanded) smoothTime = 0.05f;

        currentVelocityTiltX = Mathf.SmoothDamp(currentVelocityTiltX, targetTilt, ref tiltVelocityRefX, smoothTime);
    }

    private void UpdateMovementTilt()
    {
        if (playerController.GetComponent<Rigidbody>() == null) return;

        Vector3 localVelocity = playerController.transform.InverseTransformDirection(playerController.GetComponent<Rigidbody>().linearVelocity);
        float vx = localVelocity.x;

        float targetTiltY = Mathf.Clamp(-vx * tiltOnStrafeY, -maxStrafeTiltY, maxStrafeTiltY);
        float targetTiltZ = Mathf.Clamp(-vx * tiltOnStrafeZ, -maxStrafeTiltZ, maxStrafeTiltZ);

        float smoothTime = 1f / tiltSmooth;
        if (Mathf.Abs(vx) < 0.1f) smoothTime /= returnSpeedMultiplier;

        currentVelocityTiltY = Mathf.SmoothDamp(currentVelocityTiltY, targetTiltY, ref tiltVelocityRefY, smoothTime);
        currentVelocityTiltZ = Mathf.SmoothDamp(currentVelocityTiltZ, targetTiltZ, ref tiltVelocityRefZ, smoothTime);
    }

    private void MoveSway()
    {
        float moveY = Mathf.Clamp(inputY * amount, -maxAmount, maxAmount);
        Vector3 finalPosition = new Vector3(0f, moveY, 0f);

        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosition + initialPosition, Time.deltaTime * smoothAmount);
    }

    private void TiltSway()
    {
        float mouseTiltY = Mathf.Clamp(inputX * rotationAmount, -maxRotationAmount, maxRotationAmount);
        float mouseTiltX = Mathf.Clamp(inputY * rotationAmount, -maxRotationAmount, maxRotationAmount);

        float finalTiltX = -mouseTiltX + currentVelocityTiltX;
        float finalTiltY = mouseTiltY + currentVelocityTiltY;
        float finalTiltZ = mouseTiltY + currentVelocityTiltZ;

        Quaternion finalRotation = Quaternion.Euler(new Vector3(
            rotationX ? finalTiltX : 0f,
            rotationY ? finalTiltY : 0f,
            rotationZ ? finalTiltZ : 0f
        ));

        float smooth = (Mathf.Abs(inputX) > 0.01f || Mathf.Abs(inputY) > 0.01f) ? rotationSmoothIn : rotationSmoothOut;
        if (playerController.justLanded) smooth *= 4f;

        transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRotation * initialRotation, Time.deltaTime * smooth);
    }
}