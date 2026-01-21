using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DynamicSpeedFOV : MonoBehaviour
{
    [Header("Referencias")]
    public Rigidbody playerRb;
    public Transform playerTransform;

    [Header("FOV")]
    public float baseFOV = 75f;
    public float maxFOV = 100f;

    [Header("Velocidad")]
    public float speedThreshold = 15f;
    public float maxSpeedForMaxFOV = 35f;

    [Header("Suavizado")]
    public float fovLerpSpeed = 6f;

    [Header("FOV Kick")]
    public float kickDecaySpeed = 25f;

    [Header("Tilt (Ladeo de pared)")]
    public float tiltAngle = 5f;
    public float tiltLerpSpeed = 8f;

    [Header("Wall Jump Shake")]
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.1f;

    private float extraFOV;
    private float currentTilt;
    private float targetTilt;
    private float currentShakeTime;
    private Vector3 shakeOffset;
    private Camera cam;
    private Vector3 initialLocalPosition; // Nueva variable

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = baseFOV;

        // Guardamos la posición original (la altura de los ojos)
        initialLocalPosition = transform.localPosition;

        if (!playerTransform && playerRb)
            playerTransform = playerRb.transform;
    }

    void Update()
    {
        if (!playerRb || !playerTransform) return;

        HandleFOV();
        HandleTilt();
        HandleShake();

        // Aplicamos el shake relativo a la posición inicial, no a cero
        cam.transform.localPosition = initialLocalPosition + shakeOffset;
    }

    void HandleFOV()
    {
        Vector3 vel = playerRb.linearVelocity;
        vel.y = 0f;

        float forwardSpeed = Vector3.Dot(vel, playerTransform.forward);
        float targetFOV = baseFOV;

        if (Mathf.Abs(forwardSpeed) > speedThreshold)
        {
            float t = Mathf.InverseLerp(speedThreshold, maxSpeedForMaxFOV, Mathf.Abs(forwardSpeed));
            targetFOV = Mathf.Lerp(baseFOV, maxFOV, t);
        }

        extraFOV = Mathf.MoveTowards(extraFOV, 0f, kickDecaySpeed * Time.deltaTime);
        float finalFOV = targetFOV + extraFOV;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, finalFOV, fovLerpSpeed * Time.deltaTime);
    }

    void HandleTilt()
    {
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltLerpSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(transform.localEulerAngles.x, transform.localEulerAngles.y, currentTilt);
    }

    void HandleShake()
    {
        if (currentShakeTime > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            currentShakeTime -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    public void AddFOVKick(float amount) => extraFOV += amount;

    public void TriggerWallJumpShake() => currentShakeTime = shakeDuration;

    public void SetWallTilt(Vector3 wallNormal)
    {
        Vector3 right = playerTransform.right;
        float side = Vector3.Dot(right, wallNormal);
        targetTilt = (side > 0) ? tiltAngle : -tiltAngle;
    }

    public void ResetTilt() => targetTilt = 0f;
}