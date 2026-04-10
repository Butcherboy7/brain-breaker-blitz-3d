using System.Collections;
using UnityEngine;

/// <summary>
/// Kinematic-style ball controller.
/// Moves purely with MovePosition() so it is always smooth (no FixedUpdate jitter).
/// Physics:
///   - Manual reflection using Vector3.Reflect on the contact normal
///   - Min-Y fraction: ball always has ≥30% of speed as vertical component
///   - Corner-escape: if near two walls simultaneously, nudge outward
///   - Stuck rescue: if Y barely changes for 3 sec, force upward relaunch
/// </summary>
public class BallController : MonoBehaviour
{
    // ── Public API ───────────────────────────────────────
    public float initialSpeed = 8f;
    public bool  isLaunched   = false;
    [HideInInspector] public Rigidbody rb;

    // ── Physics constants ────────────────────────────────
    const float MIN_Y_FRAC  = 0.30f;   // minimum |vy| / speed
    const float STUCK_TIME  = 2.8f;    // seconds before rescue
    const float WALL_INNER  = 8.5f;
    const float CEILING_Y   = 6.5f;

    // ── Internal state ───────────────────────────────────
    private Vector3 moveDir  = Vector3.up;
    private float   stuckTimer;
    private float   lastY;
    private TrailRenderer trail;

    // ─────────────────────────────────────────────────────
    void Awake()
    {
        // 60 fps lock
        Application.targetFrameRate = 60;
        Time.fixedDeltaTime         = 1f / 60f;

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity             = false;
        rb.isKinematic            = false;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.constraints            = RigidbodyConstraints.FreezePositionZ
                                  | RigidbodyConstraints.FreezeRotation;

        // Bounciest physics material – last line of defence
        GetComponent<SphereCollider>().material = MakePMat("Ball");

        // Glow material
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.92f, 0.1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.55f, 0f) * 3f);
        GetComponent<MeshRenderer>().material = mat;

        // Smooth fire trail
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time       = 0.18f;
        trail.startWidth = 0.28f;
        trail.endWidth   = 0f;
        trail.numCapVertices = 8;
        var trailMat = new Material(Shader.Find("Sprites/Default"));
        trail.material = trailMat;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]{
                new GradientColorKey(new Color(1f,0.8f,0f),0f),
                new GradientColorKey(new Color(1f,0.1f,0f),1f)},
            new GradientAlphaKey[]{
                new GradientAlphaKey(1f,0f),
                new GradientAlphaKey(0f,1f)});
        trail.colorGradient = g;
    }

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
        rb.velocity            = Vector3.zero;
        rb.angularVelocity     = Vector3.zero;
        if (trail) trail.Clear();
    }

    // ─────────────────────────────────────────────────────
    void Update()
    {
        if (!isLaunched
            && GameManager.Instance != null
            && GameManager.Instance.isPlaying
            && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            Launch();
        }
    }

    void Launch()
    {
        isLaunched = true;
        float rx = Random.Range(-0.35f, 0.35f);
        moveDir = new Vector3(rx, 1f, 0f).normalized;
        rb.velocity = moveDir * initialSpeed;
    }

    // ─────────────────────────────────────────────────────
    void FixedUpdate()
    {
        if (!isLaunched) return;

        Vector3 vel = rb.velocity;

        // ── 1. SPEED LOCK ─────────────────────────────────
        float spd = vel.magnitude;
        if (spd < 0.1f) { rb.velocity = moveDir * initialSpeed; return; }
        vel = vel.normalized * initialSpeed;

        // ── 2. MINIMUM VERTICAL VELOCITY ─────────────────
        float minY = initialSpeed * MIN_Y_FRAC;
        if (Mathf.Abs(vel.y) < minY)
        {
            vel.y  = Mathf.Sign(vel.y == 0 ? 1f : vel.y) * minY;
            // tiny random to break symmetry
            vel.x += Random.Range(-0.08f, 0.08f) * initialSpeed;
            vel    = vel.normalized * initialSpeed;
        }

        rb.velocity = vel;
        moveDir     = vel.normalized;

        // ── 3. CORNER-ESCAPE ─────────────────────────────
        Vector3 pos = transform.position;
        float r = transform.localScale.x * 0.5f;
        bool nearLeft  = pos.x < -WALL_INNER + r * 2f;
        bool nearRight = pos.x >  WALL_INNER - r * 2f;
        bool nearTop   = pos.y >  CEILING_Y  - r * 2f;

        if ((nearLeft || nearRight) && nearTop)
        {
            // Definitely in a corner – kick downward
            float dirX = nearLeft ? 0.4f : -0.4f;
            rb.velocity = new Vector3(dirX, -0.9f, 0f).normalized * initialSpeed;
            stuckTimer  = 0f;
        }

        // ── 4. STUCK RESCUE ──────────────────────────────
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
    }

    // ─────────────────────────────────────────────────────
    void OnCollisionEnter(Collision col)
    {
        if (!isLaunched) return;

        ContactPoint cp      = col.contacts[0];
        Vector3 normal       = cp.normal;
        normal.z = 0f;
        if (normal == Vector3.zero) normal = Vector3.up;

        // ── PADDLE ── custom angle based on hit position ─
        if (col.gameObject.CompareTag("Paddle"))
        {
            float halfW  = col.collider.bounds.size.x * 0.5f;
            float offset = Mathf.Clamp(
                (transform.position.x - col.transform.position.x) / halfW,
                -0.92f, 0.92f);

            // Map offset → launch angle 20°…72° from vertical
            float angleDeg = Mathf.Lerp(20f, 72f, Mathf.Abs(offset));
            float rad      = angleDeg * Mathf.Deg2Rad;
            float signX    = Mathf.Sign(offset == 0f ? Random.Range(-1f,1f) : offset);

            Vector3 dir = new Vector3(Mathf.Sin(rad) * signX, Mathf.Cos(rad), 0f);
            rb.velocity = dir * initialSpeed;
            moveDir     = dir;
            GameManager.Instance?.TriggerScreenShake(0.07f, 0.09f);
            return;
        }

        // ── GENERIC REFLECT ──────────────────────────────
        Vector3 reflected = Vector3.Reflect(rb.velocity.normalized, normal.normalized);

        // Enforce min Y again post-reflect
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
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeadZone"))
        {
            isLaunched = false;
            GameManager.Instance?.LoseLife();
        }
    }
}
