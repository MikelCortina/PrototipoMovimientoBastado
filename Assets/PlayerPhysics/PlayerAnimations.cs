using Fusion;
using UnityEngine;

public class PlayerAnimations : NetworkBehaviour
{
    private FusionMovement moveScript;
    private Animator anim;
    private Rigidbody rb;

    private bool wasWallRunningLastFrame = false;

    // --- SOLUCIÓN: VELOCIDAD VISUAL PARA LOS CLONES ---
    private Vector3 lastPosition;
    private Vector3 visualVelocity;

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
        moveScript = GetComponent<FusionMovement>();
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
    }

    public override void Spawned()
    {
        // Guardamos la posición inicial al aparecer en la red
        lastPosition = transform.position;
    }

    // Usamos Render en lugar de Update para animaciones suaves en Fusion
    public override void Render()
    {
        if (!Object || !Object.IsValid || anim == null) return;

        // 1. Calculamos la velocidad basándonos en el movimiento real del objeto.
        // Esto salva la vida en multiplayer porque los clones no tienen velocidad en su Rigidbody.
        if (Time.deltaTime > 0)
        {
            visualVelocity = (transform.position - lastPosition) / Time.deltaTime;
            lastPosition = transform.position;
        }

        UpdateGroundMovement();
        UpdateAirState();
    }

    void UpdateGroundMovement()
    {
        bool isMine = Object.HasInputAuthority || (Runner.Topology == Topologies.Shared && Object.HasStateAuthority);

        // El dueño lee su Rigidbody real (más preciso). Los clones leen la velocidad visual.
        Vector3 velocityToUse = isMine ? rb.linearVelocity : visualVelocity;

        // Calculamos velocidad local
        Vector3 localVelocity = transform.InverseTransformDirection(velocityToUse);

        float targetHorizontal = localVelocity.x;
        float targetVertical = localVelocity.z;

        // --- MEJORA PARA IDLE ---
        float stopThreshold = 0.5f;

        // Nos guiamos principalmente por la velocidad real (localVelocity) en lugar del input
        // para asegurar que la animación coincida perfectamente con el movimiento visual en red.
        if (localVelocity.magnitude < stopThreshold)
        {
            targetHorizontal = 0f;
            targetVertical = 0f;

            anim.SetFloat(horizontalParam, 0f, 0.05f, Time.deltaTime);
            anim.SetFloat(verticalParam, 0f, 0.05f, Time.deltaTime);
        }
        else
        {
            anim.SetFloat(horizontalParam, targetHorizontal, 0.1f, Time.deltaTime);
            anim.SetFloat(verticalParam, targetVertical, 0.1f, Time.deltaTime);
        }
    }

    void UpdateAirState()
    {
        bool isMine = Object.HasInputAuthority || (Runner.Topology == Topologies.Shared && Object.HasStateAuthority);
        Vector3 velocityToUse = isMine ? rb.linearVelocity : visualVelocity;

        bool isGrounded = moveScript.grounded; // Esta variable sí viaja por la red correctamente
        anim.SetBool(isGroundedParam, isGrounded);

        // ¡AQUÍ ESTABA EL ERROR DEL SALTO! Ahora los clones leen la caída correctamente.
        anim.SetFloat(yVelocityParam, velocityToUse.y);

        UpdateWallRunAnimations();
    }

    void UpdateWallRunAnimations()
    {
        bool wallRunning = moveScript.IsWallRunning;
        int side = moveScript.WallRunSide;

        if (showDebugLogs)
        {
            if (wallRunning && !wasWallRunningLastFrame)
            {
                string lado = (side == -1) ? "IZQUIERDA" : (side == 1 ? "DERECHA" : "DESCONOCIDO");
                Debug.Log($"<color=green><b>[WALLRUN START]</b></color> Estado: {wallRunning} | Lado: {lado} ({side})");
            }
            else if (!wallRunning && wasWallRunningLastFrame)
            {
                Debug.Log("<color=red><b>[WALLRUN END]</b></color> El bool 'isWallRunning' ahora es FALSE.");
            }
        }

        wasWallRunningLastFrame = wallRunning;

        if (!wallRunning)
        {
            anim.SetBool("wallRunLeft", false);
            anim.SetBool("wallRunRight", false);
            return;
        }

        anim.SetBool("wallRunLeft", side == -1);
        anim.SetBool("wallRunRight", side == 1);
    }

    // -----------------------------
    // TRIGGERS SINCRONIZADOS (RPCs)
    // -----------------------------
    public void TriggerJump()
    {
        if (Object.HasInputAuthority || Object.HasStateAuthority)
            RPC_TriggerJump();
    }

    public void TriggerDash(float x, float y)
    {
        if (Object.HasInputAuthority || Object.HasStateAuthority)
            RPC_TriggerDash(x, y);
    }

    public void TriggerFallDash()
    {
        if (Object.HasInputAuthority || Object.HasStateAuthority)
            RPC_TriggerFallDash();
    }

    public void SetShooting(bool value)
    {
        if (Object.HasInputAuthority || Object.HasStateAuthority)
            RPC_SetShooting(value);
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerJump()
    {
        if (anim == null) return;
        anim.ResetTrigger(jumpTrigger);
        anim.SetTrigger(jumpTrigger);
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerDash(float x, float y)
    {
        if (anim == null) return;
        anim.SetFloat(horizontalParam, x);
        anim.SetFloat(verticalParam, y);
        anim.ResetTrigger(dashTrigger);
        anim.SetTrigger(dashTrigger);
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerFallDash()
    {
        if (anim == null) return;
        anim.ResetTrigger(FallDashTrigger);
        anim.SetTrigger(FallDashTrigger);
    }

    [Rpc(RpcSources.InputAuthority | RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetShooting(NetworkBool value)
    {
        if (anim == null) return;
        anim.SetBool(isShootingParam, value);
    }
}