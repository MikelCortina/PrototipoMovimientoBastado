using UnityEngine;
using Photon.Pun;

public class PlayerLegsController : MonoBehaviourPun
{
    [Header("Referencias")]
    [SerializeField] private Movement movementScript;
    [SerializeField] private Transform legsTransform;

    [Header("Posiciones y Rotaciones")]
    [SerializeField] private Vector3 defaultPosition = new Vector3(0, -2.0f, 0);
    [SerializeField] private Vector3 slidePosition = new Vector3(0, -0.6f, 0.4f);
    [SerializeField] private Vector3 defaultRotation = new Vector3(20, 0, 0);
    [SerializeField] private Vector3 slideRotation = new Vector3(-15, 0, 0);

    [Header("Ajustes de Velocidad de Movimiento")]
    [SerializeField] private float speedThreshold = 15f;
    [SerializeField] private float transitionSpeedIn = 12f;
    [SerializeField] private float transitionSpeedOut = 1.5f;

    private float lastSpeed;
    private bool isLegsOut; // Memoria del estado de las piernas

    void Start()
    {
        if (movementScript == null) movementScript = GetComponent<Movement>();
        lastSpeed = 0f;
    }

    void Update()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine) return;
        if (movementScript == null || legsTransform == null) return;

        // 1. Cálculo de velocidad horizontal actual
        Vector3 hVel = new Vector3(movementScript.rb.linearVelocity.x, 0, movementScript.rb.linearVelocity.z);
        float currentSpeed = hVel.magnitude;

        // 2. Lógica de Aceleración vs Inercia
        bool isAccelerating = currentSpeed > lastSpeed + 0.01f; // Pequeño margen para evitar ruido
        bool aboveThreshold = currentSpeed > speedThreshold;

        // --- LÓGICA DE ACTIVACIÓN ---
        if (movementScript.grounded && aboveThreshold)
        {
            // Si hay aceleración positiva, las sacamos
            if (isAccelerating)
            {
                isLegsOut = true;
            }
            // Si no hay aceleración pero YA estaban fuera, se mantienen (perdiendo inercia)
            // Si estaban dentro y solo estamos frenando, se quedan dentro.
        }
        else
        {
            // Si saltamos o bajamos del umbral, se guardan
            isLegsOut = false;
        }

        // 3. Selección de objetivos basada en nuestra variable de estado
        Vector3 targetPos = isLegsOut ? slidePosition : defaultPosition;
        Quaternion targetRot = Quaternion.Euler(isLegsOut ? slideRotation : defaultRotation);
        float currentMoveSpeed = isLegsOut ? transitionSpeedIn : transitionSpeedOut;

        // 4. Aplicar transformaciones
        legsTransform.localPosition = Vector3.MoveTowards(
            legsTransform.localPosition,
            targetPos,
            currentMoveSpeed * Time.deltaTime
        );

        legsTransform.localRotation = Quaternion.RotateTowards(
            legsTransform.localRotation,
            targetRot,
            150f * Time.deltaTime // Velocidad de rotación fija para limpieza visual
        );

        // 5. Guardar velocidad para el siguiente frame
        lastSpeed = currentSpeed;
    }
}