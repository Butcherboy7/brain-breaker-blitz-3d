using System.Collections;
using UnityEngine;

/// <summary>
/// Dedicated camera controller.
/// Handles screen shake with magnitude decay, optional ball follow with damping,
/// and FOV zoom-out during multi-ball state.
/// Attach to the Main Camera GameObject.
/// </summary>
public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    // ── Configuration ─────────────────────────────────────────
    [Header("Base Position")]
    [SerializeField] private Vector3 basePosition = new Vector3(0f, 1f, -13f);
    [SerializeField] private Vector3 baseRotation = new Vector3(8f, 0f, 0f);
    [SerializeField] private float   baseFOV      = 60f;

    [Header("Ball Follow")]
    [SerializeField] private bool  followBall      = false;
    [SerializeField] private float followDamping   = 4f;
    [SerializeField] private float maxFollowOffset = 1.5f;

    [Header("Shake")]
    [SerializeField] private float shakeDecay = 8f;

    [Header("Multi-Ball FOV")]
    [SerializeField] private float multiBallFOV    = 68f;
    [SerializeField] private float fovLerpSpeed    = 3f;

    // ── Internal ──────────────────────────────────────────────
    private Camera cam;
    private float  shakeMax;
    private float  shakeDur;
    private float  shakeTimer;
    private float  targetFOV;
    private bool   isMultiBall;

    // ─────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }

        cam = GetComponent<Camera>();
        if (cam == null) cam = gameObject.AddComponent<Camera>();

        transform.position = basePosition;
        transform.rotation = Quaternion.Euler(baseRotation);
        targetFOV          = baseFOV;
        cam.fieldOfView    = baseFOV;
        cam.backgroundColor = new Color(0.04f, 0.04f, 0.12f);
        cam.clearFlags = CameraClearFlags.SolidColor;
    }

    void LateUpdate()
    {
        Vector3 desiredPos = basePosition;

        // ── Ball follow (optional) ────────────────────────────
        if (followBall && GameManager.Instance?.ball != null)
        {
            float bx = GameManager.Instance.ball.transform.position.x;
            float offset = Mathf.Clamp(bx * 0.12f, -maxFollowOffset, maxFollowOffset);
            desiredPos.x = Mathf.Lerp(transform.position.x, basePosition.x + offset,
                                       followDamping * Time.deltaTime);
        }

        // ── Shake ─────────────────────────────────────────────
        if (shakeTimer > 0f)
        {
            float t   = shakeTimer / shakeDur;
            float mag = shakeMax * t;
            desiredPos += (Vector3)(Random.insideUnitCircle * mag);
            shakeTimer -= Time.unscaledDeltaTime;
        }

        transform.position = desiredPos;

        // ── FOV ───────────────────────────────────────────────
        targetFOV = isMultiBall ? multiBallFOV : baseFOV;
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovLerpSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────

    /// <summary>Trigger camera shake. Later calls with higher mag override.</summary>
    public void Shake(float magnitude, float duration)
    {
        if (magnitude >= shakeMax || shakeTimer <= 0f)
        {
            shakeMax   = magnitude;
            shakeDur   = duration;
            shakeTimer = duration;
        }
    }

    /// <summary>Set multi-ball FOV state.</summary>
    public void SetMultiBall(bool active) => isMultiBall = active;

    /// <summary>Instantly reset camera to base (call between levels).</summary>
    public void ResetCamera()
    {
        shakeTimer = 0f;
        shakeMax   = 0f;
        transform.position = basePosition;
        transform.rotation = Quaternion.Euler(baseRotation);
        cam.fieldOfView    = baseFOV;
        isMultiBall        = false;
    }

    /// <summary>Flash the screen with a color (e.g. red on life lost).</summary>
    public void FlashScreen(Color color, float duration)
    {
        StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        // Create a full-screen quad overlay (UI approach via camera background tint)
        Color orig = cam.backgroundColor;
        float t = 0f;
        while (t < duration * 0.4f)
        {
            cam.backgroundColor = Color.Lerp(orig, color, t / (duration * 0.4f));
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        t = 0f;
        while (t < duration * 0.6f)
        {
            cam.backgroundColor = Color.Lerp(color, orig, t / (duration * 0.6f));
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        cam.backgroundColor = orig;
    }
}
