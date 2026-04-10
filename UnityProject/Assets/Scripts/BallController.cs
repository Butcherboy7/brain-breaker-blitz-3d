using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Robust brick-breaker ball with:
///  - Manual reflection (no reliance on Unity PhysicMaterial alone)
///  - Minimum vertical velocity enforcement (prevents wall-to-wall horizontal trap)
///  - Jitter-nudge on corner hits
///  - Constant speed lock every FixedUpdate
///  - Stuck-detection: relaunch if ball stays near same Y for 3 seconds
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class BallController : MonoBehaviour
{
    // ── Public ──────────────────────────────────────────
    public Rigidbody rb;
    public float initialSpeed = 8f;
    public bool isLaunched = false;

    // ── Physics constants ────────────────────────────────
    // Ball must always have at least this fraction of speed in Y axis
    // Prevents the ball locking into a pure horizontal bounce
    const float MIN_Y_FRACTION = 0.3f;      // 30% of total speed minimum vertical
    const float STUCK_TIME      = 3f;        // seconds before "stuck" rescue fires

    // ── Stuck detection ──────────────────────────────────
    private float lastY;
    private float stuckTimer;

    // ── Trail ────────────────────────────────────────────
    private TrailRenderer trail;

    // ─────────────────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezePositionZ
                       | RigidbodyConstraints.FreezeRotation;

        // Frictionless, perfectly bouncy physics material
        var pm = new PhysicMaterial("BallBounce") {
            bounciness     = 1f,
            dynamicFriction = 0f,
            staticFriction  = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum,
            bounceCombine   = PhysicMaterialCombine.Maximum
        };
        GetComponent<SphereCollider>().material = pm;

        // Glowing material
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.92f, 0.1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.0f) * 2.5f);
        GetComponent<MeshRenderer>().material = mat;

        // Fire trail
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time       = 0.22f;
        trail.startWidth = 0.3f;
        trail.endWidth   = 0f;
        trail.material   = new Material(Shader.Find("Sprites/Default"));
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f,0.8f,0f), 0f),
                new GradientColorKey(new Color(1f,0f,0f),   1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = g;
    }

    // ─────────────────────────────────────────────────────
    public void ResetBall(float speed)
    {
        initialSpeed    = speed;
        isLaunched      = false;
        stuckTimer      = 0f;
        transform.position = new Vector3(0, -3.2f, 0);
        rb.velocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // ─────────────────────────────────────────────────────
    void Update()
    {
        if (!isLaunched
            && GameManager.Instance != null
            && GameManager.Instance.isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
                Launch();
        }
    }

    void Launch()
    {
        isLaunched = true;
        // Always launch upward-ish with a slight random X
        float xFrac = Random.Range(-0.4f, 0.4f);
        Vector3 dir = new Vector3(xFrac, 1f, 0f).normalized;
        rb.velocity = dir * initialSpeed;
    }

    // ─────────────────────────────────────────────────────
    void FixedUpdate()
    {
        if (!isLaunched) return;

        Vector3 vel = rb.velocity;

        // 1. SPEED LOCK – keep the ball at exactly initialSpeed
        if (vel.sqrMagnitude > 0.01f)
            vel = vel.normalized * initialSpeed;

        // 2. MIN-Y-VELOCITY – the key fix for horizontal bouncing
        float minY = initialSpeed * MIN_Y_FRACTION;
        if (Mathf.Abs(vel.y) < minY)
        {
            vel.y = vel.y >= 0f ? minY : -minY;
            // Re-normalise and scale back to correct speed
            vel = vel.normalized * initialSpeed;
        }

        rb.velocity = vel;

        // 3. STUCK DETECTION – rescue the ball if it barely moves vertically
        if (Mathf.Abs(transform.position.y - lastY) < 0.05f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= STUCK_TIME)
            {
                Debug.Log("[Ball] Stuck detected – relaunching upward.");
                float rx = Random.Range(-0.5f, 0.5f);
                rb.velocity = new Vector3(rx, 1f, 0f).normalized * initialSpeed;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastY = transform.position.y;
    }

    // ─────────────────────────────────────────────────────
    void OnCollisionEnter(Collision col)
    {
        if (!isLaunched) return;

        Vector3 vel     = rb.velocity;
        Vector3 normal  = col.contacts[0].normal;
        normal.z = 0f; // project to 2D plane

        // ── PADDLE – angle based on hit offset ──────────
        if (col.gameObject.CompareTag("Paddle"))
        {
            float halfW = col.collider.bounds.size.x * 0.5f;
            float offset = Mathf.Clamp(
                (transform.position.x - col.transform.position.x) / halfW,
                -0.95f, 0.95f);

            // Spread from 20° (center) to 75° (edge) measured from vertical
            float angle = Mathf.Lerp(20f, 75f, Mathf.Abs(offset)) * Mathf.Deg2Rad;
            float dirX  = Mathf.Sin(angle) * Mathf.Sign(offset != 0 ? offset : 0.01f);
            float dirY  = Mathf.Cos(angle);

            rb.velocity = new Vector3(dirX, dirY, 0f).normalized * initialSpeed;
            GameManager.Instance?.TriggerScreenShake(0.06f, 0.1f);
            return; // skip generic reflect
        }

        // ── STANDARD REFLECT (walls, bricks, ceiling) ───
        Vector3 reflected = Vector3.Reflect(vel, normal);

        // Safety: ensure reflected has some vertical component
        float minY = initialSpeed * MIN_Y_FRACTION;
        if (Mathf.Abs(reflected.y) < minY)
        {
            // Add a small random nudge to break degenerate angles
            reflected.y = (reflected.y >= 0f ? minY : -minY)
                        + Random.Range(-0.05f, 0.05f) * initialSpeed;
        }

        // Tiny random jitter to break corner traps
        reflected.x += Random.Range(-0.04f, 0.04f) * initialSpeed;

        rb.velocity = reflected.normalized * initialSpeed;
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
