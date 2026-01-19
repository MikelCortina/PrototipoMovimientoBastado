using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DynamicSpeedFOV : MonoBehaviour
{
    [Header("Referencias")]
    public Rigidbody playerRb;
    public Transform playerTransform; // <-- Necesitamos la orientación del jugador

    [Header("FOV")]
    public float baseFOV = 75f;
    public float maxFOV = 100f;

    [Header("Velocidad")]
    public float speedThreshold = 15f;
    public float maxSpeedForMaxFOV = 35f;

    [Header("Suavizado")]
    public float fovLerpSpeed = 6f;

    [Header("FOV Kick")]
    public float kickDecaySpeed = 25f; // qué rápido baja el extra

    float extraFOV;
    Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = baseFOV;

        if (!playerTransform && playerRb)
            playerTransform = playerRb.transform; // fallback
    }

    void Update()
    {
        if (!playerRb || !playerTransform) return;

        // Velocidad horizontal
        Vector3 vel = playerRb.linearVelocity;
        vel.y = 0f;

        // Proyección de la velocidad sobre el eje forward del jugador
        float forwardSpeed = Vector3.Dot(vel, playerTransform.forward);

        float targetFOV = baseFOV;

        // Solo aplicamos el FOV extra si la velocidad hacia adelante o atrás supera el umbral
        if (Mathf.Abs(forwardSpeed) > speedThreshold)
        {
            float t = Mathf.InverseLerp(
                speedThreshold,
                maxSpeedForMaxFOV,
                Mathf.Abs(forwardSpeed)
            );

            targetFOV = Mathf.Lerp(baseFOV, maxFOV, t);
        }

        // Decaimiento rápido del kick
        extraFOV = Mathf.MoveTowards(
            extraFOV,
            0f,
            kickDecaySpeed * Time.deltaTime
        );

        float finalFOV = targetFOV + extraFOV;

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            finalFOV,
            fovLerpSpeed * Time.deltaTime
        );
    }

    // 🔥 Llamar desde walljump / bhop
    public void AddFOVKick(float amount)
    {
        extraFOV += amount;
    }
}
