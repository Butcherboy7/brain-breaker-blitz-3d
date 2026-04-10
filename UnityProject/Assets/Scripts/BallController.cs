using System.Collections;
using UnityEngine;

/// <summary>
/// BallController — Cyberpunk Neon Edition.
/// Handles ball physics + all visual juice:
///   - Neon glowing material with pulsing emission
///   - Thicc multi-layer trail that color-shifts with combo level
///   - Rainbow shift on max combo (x10+)
///   - Speed lines ghost for fast movement
///   - Squash-stretch on collisions (1-frame juice)
///   - Point light that throbs with the ball
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    // ── Public API ───────────────────────────────────────
    public float initialSpeed = 8f;
    public bool  isLaunched   = false;
    
    private Rigidbody _rb;
    public  Rigidbody rb { get { if (!_rb) _rb = GetComponent<Rigidbody>(); return _rb; } }

    // ── Physics constants ────────────────────────────────
    const float MIN_Y_FRAC  = 0.30f;
    const float STUCK_TIME  = 2.8f;
    const float WALL_INNER  = 8.5f;
    const float CEILING_Y   = 6.5f;

    // ── Internal state ───────────────────────────────────
    private Vector3       moveDir  = Vector3.up;
    private float         stuckTimer;
    private float         lastY;
    private TrailRenderer trail;
    private TrailRenderer trailGhost; // second ghost trail for afterimage effect
    private Material      ballMat;
    private Light         ballLight;
    private Vector3       baseScale;
    private float         pulsePhase; // for breathing glow
    private bool          squashing;
    private float         launchCooldown = 0.5f;

    // ─────────────────────────────────────────────────────
    void Awake()
    {
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime         = 1f / 60f;

        rb.useGravity             = false;
        rb.isKinematic            = false;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints            = RigidbodyConstraints.FreezePositionZ
                                  | RigidbodyConstraints.FreezeRotation;

        var sc = GetComponent<SphereCollider>();
        if (sc == null) sc = gameObject.AddComponent<SphereCollider>();
        sc.material = MakePMat("Ball");

        baseScale = transform.localScale;

        SetupMaterial();
        SetupLight();
        SetupTrails();

        launchCooldown = 0.5f;
    }

    // ── Visual Setup ──────────────────────────────────────
    void SetupMaterial()
    {
        ballMat = NeonVisuals.MakeNeonMaterial(NeonVisuals.NeonCyan, emissionIntensity: 3f,
                                               metallic: 0.8f, smoothness: 0.95f);
        GetComponent<MeshRenderer>().material = ballMat;
    }

    void SetupLight()
    {
        ballLight = GetComponent<Light>();
        if (ballLight == null) ballLight = gameObject.AddComponent<Light>();
        ballLight.type      = LightType.Point;
        ballLight.color     = NeonVisuals.NeonCyan;
        ballLight.range     = 8f;
        ballLight.intensity = 4f;
    }

    void SetupTrails()
    {
        // Primary trail — thick neon
        trail = GetComponent<TrailRenderer>();
        if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
        trail.time           = 0.22f;
        trail.startWidth     = 0.55f;
        trail.endWidth       = 0f;
        trail.numCapVertices = 10;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.material       = new Material(Shader.Find("Sprites/Default"));
        trail.colorGradient  = NeonVisuals.MakeTrailGradient(NeonVisuals.NeonCyan);

        // Ghost afterimage trail — wider, more transparent
        var ghostGO = new GameObject("BallTrailGhost");
        ghostGO.transform.SetParent(transform, false);
        ghostGO.transform.localPosition = Vector3.zero;
        trailGhost = ghostGO.AddComponent<TrailRenderer>();
        trailGhost.time           = 0.1f;
        trailGhost.startWidth     = 0.9f;
        trailGhost.endWidth       = 0f;
        trailGhost.numCapVertices = 6;
        trailGhost.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trailGhost.material       = new Material(Shader.Find("Sprites/Default"));

        var ghostGrad = new Gradient();
        ghostGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(NeonVisuals.NeonCyan, 0f),
                new GradientColorKey(NeonVisuals.NeonPurple, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.25f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trailGhost.colorGradient = ghostGrad;
    }

    // ─────────────────────────────────────────────────────
    PhysicMaterial MakePMat(string n) => new PhysicMaterial(n)
    {
        bounciness      = 1f,
        dynamicFriction = 0f,
        staticFriction  = 0f,
        frictionCombine = PhysicMaterialCombine.Minimum,
        bounceCombine   = PhysicMaterialCombine.Maximum,
    };

    // ─────────────────────────────────────────────────────
    public void ResetBall(float speed)
    {
        initialSpeed = speed;
        isLaunched   = false;
        stuckTimer   = 0f;
        lastY        = 0f;
        moveDir      = Vector3.up;
        transform.position     = new Vector3(0f, -3.2f, 0f);
        transform.localScale   = baseScale;
        rb.velocity            = Vector3.zero;
        rb.angularVelocity     = Vector3.zero;
        if (trail)      trail.Clear();
        if (trailGhost) trailGhost.Clear();
        squashing = false;
        launchCooldown = 0.5f;
        UpdateVisualForCombo(0);
    }

    // ─────────────────────────────────────────────────────
    void Update()
    {
        if (launchCooldown > 0) launchCooldown -= Time.unscaledDeltaTime;

        // Launch input
        if (!isLaunched
            && launchCooldown <= 0
            && GameManager.Instance != null
            && GameManager.Instance.isPlaying
            && !GameManager.Instance.isPaused
            && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            Launch();
        }

        // Breathing pulse glow
        pulsePhase += Time.deltaTime * 4f;
        UpdatePulseGlow();

        // Combo-based color shift
        int combo = GameManager.Instance?.combo ?? 0;
        UpdateVisualForCombo(combo);
    }

    void Launch()
    {
        isLaunched = true;
        float rx   = Random.Range(-0.35f, 0.35f);
        moveDir    = new Vector3(rx, 1f, 0f).normalized;
        rb.velocity = moveDir * initialSpeed;
    }

    // ─────────────────────────────────────────────────────
    void UpdatePulseGlow()
    {
        if (ballMat == null || ballLight == null) return;
        float pulse = 0.75f + Mathf.Sin(pulsePhase) * 0.25f;
        ballLight.intensity = 4f * pulse;
    }

    void UpdateVisualForCombo(int combo)
    {
        if (ballMat == null) return;

        Color targetColor;
        float emission;

        if (combo >= 10) // Rainbow max combo
        {
            targetColor = NeonVisuals.RainbowColor(Time.time * 0.5f);
            emission    = 5f;
        }
        else if (combo >= 7)
        {
            targetColor = Color.Lerp(NeonVisuals.NeonPurple, NeonVisuals.NeonPink, Mathf.PingPong(Time.time, 1f));
            emission    = 4f;
        }
        else if (combo >= 5)
        {
            targetColor = NeonVisuals.NeonPink;
            emission    = 3.5f;
        }
        else if (combo >= 3)
        {
            targetColor = NeonVisuals.NeonYellow;
            emission    = 3f;
        }
        else
        {
            targetColor = NeonVisuals.NeonCyan;
            emission    = 2f;
        }

        ballMat.SetColor("_EmissionColor", targetColor * emission);
        ballMat.color = targetColor * 0.5f;
        if (ballLight) ballLight.color = targetColor;

        // Update trail colors  
        if (trail) trail.colorGradient = NeonVisuals.MakeTrailGradient(targetColor);
    }

    // ─────────────────────────────────────────────────────
    void FixedUpdate()
    {
        if (!isLaunched) return;

        Vector3 vel = rb.velocity;

        // 1. SPEED LOCK
        float spd = vel.magnitude;
        if (spd < 0.1f) { rb.velocity = moveDir * initialSpeed; return; }
        vel = vel.normalized * initialSpeed;

        // 2. MINIMUM VERTICAL VELOCITY
        float minY = initialSpeed * MIN_Y_FRAC;
        if (Mathf.Abs(vel.y) < minY)
        {
            vel.y  = Mathf.Sign(vel.y == 0 ? 1f : vel.y) * minY;
            vel.x += Random.Range(-0.08f, 0.08f) * initialSpeed;
            vel    = vel.normalized * initialSpeed;
        }

        rb.velocity = vel;
        moveDir     = vel.normalized;

        // 3. CORNER-ESCAPE
        Vector3 pos = transform.position;
        float r = transform.localScale.x * 0.5f;
        bool nearLeft  = pos.x < -WALL_INNER + r * 2f;
        bool nearRight = pos.x >  WALL_INNER - r * 2f;
        bool nearTop   = pos.y >  CEILING_Y  - r * 2f;

        if ((nearLeft || nearRight) && nearTop)
        {
            float dirX = nearLeft ? 0.4f : -0.4f;
            rb.velocity = new Vector3(dirX, -0.9f, 0f).normalized * initialSpeed;
            stuckTimer  = 0f;
        }

        // 4. STUCK RESCUE
        if (Mathf.Abs(pos.y - lastY) < 0.04f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= STUCK_TIME)
            {
                float rx = Random.Range(-0.4f, 0.4f);
                rb.velocity = new Vector3(rx, 1f, 0f).normalized * initialSpeed;
                stuckTimer  = 0f;
            }
        }
        else { stuckTimer = 0f; }
        lastY = pos.y;

        // Squash cancel
        if (squashing) RecoverScale();
    }

    // ─────────────────────────────────────────────────────
    void OnCollisionEnter(Collision col)
    {
        if (!isLaunched) return;

        ContactPoint cp = col.contacts[0];
        Vector3 normal  = cp.normal;
        normal.z = 0f;
        if (normal == Vector3.zero) normal = Vector3.up;

        // Squash on impact (1 frame visual)
        if (!squashing) StartCoroutine(SquashEffect(normal));

        // ── PADDLE ──────────────────────────────────────
        if (col.gameObject.CompareTag("Paddle"))
        {
            PaddleController pc = col.gameObject.GetComponent<PaddleController>();
            if (pc != null) pc.OnBallHit();

            GameManager.Instance?.ResetComboMiss();

            float halfW  = col.collider.bounds.size.x * 0.5f;
            float offset = Mathf.Clamp(
                (transform.position.x - col.transform.position.x) / halfW,
                -0.92f, 0.92f);

            float angleDeg = Mathf.Lerp(20f, 72f, Mathf.Abs(offset));
            float rad      = angleDeg * Mathf.Deg2Rad;
            float signX    = Mathf.Sign(offset == 0f ? Random.Range(-1f, 1f) : offset);

            Vector3 dir = new Vector3(Mathf.Sin(rad) * signX, Mathf.Cos(rad), 0f);
            rb.velocity = dir * initialSpeed;
            moveDir     = dir;

            CameraController.Instance?.Shake(0.07f, 0.09f);
            return;
        }

        // ── WALLS ────────────────────────────────────────
        if (col.gameObject.name.StartsWith("Wall"))
        {
            AudioManager.Instance?.PlayWallBounce();
            CameraController.Instance?.Shake(0.03f, 0.05f);
        }

        // Generic reflect
        Vector3 reflected = Vector3.Reflect(rb.velocity.normalized, normal.normalized);
        float minY = initialSpeed * MIN_Y_FRAC;
        if (Mathf.Abs(reflected.y) < minY)
        {
            reflected.y  = Mathf.Sign(reflected.y == 0 ? 1f : reflected.y) * minY;
            reflected.x += Random.Range(-0.1f, 0.1f);
        }
        reflected    = reflected.normalized;
        rb.velocity  = reflected * initialSpeed;
        moveDir      = reflected;
    }

    // ─────────────────────────────────────────────────────
    private IEnumerator SquashEffect(Vector3 collisionNormal)
    {
        squashing = true;
        Vector3 squash;

        // Squash along the normal direction
        if (Mathf.Abs(collisionNormal.y) > 0.5f)
            squash = new Vector3(1.3f, 0.7f, 1f);
        else
            squash = new Vector3(0.7f, 1.3f, 1f);

        transform.localScale = Vector3.Scale(baseScale, squash);
        yield return null; // 1 frame squash
        yield return null;

        // Spring back
        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(
                Vector3.Scale(baseScale, squash), baseScale, t / 0.1f);
            yield return null;
        }
        transform.localScale = baseScale;
        squashing = false;
    }

    private void RecoverScale()
    {
        if (!squashing) transform.localScale = baseScale;
    }

    // ─────────────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            isLaunched = false;
            if (trail)      trail.Clear();
            if (trailGhost) trailGhost.Clear();
            GameManager.Instance?.LoseLife();
            Destroy(gameObject, 0.1f);
        }
    }

    // ── Trail on/off (settings) ───────────────────────────
    public void SetTrailEnabled(bool on)
    {
        if (trail)      trail.enabled      = on;
        if (trailGhost) trailGhost.enabled = on;
    }
}
