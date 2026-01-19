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
    public float bHopTimer = 0.1f;
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
            v.y = 0f;
            rb.linearVelocity = v;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            grounded = false;
            dashPendingJump = false;
            hasJumped = true;

            if (bHopTimer > 0f)
            {
                bHopCounter++;
                float bhopBoost = maxSpeed * 0.075f;
                float bhopDecayFactor = 0.8f;
                currentMaxSpeed += bhopBoost;
                bhopBoost *= bhopDecayFactor;

                if (dynamicFOV) dynamicFOV.AddFOVKick(bhopFov);
            }
            else
            {
                dashDecayDone = false;
                bHopCounter = 0;
            }
            
        }

        if (Input.GetKeyDown(KeyCode.Space) && wallTimer > 0f && !grounded)
        {
            WallJump();
        }
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

            justLanded = true; // ✅ Señal de aterrizaje
            Vector3 preLandingVel = rb.linearVelocity;
            Vector3 hVel = new Vector3(preLandingVel.x, 0f, preLandingVel.z);
            float hSpeed = hVel.magnitude;

            bHopTimer = 0.1f;


           

            if (hSpeed > slideMinSpeedThreshold)
            {
                slideTimer = postDashSlideTime;
                float speedToKeep = Mathf.Max(hSpeed * 0.82f, slideMinSpeedThreshold * 1.15f);
                Vector3 targetSlideVel = hVel.normalized * speedToKeep;
                rb.linearVelocity = new Vector3(targetSlideVel.x, Mathf.Max(preLandingVel.y, -1f), targetSlideVel.z);

                if (inputDir.sqrMagnitude > 0.01f)
                {
                    Vector3 wish = transform.TransformDirection(inputDir).normalized;
                    float dot = Vector3.Dot(hVel.normalized, wish);
                    if (dot < 0.3f)
                        rb.AddForce(wish * 6f, ForceMode.VelocityChange);
                }
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
        Vector3[] dirs =
        {
    transform.right,
    -transform.right,
    transform.forward,
    -transform.forward
};

        Vector3 camForward = transform.forward;
        float minWallLookDot = -0.3f; // cuanto más cercano a -1, más estricto

        foreach (var dir in dirs)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 1f, wallMask))
            {
                if (!grounded)
                {
                    // Evitar spamear la misma pared
                    if (hasWallJumpedSinceGround && Vector3.Dot(hit.normal, lastWallNormal) > 0.98f)
                        continue;

                    // 🔥 CHECK DE ÁNGULO CAMARA-PARED
                    float lookDot = Vector3.Dot(camForward, hit.normal);

                    if (lookDot > minWallLookDot)
                        continue; // Está de lado o de espaldas → no walljump

                    touchingWall = true;
                    wallNormal = hit.normal;
                    wallTimer = wallCoyoteTime;
                    break;
                }
            }
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

        if (inputDir.sqrMagnitude > 0.01f)
        {
            Vector3 targetVel = horizontalVel + wishDir * accel * Time.fixedDeltaTime;
            if (targetVel.magnitude > currentMaxSpeed)
                targetVel = targetVel.normalized * currentMaxSpeed;

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
        if (!grounded)
        {
            if (rb.linearVelocity.y < 0f)
                rb.AddForce(Vector3.down * gravity * fallGravityMultiplier, ForceMode.Acceleration);
            else if (rb.linearVelocity.y > 0f && !Input.GetKey(KeyCode.Space))
                rb.AddForce(Vector3.down * gravity * lowJumpMultiplier, ForceMode.Acceleration);
            else
                rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration);
        }

        // Decay del boost de velocidad del dash
        if (currentMaxSpeed > maxSpeed && !dashDecayDone)
        {
            currentMaxSpeed -= maxSpeedDecayRate * Time.fixedDeltaTime;
            if (currentMaxSpeed <= maxSpeed)
            {
                currentMaxSpeed = maxSpeed;
                dashDecayDone = true;
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
            dirMultiplier = Mathf.Clamp(dirMultiplier, 1.5f, 2f);
        }

        Vector3 camForward = cameraTransform.forward;
        float vertical = camForward.y;
        if (inputDir.z < 0f) vertical *= -1f;
        vertical = Mathf.Min(vertical, 0f);

        Vector3 dashDir = horizontalDir;
        dashDir.y = vertical;
        dashDir.Normalize();

        rb.AddForce(dashDir * dashStrength * dirMultiplier, ForceMode.VelocityChange);
        currentMaxSpeed = maxSpeed + dashMaxSpeedBoost;
        dashDecayDone = false;
        dashLockTimer = dashMomentumLockTime;
        dashLockedDirection = dashDir;

        if (vertical < -0.8f) dashDir.y *= 1.3f;
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
