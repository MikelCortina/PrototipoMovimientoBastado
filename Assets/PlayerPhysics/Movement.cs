using UnityEngine;
using static UnityEngine.Rendering.DebugUI.MessageBox;

[RequireComponent(typeof(Rigidbody))]
public class SmoothPlayerController : MonoBehaviour
{
    [Header("FOV Effects")]
    public DynamicSpeedFOV dynamicFOV;
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
    public float bHopTimer = 0.075f;
    public bool hasJumped = false;

    [Header("Dash Momentum Priority")]
    public float dashMomentumLockTime = 0.55f;
    public float dashMomentumLockStrength = 0.15f;

    private float dashLockTimer = 0f;
    private Vector3 dashLockedDirection;

    [Header("Wall Jump")]
    public float wallJumpForce = 12f;
    public float wallJumpSideForce = 10f;
    public float wallCoyoteTime = 0.1f;
    public LayerMask wallMask = ~0;
    private bool wantsWallJump;


    [Header("Wall Run")]
    public float wallRunForce = 18f;
    public float wallRunGravity = 5f;

    private bool wallRunning;
    private bool spaceHeldLastFrame;
    public float wallRideLookThreshold = 0.75f;     // ← NUEVO (antes era rideLookThreshold = -0.05f)
    public float wallJumpLookThreshold = -0.20f;    // ← antes era -0.3f (un poco más permisivo)
    public float wallStickForceOpenAngle = 18f;     // ← fuerza extra cuando miras muy hacia fuera
    public float wallStickAngleStart = 0.4f;        // a partir de qué dot empezamos a aplicar más stick





    private Vector3 lastWallNormal;
    private bool hasWallJumpedSinceGround = false;
    private float wallTimer;
    private Vector3 wallNormal;
    private bool touchingWall;
    private int bHopCounter = 0;

    // ─── PRIVADAS ───
    public Rigidbody rb;
    private Vector3 inputDir;
    public bool grounded;
    private bool wasGrounded;
    private float dashTimer;
    private float dashHoldTimer;
    private bool dashButtonHeld;
    private bool dashPendingJump;
    private float currentMaxSpeed;
    private bool dashDecayDone;
    private float slideTimer;
    private float speedAtWallEntry;
    private bool wasWallRunningLastFrame;
    private float peakBhopSpeed; // Almacena la velocidad más alta de la cadena actual


    [HideInInspector] public bool justLanded = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        currentMaxSpeed = maxSpeed;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.fixedDeltaTime = 0.0025f;
    }

    void Update()
    {
        // Input
        inputDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")).normalized;

        // Dash
        dashTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.LeftShift) && dashTimer <= 0f && inputDir.sqrMagnitude > 0.01f)
        {
            Dash();
            dashTimer = dashCooldown;
            dashButtonHeld = true;
            dashHoldTimer = 0f;
            dashPendingJump = true;
        }

        if (dashButtonHeld)
            dashHoldTimer += Time.deltaTime;

        if (Input.GetKeyUp(KeyCode.LeftShift) && rb.linearVelocity.magnitude >= 15.5f)
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

        // Salto normal
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            Vector3 v = rb.linearVelocity;
            float currentHorizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;
            v.y = 0f;
            rb.linearVelocity = v;

            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            grounded = false;
            dashPendingJump = false;
            hasJumped = true;

            if (bHopTimer > 0f)
            {
                bHopCounter++;
                float bhopBoost = maxSpeed * 0.1f;

                // Solo actualizamos si la velocidad actual + boost es mayor que nuestro tope penalizado
                if (currentHorizontalSpeed + bhopBoost > currentMaxSpeed)
                {
                    currentMaxSpeed = currentHorizontalSpeed + bhopBoost;
                    peakBhopSpeed = currentMaxSpeed;
                }

                // 2. Forzamos a que la velocidad actual intente recuperar ese pico
                currentMaxSpeed = peakBhopSpeed;

                // 3. Aplicamos un pequeño empujón extra en la dirección que mira el jugador 
                // para compensar la pérdida de la fricción del aire del frame anterior
                Vector3 wishDir = transform.TransformDirection(inputDir).normalized;
                rb.AddForce(wishDir * bhopBoost, ForceMode.VelocityChange);

                if (dynamicFOV) dynamicFOV.AddFOVKick(bhopFov);
                dashDecayDone = false;
            }
            else
            {
                // Si fallamos el timing, reseteamos el récord de velocidad
                bHopCounter = 0;
                peakBhopSpeed = maxSpeed;
                dashDecayDone = false;
            }
        }

        bool spaceHeld = Input.GetKey(KeyCode.Space);
        // BUFFER de intención de wall jump
        if (Input.GetKeyDown(KeyCode.Space) && wallTimer > 0f && !grounded)
        {
            wantsWallJump = true;
        }

        if (touchingWall && !grounded && spaceHeld)
        {
            wallRunning = true;
        }
        else
        {
            // SOLTAR SPACE → wall jump inmediato (aunque no haya wallrun)
            if ((wallRunning || wantsWallJump) && spaceHeldLastFrame && !spaceHeld && wallTimer > 0f)
            {
                WallJump();
            }

            wallRunning = false;
        }


        if (!spaceHeld)
            wantsWallJump = false;

        spaceHeldLastFrame = spaceHeld;
    }

    void FixedUpdate()
    {
        justLanded = false;
        // Ground check
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
        bool groundHit = Physics.Raycast(rayOrigin, Vector3.down, groundRayLength, groundMask);
        bool groundedNow = groundHit && rb.linearVelocity.y > landingVerticalSpeedThreshold;
        bHopTimer -= Time.fixedDeltaTime;

        // Detectar aterrizaje → activar slide si vamos rápido
        if (!wasGrounded && groundedNow)
        {
            justLanded = true;
            Vector3 preLandingVel = rb.linearVelocity;
            Vector3 hVel = new Vector3(preLandingVel.x, 0f, preLandingVel.z);
            float hSpeed = hVel.magnitude;

            bHopTimer = 0.12f; // Un margen ligeramente mayor ayuda a la consistencia

            if (hSpeed > slideMinSpeedThreshold)
            {
                slideTimer = postDashSlideTime;

                // --- MODIFICACIÓN AQUÍ ---
                // Si el jugador está presionando espacio (intentando bhop), preservamos más velocidad
                float preservation = Input.GetKey(KeyCode.Space) ? 0.98f : 0.82f;

                float speedToKeep = Mathf.Max(hSpeed * preservation, slideMinSpeedThreshold * 1.15f);
                Vector3 targetSlideVel = hVel.normalized * speedToKeep;
                rb.linearVelocity = new Vector3(targetSlideVel.x, Mathf.Max(preLandingVel.y, -1f), targetSlideVel.z);

                // Actualizamos el currentMaxSpeed para que el motor de movimiento no nos frene en el FixedUpdate
                currentMaxSpeed = Mathf.Max(currentMaxSpeed, speedToKeep);
            }
        }


        wasGrounded = groundedNow;
        grounded = groundedNow;

        if (groundedNow)
        {
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

        // Wall check
        touchingWall = false;
        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3[] dirs = { transform.right, -transform.right, transform.forward, -transform.forward };
        Vector3 camForward = transform.forward; // o cameraTransform.forward si prefieres

        float bestDot = -1f;
        RaycastHit bestHit = default;

        // Simplificamos la detección para que sea más robusta de frente
        float closestDist = float.MaxValue;
        bool foundWall = false;

        foreach (var dir in dirs)
        {
            // Aumentamos un poco el rango del rayo (1.3f) para detectar la pared antes de chocar físicamente
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 1.3f, wallMask))
            {
                if (!grounded)
                {
                    if (hasWallJumpedSinceGround && Vector3.Dot(hit.normal, lastWallNormal) > 0.98f)
                        continue;

                    // En lugar de buscar el "mejor dot", buscamos la pared más cercana
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

            // WALL RIDE: Se activa si no estamos mirando directamente AL INTERIOR de la pared (normal vs forward)
            // De frente, lookDot es -1. De lado es 0. 
            if (lookDot < 0.9f) // Casi cualquier ángulo sirve para tocar la pared
            {
                touchingWall = true;
                if (dynamicFOV) dynamicFOV.SetWallTilt(wallNormal);
                wallTimer = wallCoyoteTime; // Permitimos el salto siempre que estemos tocando
            }
        }
        if (!touchingWall && dynamicFOV)
        {
            dynamicFOV.ResetTilt();
        }

        
        wallTimer -= Time.fixedDeltaTime;

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(velocity.x, 0f, velocity.z);

        // En lugar de usar cameraTransform, usa directamente el transform del jugador
        // ya que el MouseLook ya rota el cuerpo (playerBody).
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 wishDir = (forward * inputDir.z + right * inputDir.x).normalized;
        float accel = grounded ? groundAccel : airAccel;

        if (grounded && inputDir.sqrMagnitude > 0.01f)
        {
            Vector3 wishDirNorm = wishDir.normalized;
            Vector3 horizVelNorm = horizontalVel.sqrMagnitude > 0.01f ? horizontalVel.normalized : Vector3.zero;
            float dot = Vector3.Dot(horizVelNorm, wishDirNorm);

            if (dot < 0f)
            {
                float cancel = (-dot) * directionCancelStrength;

                // ❌ NO instantáneo
                // horizontalVel = Vector3.Lerp(horizontalVel, projected, cancel);

                // ✅ Cancel progresivo tipo Source
                float cancelRate = accel * Time.fixedDeltaTime;
                Vector3 cancelDir = Vector3.Project(horizontalVel, wishDirNorm);
                Vector3 delta = cancelDir - horizontalVel;

                if (delta.magnitude > cancelRate)
                    delta = delta.normalized * cancelRate;

                horizontalVel += delta;
                rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);
            }
        }
        if (wallRunning)
        {
            // --- NUEVO: Capturar velocidad al entrar ---
            if (!wasWallRunningLastFrame)
            {
                // Guardamos la magnitud actual, pero nos aseguramos de que sea al menos la velocidad base
                speedAtWallEntry = Mathf.Max(rb.linearVelocity.magnitude, maxSpeed);
                wasWallRunningLastFrame = true;
            }

            // [Tu lógica actual de movimiento proyectado en la pared...]
            Vector3 playerInputWorld = transform.TransformDirection(inputDir);
            Vector3 wallProjectedMove = Vector3.ProjectOnPlane(playerInputWorld, wallNormal).normalized;

            if (inputDir.sqrMagnitude < 0.01f)
            {
                Vector3 wallForward = Vector3.Cross(wallNormal, Vector3.up);
                if (Vector3.Dot(wallForward, rb.linearVelocity) < 0f) wallForward = -wallForward;
                wallProjectedMove = wallForward;
            }

            // Aplicar fuerza de movimiento
            rb.AddForce(wallProjectedMove * wallRunForce, ForceMode.Acceleration);

            // --- NUEVO: Limitar velocidad ---
            // Si la velocidad actual supera la que teníamos al entrar, la recortamos
            if (rb.linearVelocity.magnitude > speedAtWallEntry)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * speedAtWallEntry;
            }

            // [El resto de tus fuerzas: Succión, Gravedad reducida, etc.]
            rb.AddForce(-wallNormal * 8f, ForceMode.Acceleration);

            // 2. Succión extra al mirar hacia fuera
            float lookDot = Vector3.Dot(transform.forward, wallNormal);
            if (lookDot > wallStickAngleStart)
            {
                // Usamos un multiplicador para que la fuerza no sea infinita
                float extraStick = Mathf.InverseLerp(wallStickAngleStart, 0.95f, lookDot);
                // IMPORTANTE: Limitamos la fuerza total para evitar el rebote
                rb.AddForce(-wallNormal * wallStickForceOpenAngle * extraStick, ForceMode.Acceleration);
            }

            // Gravedad reducida y control vertical
            rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Acceleration);

            // Limitar caída vertical para que se sienta que "planeas"
            Vector3 v = rb.linearVelocity;
            if (v.y < -maxSpeed * 0.2f) v.y = -maxSpeed * 0.2f;
            rb.linearVelocity = v;
        }
        else
        {
            wasWallRunningLastFrame = false;
        }
        if (inputDir.sqrMagnitude > 0.01f)
        {
            // Calculamos la velocidad máxima deseada para este frame
            float speedCap = currentMaxSpeed;

            // Si nos movemos hacia adelante (W), aumentamos el límite de velocidad
            if (inputDir.z > 0.1f)
            {
                // Lerp opcional para que la transición sea suave si el input no es binario
                speedCap *= Mathf.Lerp(1f, forwardSpeedMultiplier, inputDir.z);
            }

            Vector3 targetVel = horizontalVel + wishDir * accel * Time.fixedDeltaTime;

            // Usamos el speedCap dinámico en lugar de currentMaxSpeed fijo
            if (targetVel.magnitude > speedCap)
                targetVel = targetVel.normalized * speedCap;

            rb.AddForce(targetVel - horizontalVel, ForceMode.VelocityChange);
        }
        else
        {
            float currentFriction = grounded
                ? (slideTimer > 0f && inputDir.sqrMagnitude > 0.01f ? slideFriction : groundFriction)
                : airFriction;

            horizontalVel = Vector3.Lerp(horizontalVel, Vector3.zero, currentFriction * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);
        }

        // Gravedad personalizada
        // Gravedad personalizada
        if (!grounded)
        {
            if (wallRunning)
            {
                rb.AddForce(Vector3.down * wallRunGravity, ForceMode.Acceleration);
            }
            else
            {
                if (rb.linearVelocity.y < 0f)
                    rb.AddForce(Vector3.down * gravity * fallGravityMultiplier, ForceMode.Acceleration);
                else if (rb.linearVelocity.y > 0f && !Input.GetKey(KeyCode.Space))
                    rb.AddForce(Vector3.down * gravity * lowJumpMultiplier, ForceMode.Acceleration);
                else
                    rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
            }
        }


        if (currentMaxSpeed > maxSpeed && !dashDecayDone)
        {
            // Calculamos la velocidad horizontal actual
            float currentHVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;

            // --- NUEVA LÓGICA DE PENALIZACIÓN ---
            // Si la velocidad real cae más de 5 unidades por debajo del máximo que deberíamos llevar...
            if (currentMaxSpeed - currentHVelocity > 5f)
            {
                // Reducimos el tope máximo en 2.5
                currentMaxSpeed -= 2.5f;

                // También actualizamos el récord de memoria para que el próximo salto no nos teletransporte a la velocidad vieja
                peakBhopSpeed = currentMaxSpeed;
            }

            // Decay natural (el que ya teníamos para que la velocidad baje suavemente con el tiempo)
            float decayMod = (grounded && !Input.GetKey(KeyCode.Space)) ? 1f : 0.05f;
            currentMaxSpeed -= maxSpeedDecayRate * decayMod * Time.fixedDeltaTime;

            // Si bajamos del límite base, reseteamos todo
            if (currentMaxSpeed <= maxSpeed)
            {
                currentMaxSpeed = maxSpeed;
                dashDecayDone = true;
                peakBhopSpeed = maxSpeed;
            }
        }


    }

    void Dash()
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

        Vector3 camForward = cameraTransform.forward;
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

       
    }


    void WallJump()
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

        if (dynamicFOV) dynamicFOV.AddFOVKick(wallJumpFov);

        lastWallNormal = wallNormal;
        hasWallJumpedSinceGround = true;
        if (dynamicFOV)
        {
            dynamicFOV.AddFOVKick(wallJumpFov);
            dynamicFOV.TriggerWallJumpShake(); // ACTIVA EL SHAKE
        }

    }

    void DashJump()
    {
        Vector3 dashDir = inputDir.sqrMagnitude > 0.01f
            ? transform.TransformDirection(inputDir)
            : new Vector3(cameraTransform.forward.x, 0f, cameraTransform.forward.z).normalized;

        rb.AddForce(dashDir * dashStrength / 3f, ForceMode.VelocityChange);
        currentMaxSpeed = maxSpeed + dashMaxSpeedBoost;
        dashDecayDone = false;

     
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = grounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * groundRayLength);
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;
        GUI.Label(new Rect(300, 10, 300, 30), $"Bhop Counter: {bHopCounter:0}", style);
    }
#endif
}
