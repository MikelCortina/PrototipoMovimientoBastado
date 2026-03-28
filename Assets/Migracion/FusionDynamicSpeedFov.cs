using UnityEngine;
using Fusion; // Necesitamos Fusion para comprobar InputAuthority

[RequireComponent(typeof(Camera))]
public class FusionDynamicSpeedFOV : MonoBehaviour
{
    [Header("Referencias")]
    public Rigidbody playerRb;
    public Transform playerTransform;
    // NUEVO: Referencia al NetworkObject del jugador para saber si somos dueños
    public NetworkObject playerNetworkObject;

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
    private Vector3 initialLocalPosition;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = baseFOV;
        initialLocalPosition = transform.localPosition;

        if (!playerTransform && playerRb)
            playerTransform = playerRb.transform;

        // Si no se asignó el NetworkObject en el inspector, intentamos buscarlo en el padre
        if (playerNetworkObject == null)
            playerNetworkObject = GetComponentInParent<NetworkObject>();
    }

    void Update()
    {
        // 1. SEGURIDAD VITAL PARA FUSION: 
        // Si no tenemos el objeto, o si el juego no ha empezado, o si ESTA CÁMARA NO ES LA NUESTRA, salimos.
        if (!playerRb || !playerTransform || playerNetworkObject == null || !playerNetworkObject.IsValid || !playerNetworkObject.HasInputAuthority)
            return;

        HandleFOV();
        HandleTilt();
        HandleShake();

        cam.transform.localPosition = initialLocalPosition + shakeOffset;
    }

    void HandleFOV()
    {
        // En Fusion, es mejor usar la velocidad horizontal pura
        Vector3 vel = playerRb.linearVelocity;
        vel.y = 0f;
        float forwardSpeed = vel.magnitude; // Simplificado para que tome la velocidad total

        float targetFOV = baseFOV;

        if (forwardSpeed > speedThreshold)
        {
            float t = Mathf.InverseLerp(speedThreshold, maxSpeedForMaxFOV, forwardSpeed);
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