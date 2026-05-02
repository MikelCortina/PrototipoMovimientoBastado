using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FusionMovement : NetworkBehaviour
{
    [Header("FOV Effects")]
    public FusionDynamicSpeedFOV dynamicFOV;
    public float bhopFov = 9f;
    public float wallJumpFov = 12f;

    [Header("Movimiento")]
    public float maxSpeed = 20f;
    public float forwardSpeedMultiplier = 1.2f;
    public float groundAccel = 80f;
    public float airAccel = 30f;
    public float jumpForce = 12f;

    [Header("Gravedad")]
    public float gravity = 30f;

    [Header("Fricción")]
    public float groundFriction = 4.5f;
    public float slideFriction = 0.8f;
    public float airFriction = 1.5f;

    [Header("Dash")]
    public float dashStrength = 15f;
    public float dashCooldown = 0.5f;
    public float dashMaxSpeedBoost = 18f;
    public float maxSpeedDecayRate = 12f;

    [Header("Dash + salto con inercia")]
    public float dashHoldThreshold = 0.2f;

    [Header("Slide post-dash / aterrizaje")]
    public float postDashSlideTime = 0.9f;
    public float slideMinSpeedThreshold = 10f;
    public float landingMomentumPreservation = 0.85f;

    [Header("Cámara")]
    public Transform cameraTransform;

    [Header("Ground Check")]
    public float groundRayLength = 1.2f;
    public LayerMask groundMask = ~0;

    [Header("Pre-landing smoothing")]
    public float landingVerticalSpeedThreshold = -2.5f;

    [Header("Gravedad avanzada")]
    public float fallGravityMultiplier = 1.8f;
    public float lowJumpMultiplier = 1.3f;

    [Header("Control de inercia")]
    [Range(0f, 2f)]
    public float directionCancelStrength = 1f;

    [Header("Dash Momentum Priority")]
    public float dashMomentumLockTime = 0.55f;
    public float dashMomentumLockStrength = 0.15f;

    [Header("Wall Jump")]
    public float wallJumpForce = 12f;
    public float wallJumpSideForce = 10f;
    public float wallCoyoteTime = 0.1f;
    public LayerMask wallMask = ~0;

    [Header("Wall Run")]
    public float wallRunForce = 18f;
    public float wallRunGravity = 5f;
    public float wallRideLookThreshold = 0.75f;
    public float wallJumpLookThreshold = -0.20f;
    public float wallStickForceOpenAngle = 18f;
    public float wallStickAngleStart = 0.4f;

    // ─── ESTADO SINCRONIZADO (NETWORKED) ───
    // Todo lo que cambie frame a frame y afecte al movimiento DEBE estar aquí para el Rollback.
    [Networked] public float currentMaxSpeed { get; set; }
    [Networked] public float dashTimer { get; set; }
    [Networked] public float dashHoldTimer { get; set; }
    [Networked] public float bHopTimer { get; set; }
    [Networked] public float wallTimer { get; set; }
    [Networked] public float slideTimer { get; set; }
    [Networked] public float dashLockTimer { get; set; }
    [Networked] public float speedAtWallEntry { get; set; }
    [Networked] public float peakBhopSpeed { get; set; }

    [Networked] public int bHopCounter { get; set; }
    [Networked] public int WallRunSide { get; set; }

    [Networked] public Vector3 wallNormal { get; set; }
    [Networked] public Vector3 lastWallNormal { get; set; }
    [Networked] public Vector3 dashLockedDirection { get; set; }
    [Networked] public Vector3 inputDir { get; set; }

    [Networked] public NetworkBool grounded { get; set; }
    [Networked] public NetworkBool wasGrounded { get; set; }
    [Networked] public NetworkBool dashButtonHeld { get; set; }
    [Networked] public NetworkBool dashPendingJump { get; set; }
    [Networked] public NetworkBool dashDecayDone { get; set; }
    [Networked] public NetworkBool dashAnimMade { get; set; }
    [Networked] public NetworkBool hasJumped { get; set; }
    [Networked] public NetworkBool touchingWall { get; set; }
    [Networked] public NetworkBool wantsWallJump { get; set; }
    [Networked] public NetworkBool wallRunning { get; set; }
    [Networked] public NetworkBool wasWallRunningLastFrame { get; set; }
    [Networked] public NetworkBool hasWallJumpedSinceGround { get; set; }
    [Networked] public NetworkBool justLanded { get; set; }

    [Networked] public NetworkButtons buttonsPrevious { get; set; }

    public bool IsWallRunning => wallRunning;

    // ─── REFERENCIAS ───
    private Rigidbody rb;
    private PlayerAnimations playerAnims;

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        playerAnims = GetComponent<PlayerAnimations>();

        rb.freezeRotation = true;
        rb.useGravity = false;

        // En Fusion, la interpolación la suele manejar el NetworkRigidbody/NetworkTransform.
        // Si no usas uno, lo dejamos en Interpolate para que se vea suave.
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (HasStateAuthority)
        {
            currentMaxSpeed = maxSpeed;
            dashAnimMade = true;
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (FusionGameState.Instance != null &&
    FusionGameState.Instance.currentMatchState == MatchState.Finished)
        {
            if (rb == null)
                rb = GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            return;
        }
        // Obtenemos el input sincronizado por la red
        if (GetInput(out NetworkInputData input))
        {
            float dt = Runner.DeltaTime;

            // 1. ROTACIÓN DEL JUGADOR SEGÚN EL INPUT
            // Asumiendo que rotas el cuerpo en Y y la cámara en X
            transform.rotation = Quaternion.Euler(0, input.yaw, 0);
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(input.pitch, 0, 0);

            // Mapeamos el Vector2 de input al Vector3 que usabas
            inputDir = new Vector3(input.direction.x, 0f, input.direction.y).normalized;

            var pressed = input.buttons.GetPressed(buttonsPrevious);
            var released = input.buttons.GetReleased(buttonsPrevious);
            bool spaceHeld = input.buttons.IsSet(NetworkInputData.BUTTON_JUMP);
            bool dashHeld = input.buttons.IsSet(NetworkInputData.BUTTON_DASH);

            justLanded = false;
            bHopTimer -= dt;
            dashTimer -= dt;
            wallTimer -= dt;
            if (slideTimer > 0f) slideTimer -= dt;
            if (dashLockTimer > 0f) dashLockTimer -= dt;

            // --- GROUND CHECK ---
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            bool groundHit = Physics.Raycast(rayOrigin, Vector3.down, groundRayLength, groundMask);
            bool groundedNow = groundHit && rb.linearVelocity.y > landingVerticalSpeedThreshold;

            if (!wasGrounded && groundedNow)
            {
                justLanded = true;
                Vector3 preLandingVel = rb.linearVelocity;
                Vector3 hVel = new Vector3(preLandingVel.x, 0f, preLandingVel.z);
                float hSpeed = hVel.magnitude;

                bHopTimer = 0.12f;

                if (hSpeed > slideMinSpeedThreshold)
                {
                    slideTimer = postDashSlideTime;
                    float preservation = spaceHeld ? 0.98f : 0.82f;
                    float speedToKeep = Mathf.Max(hSpeed * preservation, slideMinSpeedThreshold * 1.15f);

                    Vector3 targetSlideVel = hVel.normalized * speedToKeep;
                    rb.linearVelocity = new Vector3(targetSlideVel.x, Mathf.Max(preLandingVel.y, -1f), targetSlideVel.z);
                    currentMaxSpeed = Mathf.Max(currentMaxSpeed, speedToKeep);
                }
            }

            wasGrounded = groundedNow;
            grounded = groundedNow;

            if (groundedNow)
            {
                if (!dashAnimMade && !dashDecayDone)
                {
                    // Evitamos que las animaciones se disparen varias veces en rollbacks
                    if (Runner.IsForward && playerAnims != null)
                        playerAnims.TriggerDash(inputDir.x, inputDir.z);
                    dashAnimMade = true;
                }
                hasWallJumpedSinceGround = false;
                lastWallNormal = Vector3.zero;
                wallTimer = 0f;
                touchingWall = false;

                if (hasJumped && bHopTimer < -0.1f)
                {
                    dashDecayDone = false;
                    bHopCounter = 0;
                }
            }

            // --- WALL CHECK ---
            touchingWall = false;
            Vector3 origin = transform.position + Vector3.up * 1f;
            Vector3[] dirs = { transform.right, -transform.right, transform.forward, -transform.forward };

            RaycastHit bestHit = default;
            float closestDist = float.MaxValue;
            bool foundWall = false;

            foreach (var dir in dirs)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, 1.3f, wallMask))
                {
                    if (!grounded)
                    {
                        if (hasWallJumpedSinceGround && Vector3.Dot(hit.normal, lastWallNormal) > 0.98f)
                            continue;

                        if (hit.distance < closestDist)
                        {
                            closestDist = hit.distance;
                            bestHit = hit;
                            foundWall = true;
                        }
                    }
                }
            }

            if (foundWall)
            {
                wallNormal = bestHit.normal;
                float lookDot = Vector3.Dot(transform.forward, wallNormal);
                if (lookDot < 0.9f)
                {
                    touchingWall = true;
                    wallTimer = wallCoyoteTime;

                    float sideDot = Vector3.Dot(transform.right, wallNormal);
                    WallRunSide = sideDot > 0 ? 1 : -1;

                    if (HasInputAuthority && Runner.IsForward && dynamicFOV)
                        dynamicFOV.SetWallTilt(wallNormal);
                }
            }
            else
            {
                WallRunSide = 0;
            }

            if (!touchingWall && HasInputAuthority && Runner.IsForward && dynamicFOV)
            {
                dynamicFOV.ResetTilt();
                WallRunSide = 0;
            }

            // --- INPUTS: DASH ---
            if (pressed.IsSet(NetworkInputData.BUTTON_DASH) && dashTimer <= 0f && inputDir.sqrMagnitude > 0.01f)
            {
                Dash();
                dashTimer = dashCooldown;
                dashButtonHeld = true;
                dashHoldTimer = 0f;
                dashPendingJump = true;
            }

            if (dashButtonHeld) dashHoldTimer += dt;

            // Release Dash -> DashJump check
            if (released.IsSet(NetworkInputData.BUTTON_DASH) && rb.linearVelocity.magnitude >= 15.5f)
            {
                dashButtonHeld = false;
                if (grounded && dashPendingJump && dashHoldTimer >= dashHoldThreshold)
                {
                    Vector3 v = rb.linearVelocity;
                    v.y = 0f;
                    rb.linearVelocity = v;
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
                    grounded = false;
                    DashJump();
                }
                dashPendingJump = false;
            }

            // --- INPUTS: JUMP & WALL JUMP ---
            // Salto Normal
            if (pressed.IsSet(NetworkInputData.BUTTON_JUMP) && grounded)
            {
                Vector3 v = rb.linearVelocity;
                float currentHorizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;
                v.y = 0f;
                rb.linearVelocity = v;

                rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
                grounded = false;

                if (Runner.IsForward && playerAnims != null) playerAnims.TriggerJump();
                dashPendingJump = false;
                hasJumped = true;

                if (bHopTimer > 0f)
                {
                    bHopCounter++;
                    float bhopBoost = maxSpeed * 0.1f;

                    if (currentHorizontalSpeed + bhopBoost > currentMaxSpeed)
                    {
                        currentMaxSpeed = currentHorizontalSpeed + bhopBoost;
                        peakBhopSpeed = currentMaxSpeed;
                    }

                    currentMaxSpeed = peakBhopSpeed;

                    Vector3 wishDirBhop = transform.TransformDirection(inputDir).normalized;
                    rb.AddForce(wishDirBhop * bhopBoost, ForceMode.VelocityChange);

                    if (HasInputAuthority && Runner.IsForward && dynamicFOV)
                        dynamicFOV.AddFOVKick(bhopFov);

                    dashDecayDone = false;
                }
                else
                {
                    bHopCounter = 0;
                    peakBhopSpeed = maxSpeed;
                    dashDecayDone = false;
                }
            }

            // Intención de Wall Jump (Buffer)
            if (pressed.IsSet(NetworkInputData.BUTTON_JUMP) && wallTimer > 0f && !grounded)
            {
                wantsWallJump = true;
            }

            // Iniciar Wallrun
            if (touchingWall && !grounded && spaceHeld)
            {
                wallRunning = true;
            }
            else
            {
                // Soltar el espacio activa el wall jump si estamos en la pared
                if ((wallRunning || wantsWallJump) && released.IsSet(NetworkInputData.BUTTON_JUMP) && wallTimer > 0f)
                {
                    WallJump();
                }
                wallRunning = false;
            }

            if (!spaceHeld) wantsWallJump = false;

            // --- MOVIMIENTO PRINCIPAL ---
            Vector3 velocity = rb.linearVelocity;
            Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);

            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            forward.y = 0; right.y = 0;
            forward.Normalize(); right.Normalize();

            Vector3 wishDir = (forward * inputDir.z + right * inputDir.x).normalized;
            float accel = grounded ? groundAccel : airAccel;

            // Direction cancel (Source style)
            if (grounded && inputDir.sqrMagnitude > 0.01f)
            {
                Vector3 wishDirNorm = wishDir.normalized;
                Vector3 horizVelNorm = horizontalVel.sqrMagnitude > 0.01f ? horizontalVel.normalized : Vector3.zero;
                float dot = Vector3.Dot(horizVelNorm, wishDirNorm);

                if (dot < 0f)
                {
                    float cancelRate = accel * dt;
                    Vector3 cancelDir = Vector3.Project(horizontalVel, wishDirNorm);
                    Vector3 delta = cancelDir - horizontalVel;

                    if (delta.magnitude > cancelRate)
                        delta = delta.normalized * cancelRate;

                    horizontalVel += delta;
                    rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);
                }
            }

            // Lógica WallRun Física
            if (wallRunning)
            {
                if (!wasWallRunningLastFrame)
                {
                    speedAtWallEntry = Mathf.Max(rb.linearVelocity.magnitude, maxSpeed);
                    Vector3 currentVel = rb.linearVelocity;
                    Vector3 velocityOnWall = Vector3.ProjectOnPlane(currentVel, wallNormal);
                    rb.linearVelocity = velocityOnWall.normalized * speedAtWallEntry;
                    wasWallRunningLastFrame = true;
                }

                Vector3 playerInputWorld = transform.TransformDirection(inputDir);
                Vector3 wallProjectedMove = Vector3.ProjectOnPlane(playerInputWorld, wallNormal).normalized;

                if (inputDir.sqrMagnitude < 0.01f)
                {
                    Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
                    if (Vector3.Dot(wallForward, rb.linearVelocity) < 0f) wallForward = -wallForward;
                    wallProjectedMove = wallForward;
                }

                rb.AddForce(wallProjectedMove * wallRunForce, ForceMode.Acceleration);

                if (rb.linearVelocity.magnitude > speedAtWallEntry)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * speedAtWallEntry;
                }

                rb.AddForce(-wallNormal * 8f, ForceMode.Acceleration);

                float lookDot = Vector3.Dot(transform.forward, wallNormal);
                if (lookDot > wallStickAngleStart)
                {
                    float extraStick = Mathf.InverseLerp(wallStickAngleStart, 0.95f, lookDot);
                    rb.AddForce(-wallNormal * wallStickForceOpenAngle * extraStick, ForceMode.Acceleration);
                }

                rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Acceleration);

                Vector3 v = rb.linearVelocity;
                if (v.y < -maxSpeed * 0.2f) v.y = -maxSpeed * 0.2f;
                rb.linearVelocity = v;
            }
            else
            {
                wasWallRunningLastFrame = false;
            }

            // Aplicar Aceleración y Fricción
            if (inputDir.sqrMagnitude > 0.01f)
            {
                float speedCap = currentMaxSpeed;
                if (inputDir.z > 0.1f)
                    speedCap *= Mathf.Lerp(1f, forwardSpeedMultiplier, inputDir.z);

                Vector3 targetVel = horizontalVel + wishDir * accel * dt;

                if (targetVel.magnitude > speedCap)
                    targetVel = targetVel.normalized * speedCap;

                rb.AddForce(targetVel - horizontalVel, ForceMode.VelocityChange);
            }
            else
            {
                float currentFriction = grounded
                    ? (slideTimer > 0f && inputDir.sqrMagnitude > 0.01f ? slideFriction : groundFriction)
                    : airFriction;

                horizontalVel = Vector3.Lerp(horizontalVel, Vector3.zero, currentFriction * dt);
                rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);
            }

            // Gravedad
            if (!grounded)
            {
                if (wallRunning)
                {
                    // La gravedad del wallrun ya se aplica arriba para evitar bugs de orden, 
                    // pero si la necesitas aquí, puedes asegurarla.
                }
                else
                {
                    if (rb.linearVelocity.y < 0f)
                        rb.AddForce(Vector3.down * gravity * fallGravityMultiplier, ForceMode.Acceleration);
                    else if (rb.linearVelocity.y > 0f && !spaceHeld)
                        rb.AddForce(Vector3.down * gravity * lowJumpMultiplier, ForceMode.Acceleration);
                    else
                        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
                }
            }

            // Decaimiento de velocidad
            if (currentMaxSpeed > maxSpeed && !dashDecayDone)
            {
                float currentHVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;

                if (currentMaxSpeed - currentHVelocity > 5f)
                {
                    currentMaxSpeed -= 2.5f;
                    peakBhopSpeed = currentMaxSpeed;
                }

                float decayMod = (grounded && !spaceHeld) ? 1f : 0.05f;
                currentMaxSpeed -= maxSpeedDecayRate * decayMod * dt;

                if (currentMaxSpeed <= maxSpeed)
                {
                    currentMaxSpeed = maxSpeed;
                    dashDecayDone = true;
                    peakBhopSpeed = maxSpeed;
                }
            }

            // Guardar botones para el siguiente frame
            buttonsPrevious = input.buttons;
        }
    }

    // ─── FUNCIONES DE HABILIDADES ───

    private void Dash()
    {
        if (inputDir.sqrMagnitude < 0.01f) return;

        Vector3 horizontalDir = transform.TransformDirection(inputDir).normalized;
        float xAbs = Mathf.Abs(inputDir.x);
        float zAbs = Mathf.Abs(inputDir.z);

        float dirMultiplier = 2f;
        if (xAbs > zAbs)
        {
            dirMultiplier = 1f + (xAbs - zAbs);
            dirMultiplier = Mathf.Clamp(dirMultiplier, 3f, 3.5f);
        }

        Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;
        float vertical = camForward.y;
        if (inputDir.z < 0f) vertical *= -1f;
        vertical = Mathf.Min(vertical, 0f);

        Vector3 dashDir = horizontalDir;
        dashDir.y = vertical;
        dashDir.Normalize();
        if (vertical < -0.8f) dashDir.y *= 1.3f;

        rb.AddForce(dashDir * dashStrength * dirMultiplier, ForceMode.VelocityChange);
        currentMaxSpeed = maxSpeed + dashMaxSpeedBoost;
        dashDecayDone = false;
        dashLockTimer = dashMomentumLockTime;
        dashLockedDirection = dashDir;
        dashAnimMade = false;
    }

    private void WallJump()
    {
        wallTimer = 0f;
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        Vector3 baseDir = wallNormal + Vector3.up;
        baseDir.Normalize();

        Vector3 inputWorld = transform.TransformDirection(inputDir);
        inputWorld.y = 0f;
        Vector3 lateralDir = Vector3.ProjectOnPlane(inputWorld, wallNormal);
        float lateralInfluence = Mathf.Clamp01(inputDir.magnitude);

        Vector3 finalDir = baseDir;
        if (lateralDir.sqrMagnitude > 0.001f)
            finalDir = Vector3.Lerp(baseDir, (baseDir + lateralDir.normalized), lateralInfluence).normalized;

        rb.AddForce(finalDir * wallJumpForce, ForceMode.VelocityChange);
        grounded = false;

        lastWallNormal = wallNormal;
        hasWallJumpedSinceGround = true;

        if (HasInputAuthority && Runner.IsForward && dynamicFOV)
        {
            dynamicFOV.AddFOVKick(wallJumpFov);
            dynamicFOV.TriggerWallJumpShake();
        }
    }

    private void DashJump()
    {
        if (Runner.IsForward && playerAnims != null) playerAnims.TriggerJump();

        Vector3 camForward = cameraTransform != null ? cameraTransform.forward : transform.forward;

        Vector3 dashDir = inputDir.sqrMagnitude > 0.01f
            ? transform.TransformDirection(inputDir)
            : new Vector3(camForward.x, 0f, camForward.z).normalized;

        rb.AddForce(dashDir * dashStrength / 3f, ForceMode.VelocityChange);
        currentMaxSpeed = maxSpeed + dashMaxSpeedBoost;
        dashDecayDone = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // ¡ESTO ES LO IMPORTANTE PARA EVITAR EL ERROR!
        // No intentamos leer 'grounded' si el juego no está corriendo o el objeto no ha hecho Spawn.
        if (!Application.isPlaying || Object == null || !Object.IsValid)
            return;

        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * groundRayLength);
    }

    private void OnGUI()
    {
        // Y lo mismo aquí para el contador de Bhop
        if (!Application.isPlaying || Object == null || !Object.IsValid)
            return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(300, 10, 300, 30), $"Bhop Counter: {bHopCounter:0}", style);
    }
#endif
}