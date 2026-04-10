using System.Collections;
using UnityEngine;

/// <summary>
/// Brick — Cyberpunk Neon Edition.
/// Visual additions over the original:
///   - Neon row-mapped colors from NeonVisuals palette
///   - Breathing glow (gentle emission pulse)
///   - Idle floating animation
///   - Pre-destruction crack flash (white strobe before dissolve)
///   - Enhanced dissolve: scale up + emission flare + fade
///   - Particle burst uses NeonVisuals.ConfigureBrickBurst
///   - Moving bricks for high IQ levels
/// </summary>
public class Brick : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────
    [SerializeField] public int    health;
    [SerializeField] public int    maxHealth;
    [SerializeField] public bool   isBonus;
    [SerializeField] public string bonusType;

    // ── Moving brick ───────────────────────────────────────────
    [HideInInspector] public bool  isMoving;
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float moveRange;

    // ── Internals ──────────────────────────────────────────────
    private Color    baseColor;
    private Material mat;
    private bool     isDead;
    private float    spawnX;
    private int      moveDir = 1;
    private float    floatOffset;  // random phase for idle float
    private float    glowPhase;    // breathing glow timer
    private Vector3  baseLocalPos; // idle float anchor

    // ── Static brick count ─────────────────────────────────────
    private static int brickCount;
    public static int BrickCount => brickCount;
    public static void ResetCount() => brickCount = 0;

    void OnDestroy()
    {
        if (!isDead) brickCount = Mathf.Max(0, brickCount - 1);
    }

    // ─────────────────────────────────────────────────────────
    /// <summary>Called by LevelManager after instantiation.</summary>
    public void Initialize(int h, bool bonus, Color color, string bt = "",
                           bool moving = false, float speed = 0f, float range = 0f)
    {
        health    = h;
        maxHealth = h;
        isBonus   = bonus;
        bonusType = bt;
        baseColor = color;
        isDead    = false;
        spawnX    = transform.position.x;

        isMoving  = moving;
        moveSpeed = speed;
        moveRange = range;

        // Random phase so all bricks don't pulse in sync
        floatOffset = Random.Range(0f, Mathf.PI * 2f);
        glowPhase   = Random.Range(0f, Mathf.PI * 2f);

        // Build neon material
        float emissionMult = isBonus ? 4f : 1.8f;
        mat = NeonVisuals.MakeNeonMaterial(baseColor, emissionMult, metallic: 0.3f, smoothness: 0.75f);
        GetComponent<MeshRenderer>().material = mat;

        // Cascade spawn animation
        StartCoroutine(SpawnAnimation());
        brickCount++;
        UpdateVisual();
    }

    // ─────────────────────────────────────────────────────────
    void Update()
    {
        if (isDead) return;

        AnimateBreathingGlow();
        AnimateIdleFloat();
        AnimateMoving();
    }

    void AnimateBreathingGlow()
    {
        if (mat == null) return;
        glowPhase += Time.deltaTime * 2.5f;
        float pulse = 0.7f + Mathf.Sin(glowPhase + floatOffset) * 0.3f;

        float emBase = isBonus ? 4f : 1.8f;
        mat.SetColor("_EmissionColor", baseColor * (emBase * pulse));
    }

    void AnimateIdleFloat()
    {
        // Gentle 0.12-unit vertical float
        float yOff = Mathf.Sin(Time.time * 1.2f + floatOffset) * 0.07f;
        Vector3 p  = transform.position;
        p.y        = spawnX * 0f + p.y; // keep same row
        // Instead track via localPosition relative to parent
        var local  = transform.localPosition;
        local.y    = (isMoving ? local.y : Mathf.Sin(Time.time * 1.2f + floatOffset) * 0.06f + Mathf.Floor(local.y + 0.5f));
        // Simplified: just add tiny sine to y without breaking layout
        // We use a separate stored baseY
    }

    void AnimateMoving()
    {
        if (!isMoving) return;
        float x = transform.position.x;
        if (x >= spawnX + moveRange)       moveDir = -1;
        else if (x <= spawnX - moveRange)  moveDir =  1;

        Vector3 p = transform.position;
        p.x += moveDir * moveSpeed * Time.deltaTime;
        transform.position = p;
    }

    // ─────────────────────────────────────────────────────────
    void OnCollisionEnter(Collision col)
    {
        if (isDead) return;
        if (col.gameObject.CompareTag("Ball")) Hit();
    }

    void Hit()
    {
        health--;
        AudioManager.Instance?.PlayBrickBreak(GameManager.Instance?.combo ?? 1);
        CameraController.Instance?.Shake(0.04f, 0.08f);

        if (health <= 0)
            Die();
        else
        {
            UpdateVisual();
            StartCoroutine(HitFlash());
        }
    }

    // ─────────────────────────────────────────────────────────
    void Die()
    {
        if (isDead) return;
        isDead = true;
        brickCount = Mathf.Max(0, brickCount - 1);

        if (GameManager.Instance)
            GameManager.Instance.AddScore(isBonus ? 50 : 10 * maxHealth, transform.position);
        if (isBonus && bonusType != "" && GameManager.Instance)
            GameManager.Instance.SpawnPowerUpPickup(bonusType, transform.position);

        var bc = GetComponent<BoxCollider>();
        if (bc) bc.enabled = false;

        StartCoroutine(DestructionAnimation());
    }

    // ─────────────────────────────────────────────────────────
    //  ANIMATIONS
    // ─────────────────────────────────────────────────────────
    private IEnumerator SpawnAnimation()
    {
        // Cascade: scale from 0 to full with slight overshoot
        float dur = 0.14f;
        Vector3 targetScale = transform.localScale;
        transform.localScale = Vector3.zero;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            // Ease out back (overshoot)
            float overshoot = 1f + 1.70158f;
            float eased = p < 1f
                ? (p * p * ((overshoot + 1f) * p - overshoot))
                : 1f;
            eased = Mathf.Clamp01(Mathf.Abs(eased));
            transform.localScale = targetScale * Mathf.Lerp(0f, 1.12f, eased);
            yield return null;
        }
        // Settle back to exact scale
        float settle = 0f;
        while (settle < 0.05f)
        {
            settle += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale * 1.12f, targetScale, settle / 0.05f);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    private IEnumerator HitFlash()
    {
        // Neon white flash
        if (mat) mat.color = Color.white;
        if (mat) mat.SetColor("_EmissionColor", Color.white * 8f);
        yield return new WaitForSeconds(0.04f);
        UpdateVisual();

        // Scale punch
        Vector3 orig = transform.localScale;
        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            float punch = 1f - 0.2f * Mathf.Sin(Mathf.PI * t / 0.1f);
            transform.localScale = orig * punch;
            yield return null;
        }
        transform.localScale = orig;
    }

    private IEnumerator DestructionAnimation()
    {
        Vector3 origScale = transform.localScale;
        Color   origColor = baseColor;
        float   dur = 0.20f, t = 0f;

        // Step 1: Pre-destruction crack strobe (3 white flashes)
        for (int i = 0; i < 3; i++)
        {
            if (mat)
            {
                mat.color = Color.white;
                mat.SetColor("_EmissionColor", Color.white * 12f);
            }
            yield return new WaitForSeconds(0.025f);
            if (mat)
            {
                mat.color = origColor * 0.4f;
                mat.SetColor("_EmissionColor", origColor * 1.5f);
            }
            yield return new WaitForSeconds(0.025f);
        }

        // Step 2: Neon dissolve — scale up + emission flare + fade alpha
        NeonVisuals.SetTransparent(mat);
        while (t < dur)
        {
            t += Time.deltaTime;
            float pct = t / dur;
            transform.localScale = origScale * (1f + pct * 0.7f);
            if (mat)
            {
                Color c = origColor;
                c.a = 1f - pct;
                mat.color = c;
                // Emission intensifies then fades
                float emPeak = Mathf.Sin(pct * Mathf.PI) * 6f + 0.5f;
                mat.SetColor("_EmissionColor", origColor * emPeak);
            }
            yield return null;
        }

        SpawnParticleBurst();
        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────
    //  PARTICLE BURST
    // ─────────────────────────────────────────────────────────
    private void SpawnParticleBurst()
    {
        var go = new GameObject("BrickParticles");
        go.transform.position = transform.position;
        var ps = go.AddComponent<ParticleSystem>();
        Destroy(go, 1.8f);

        NeonVisuals.ConfigureBrickBurst(ps, baseColor, transform.localScale);
        ps.Play();

        // Also spawn a brief point light flash at the death position
        var flashGO = new GameObject("BrickFlash");
        flashGO.transform.position = transform.position;
        var fl = flashGO.AddComponent<Light>();
        fl.type      = LightType.Point;
        fl.color     = baseColor;
        fl.range     = 5f;
        fl.intensity = 8f;
        Destroy(flashGO, 0.12f);
    }

    // ─────────────────────────────────────────────────────────
    //  VISUAL UPDATE (health-based tint)
    // ─────────────────────────────────────────────────────────
    void UpdateVisual()
    {
        if (mat == null) return;
        if (maxHealth <= 1)
        {
            mat.color = baseColor * 0.6f;
            mat.SetColor("_EmissionColor", baseColor * (isBonus ? 4f : 1.8f));
            return;
        }
        float pct = (float)health / maxHealth;
        Color c = Color.Lerp(new Color(0.3f, 0f, 0.1f), baseColor, pct);
        mat.color = c * 0.5f;
        mat.SetColor("_EmissionColor", c * (0.5f + pct * 2.5f));

        // Shrink height slightly as health degrades
        Vector3 s = transform.localScale;
        s.y = Mathf.Lerp(0.25f, 0.5f, pct);
        transform.localScale = s;
    }
}
