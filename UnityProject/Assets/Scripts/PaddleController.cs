using System.Collections;
using UnityEngine;

/// <summary>
/// PaddleController — Heavy Robustness Fix.
/// Removed Rigidbody-based smoothing to prevent desync glitches.
/// Uses direct transform updates for 100% reliable side-to-side movement.
/// </summary>
public class PaddleController : MonoBehaviour
{
    private const float WALL_INNER = 8.51f;

    [Header("Movement")]
    public float speed        = 22f;
    public float currentWidth = 3.2f;

    [Header("Visual Juice")]
    [SerializeField] private float maxTiltAngle  = 12f;
    [SerializeField] private float tiltSpeed     = 12f;
    [SerializeField] private float pulseScale    = 1.15f;
    [SerializeField] private float pulseDecay    = 8f;

    // ── Internal ──────────────────────────────────────────────
    private Material  mat;
    private Light     underLight;
    private float     currentX;
    private float     inputH;
    private float     currentTilt;
    private float     scaleMultiplier = 1f;
    private Vector3   baseScale;
    private float     hueShift;
    private float     energyGlow;
    private Rigidbody rb;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb) { rb.isKinematic = true; rb.useGravity = false; }

        baseScale = transform.localScale;
        ApplyMaterial();
        SetupUnderglow();
        currentX = transform.position.x;
    }

    void ApplyMaterial()
    {
        mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", NeonVisuals.NeonCyan * 1.5f);
        GetComponent<MeshRenderer>().material = mat;
    }

    void SetupUnderglow()
    {
        var lightGO = new GameObject("PaddleUnderglow");
        lightGO.transform.SetParent(transform);
        lightGO.transform.localPosition = new Vector3(0f, -0.4f, 0f);
        underLight = lightGO.AddComponent<Light>();
        underLight.type = LightType.Point;
        underLight.color = NeonVisuals.NeonCyan;
        underLight.range = 5f;
        underLight.intensity = 3f;
    }

    // ─────────────────────────────────────────────────────────
    void Update()
    {
        // 1. Move even if paused or during countdown
        HandleMovement();
        
        // 2. Visuals
        AnimateTilt();
        AnimateHolographic();
        AnimateEnergyField();
        AnimatePulseDecay();
    }

    void HandleMovement()
    {
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;

        inputH = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) inputH = -1f;
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) inputH =  1f;
        
        float axis = Input.GetAxisRaw("Horizontal");
        if (inputH == 0f && Mathf.Abs(axis) > 0.1f) inputH = Mathf.Sign(axis);

        // Move currentX
        float dt = Time.unscaledDeltaTime; // Use unscaled to ensure it moves always
        currentX += inputH * speed * dt;

        // Clamp
        float halfW = currentWidth * 0.5f;
        float limit = WALL_INNER - halfW;
        currentX = Mathf.Clamp(currentX, -limit, limit);

        // Apply Position directly
        transform.position = new Vector3(currentX, transform.position.y, 0f);
        if (rb) rb.position = transform.position; // Sync physics
    }

    void AnimateTilt()
    {
        float targetTilt = -inputH * maxTiltAngle;
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, tiltSpeed * Time.unscaledDeltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, currentTilt);
    }

    void AnimateHolographic()
    {
        if (mat == null) return;
        hueShift += Time.unscaledDeltaTime * 0.2f;
        Color holo = NeonVisuals.RainbowColor(hueShift);
        mat.SetColor("_EmissionColor", Color.Lerp(NeonVisuals.NeonCyan, holo, 0.4f) * (1.5f + energyGlow * 2f));
        if (underLight) { underLight.color = holo; underLight.intensity = 2f + energyGlow * 3f; }
    }

    void AnimateEnergyField()
    {
        if (GameManager.Instance?.ball == null) { energyGlow = Mathf.Lerp(energyGlow, 0f, Time.unscaledDeltaTime * 4f); return; }
        float dist = Mathf.Abs(GameManager.Instance.ball.transform.position.y - transform.position.y);
        energyGlow = Mathf.Lerp(energyGlow, Mathf.Clamp01(1f - dist / 5f), Time.unscaledDeltaTime * 5f);
    }

    void AnimatePulseDecay()
    {
        scaleMultiplier = Mathf.Lerp(scaleMultiplier, 1f, pulseDecay * Time.unscaledDeltaTime);
        transform.localScale = new Vector3(baseScale.x * scaleMultiplier, baseScale.y * (2f - scaleMultiplier), baseScale.z);
    }

    public void OnBallHit()
    {
        scaleMultiplier = pulseScale;
        AudioManager.Instance?.PlayPaddleBounce();
        if (underLight) underLight.intensity = 12f;
    }

    public void SetWidth(float w)
    {
        currentWidth = w;
        baseScale = new Vector3(w, baseScale.y, baseScale.z);
    }

    public void ResetPaddle()
    {
        currentX = 0f;
        inputH = 0f;
        transform.position = new Vector3(0f, -4.5f, 0f);
        transform.rotation = Quaternion.identity;
        transform.localScale = baseScale;
        if (rb) { rb.velocity = Vector3.zero; rb.position = transform.position; }
    }
}
