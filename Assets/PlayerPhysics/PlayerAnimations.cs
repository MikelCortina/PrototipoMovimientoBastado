using UnityEngine;
using Photon.Pun;

public class PlayerAnimations : MonoBehaviourPun
{
    private Movement moveScript;
    private Animator anim;
    private Rigidbody rb;

    // Nombres de los parámetros en tu Animator Controller
    [Header("Animation Parameters")]
    public string horizontalParam = "Horizontal";
    public string verticalParam = "Vertical";
    public string isGroundedParam = "isGrounded";
    public string isWallRunningParam = "isWallRunning";
    public string jumpTrigger = "Jump";
    public string dashTrigger = "Dash";

    void Awake()
    {
        moveScript = GetComponent<Movement>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Solo el dueño del personaje calcula las animaciones
        if (!photonView.IsMine && PhotonNetwork.IsConnected) return;

        UpdateMovementAnimations();
    }

    void UpdateMovementAnimations()
    {
        // 1. Movimiento Básico (Blend Tree)
        // Usamos la velocidad local para que el Animator sepa si vamos adelante, atrás o a los lados
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        anim.SetFloat(horizontalParam, localVelocity.x, 0.1f, Time.deltaTime);
        anim.SetFloat(verticalParam, localVelocity.z, 0.1f, Time.deltaTime);

        // 2. Estados de Aire y Pared
        anim.SetBool(isGroundedParam, moveScript.grounded);

        // Accedemos a la variable privada wallRunning mediante una pequeña modificación o reflexión
        // Pero como en tu script es privada, lo ideal es que la pongas pública o hagas un Getter.
        // Asumiendo que detectamos Wallrun por la lógica de tu script:
        bool isWallRunning = !moveScript.grounded && moveScript.rb.useGravity == false; // O simplificar según tu lógica
        anim.SetBool(isWallRunningParam, isWallRunning);
    }

    // --- Sincronización de Triggers vía RPC ---
    // Los Triggers de Animator a veces fallan en Photon si no se envían explícitamente

    public void TriggerJump()
    {
        if (photonView.IsMine)
            photonView.RPC("RPC_TriggerJump", RpcTarget.All);
    }

    public void TriggerDash()
    {
        if (photonView.IsMine)
            photonView.RPC("RPC_TriggerDash", RpcTarget.All);
    }

    [PunRPC]
    void RPC_TriggerJump() => anim.SetTrigger(jumpTrigger);

    [PunRPC]
    void RPC_TriggerDash() => anim.SetTrigger(dashTrigger);
}