using UnityEngine;
using Photon.Pun;

public class PlayerAnimations : MonoBehaviourPun
{
    private Movement moveScript;
    private Animator anim;
    private Rigidbody rb;

    // Control para detectar cambios de estado (evita spam en consola)
    private bool wasWallRunningLastFrame = false;

    [Header("Debug Settings")]
    public bool showDebugLogs = true;

    [Header("Animator Parameters")]
    public string horizontalParam = "Horizontal";
    public string verticalParam = "Vertical";
    public string isGroundedParam = "isGrounded";
    public string isWallRunningParam = "isWallRunning";
    public string jumpTrigger = "Jump";
    public string dashTrigger = "Dash";
    public string FallDashTrigger = "FallDash";
    public string yVelocityParam = "YVelocity";

    public string isShootingParam = "isShooting";


    void Awake()
    {
        moveScript = GetComponent<Movement>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Solo el dueño del personaje calcula y setea los parámetros en SU animador.
        if (!photonView.IsMine && PhotonNetwork.IsConnected)
            return;

        UpdateGroundMovement();
        UpdateAirState();
      
    }

    void UpdateGroundMovement()
    {
        // 1. Calculamos velocidad local
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        // 2. Obtenemos el input del script de movimiento para saber si el jugador QUIERE moverse
        // Esto es mucho más fiable que solo usar la velocidad del Rigidbody
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        float targetHorizontal = localVelocity.x;
        float targetVertical = localVelocity.z;

        // --- MEJORA PARA IDLE ---
        // Si no hay input y la velocidad es baja, forzamos CERO absoluto
        float stopThreshold = 0.5f; // Bajamos de 3f a 0.5f
        if (input.sqrMagnitude < 0.01f && localVelocity.magnitude < stopThreshold)
        {
            targetHorizontal = 0f;
            targetVertical = 0f;

            // Usamos un DampTime más pequeño para que el Idle entre rápido
            anim.SetFloat(horizontalParam, 0f, 0.05f, Time.deltaTime);
            anim.SetFloat(verticalParam, 0f, 0.05f, Time.deltaTime);
        }
        else
        {
            // Si nos estamos moviendo, usamos el suavizado normal
            anim.SetFloat(horizontalParam, targetHorizontal, 0.1f, Time.deltaTime);
            anim.SetFloat(verticalParam, targetVertical, 0.1f, Time.deltaTime);
        }
    }
    void UpdateAirState()
    {
        bool isGrounded = moveScript.grounded;
        anim.SetBool(isGroundedParam, isGrounded);
        anim.SetFloat(yVelocityParam, rb.linearVelocity.y);

        UpdateWallRunAnimations();
    }

    void UpdateWallRunAnimations()
    {
        bool wallRunning = moveScript.IsWallRunning;
        int side = moveScript.WallRunSide;

        // --- SISTEMA DE DEBUG ---
        if (showDebugLogs)
        {
            // Detectar cuando el bool PASA A SER TRUE
            if (wallRunning && !wasWallRunningLastFrame)
            {
                string lado = (side == -1) ? "IZQUIERDA" : (side == 1 ? "DERECHA" : "DESCONOCIDO");
                Debug.Log($"<color=green><b>[WALLRUN START]</b></color> Estado: {wallRunning} | Lado: {lado} ({side})");
            }
            // Detectar cuando el bool PASA A SER FALSE
            else if (!wallRunning && wasWallRunningLastFrame)
            {
                Debug.Log("<color=red><b>[WALLRUN END]</b></color> El bool 'isWallRunning' ahora es FALSE.");
            }
        }

        // Guardamos el estado para comparar en el siguiente frame
        wasWallRunningLastFrame = wallRunning;
        // -------------------------

      

        if (!wallRunning)
        {
            anim.SetBool("wallRunLeft", false);
            anim.SetBool("wallRunRight", false);
            return;
        }

        // Seteamos los bools específicos de lado
        anim.SetBool("wallRunLeft", side == -1);
        anim.SetBool("wallRunRight", side == 1);
    }

    // -----------------------------
    // TRIGGERS SINCRONIZADOS (RPCs)
    // -----------------------------
    public void TriggerJump()
    {
        if (photonView.IsMine)
            photonView.RPC(nameof(RPC_TriggerJump), RpcTarget.All);
    }

    public void TriggerDash(float x, float y)
    {
        if (photonView.IsMine)
            photonView.RPC(nameof(RPC_TriggerDash), RpcTarget.All, x, y);
    }

    public void TriggerFallDash()
    {
        if (photonView.IsMine)
            photonView.RPC(nameof(RPC_TriggerDash), RpcTarget.All);
    }


    [PunRPC]
    void RPC_TriggerJump()
    {
        if (anim == null) return;
        // Forzamos la limpieza por si acaso quedó uno atascado de un lag spike
        anim.ResetTrigger(jumpTrigger);
        anim.SetTrigger(jumpTrigger);
    }

    [PunRPC]
    void RPC_TriggerDash(float x, float y)
    {
        if (anim == null) return;

        // Seteamos la dirección exacta del dash ANTES del trigger
        anim.SetFloat(horizontalParam, x);
        anim.SetFloat(verticalParam, y);

        anim.ResetTrigger(dashTrigger);
        anim.SetTrigger(dashTrigger);
    }


    [PunRPC]
    void RPC_TriggerFallDash()
    {
        if (anim == null) return;
        anim.ResetTrigger(FallDashTrigger);
        anim.SetTrigger(FallDashTrigger);
    }

    public void SetShooting(bool value)
    {
        if (!photonView.IsMine) return;

        photonView.RPC(nameof(RPC_SetShooting), RpcTarget.All, value);
    }

    [PunRPC]
    void RPC_SetShooting(bool value)
    {
        if (anim == null) return;
        anim.SetBool(isShootingParam, value);
    }
}